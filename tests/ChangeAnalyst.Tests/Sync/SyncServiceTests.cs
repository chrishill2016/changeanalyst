using ChangeAnalyst.Services.Storage;
using ChangeAnalyst.Services.Sync;
using ChangeAnalyst.Shared.Sync;
using ChangeAnalyst.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace ChangeAnalyst.Tests.Sync;

public class SyncServiceTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);

    private readonly InMemoryLocalStore _store = new();
    private readonly FakeConnectivity _connectivity = new();
    private readonly FakeTimeProvider _time = new(T0);
    private readonly List<SyncNotification> _notifications = [];
    private readonly Outbox _outbox;
    private FakeRemoteSyncClient _remote = new();

    public SyncServiceTests() => _outbox = new Outbox(_store);

    private SyncService CreateService()
    {
        var service = new SyncService(_store, _outbox, _remote, _connectivity, _time, NullLogger<SyncService>.Instance);
        service.Notified += _notifications.Add;
        return service;
    }

    private static SyncRecord Record(Guid id, string json, DateTimeOffset when) => new("Test", id, json, when);

    [Fact]
    public async Task Save_while_offline_queues_the_change_and_counts_it_as_pending()
    {
        _connectivity.Set(false);
        var service = CreateService();
        await service.InitializeAsync();

        await service.SaveAsync(Record(Guid.NewGuid(), "{}", T0));

        Assert.Equal(1, service.Status.PendingChanges);
        Assert.Empty(_remote.Pushed);
    }

    [Fact]
    public async Task Coming_back_online_pushes_queued_changes_and_notifies()
    {
        _connectivity.Set(false);
        var service = CreateService();
        await service.InitializeAsync();
        await service.SaveAsync(Record(Guid.NewGuid(), "a", T0));
        await service.SaveAsync(Record(Guid.NewGuid(), "b", T0));

        _connectivity.Set(true);
        await service.SyncAsync(); // wait for any in-flight sync started by the event

        Assert.Equal(2, _remote.Pushed.Count);
        Assert.Equal(0, service.Status.PendingChanges);
        var notification = Assert.Single(_notifications);
        Assert.False(notification.IsError);
        Assert.Contains("2 changes synced", notification.Message);
    }

    [Fact]
    public async Task Initialize_syncs_on_start()
    {
        await _outbox.EnqueueAsync(Record(Guid.NewGuid(), "{}", T0));
        var service = CreateService();

        await service.InitializeAsync();

        Assert.Single(_remote.Pushed);
        Assert.Equal(T0, service.Status.LastSuccessfulSync);
        Assert.Empty(_notifications); // was never offline
    }

    [Fact]
    public async Task Pull_uses_the_server_time_from_the_previous_sync_as_cursor()
    {
        var firstServerTime = _remote.ServerTime;
        var service = CreateService();
        await service.InitializeAsync();

        _remote.ServerTime = firstServerTime.AddHours(1);
        await service.SyncAsync();

        Assert.Equal([null, firstServerTime], _remote.PullCursors);
    }

    [Fact]
    public async Task Newer_server_change_replaces_local_copy_and_drops_stale_queued_edit()
    {
        var id = Guid.NewGuid();
        _connectivity.Set(false);
        var service = CreateService();
        await service.InitializeAsync();
        await service.SaveAsync(Record(id, "local", T0));

        // Someone else edited it on the server after our offline edit.
        _remote.ToPull.Add(Record(id, "server", T0.AddMinutes(1)));
        _connectivity.Set(true);
        await service.SyncAsync();

        var stored = await _store.GetAsync<SyncRecord>(LocalStores.Entities, LocalStores.EntityKey("Test", id));
        Assert.Equal("server", stored!.Json);
        Assert.Equal(0, service.Status.PendingChanges);
    }

    [Fact]
    public async Task Older_server_change_does_not_overwrite_newer_local_edit()
    {
        var id = Guid.NewGuid();
        await _store.PutAsync(LocalStores.Entities, LocalStores.EntityKey("Test", id), Record(id, "local", T0.AddMinutes(5)));
        _remote.ToPull.Add(Record(id, "server", T0));
        var service = CreateService();

        await service.InitializeAsync();

        var stored = await _store.GetAsync<SyncRecord>(LocalStores.Entities, LocalStores.EntityKey("Test", id));
        Assert.Equal("local", stored!.Json);
    }

    [Fact]
    public async Task Deleted_server_record_removes_local_copy()
    {
        var id = Guid.NewGuid();
        await _store.PutAsync(LocalStores.Entities, LocalStores.EntityKey("Test", id), Record(id, "local", T0));
        _remote.ToPull.Add(Record(id, "", T0.AddMinutes(1)) with { IsDeleted = true });
        var service = CreateService();

        await service.InitializeAsync();

        Assert.Null(await _store.GetAsync<SyncRecord>(LocalStores.Entities, LocalStores.EntityKey("Test", id)));
    }

    [Fact]
    public async Task Edit_made_during_push_stays_queued()
    {
        var id = Guid.NewGuid();
        await _outbox.EnqueueAsync(Record(id, "v1", T0));
        _remote.DuringPush = () => _outbox.EnqueueAsync(Record(id, "v2", T0.AddSeconds(1)));
        var service = CreateService();

        await service.InitializeAsync();

        Assert.Equal("v2", Assert.Single(await _outbox.GetPendingAsync()).Json);
        Assert.Equal(1, service.Status.PendingChanges);
    }

    [Fact]
    public async Task Failure_keeps_changes_queued_and_reports_error()
    {
        await _outbox.EnqueueAsync(Record(Guid.NewGuid(), "{}", T0));
        _remote.FailWith = new HttpRequestException("boom");
        var service = CreateService();

        await service.InitializeAsync();

        Assert.Equal(1, service.Status.PendingChanges);
        Assert.Contains("boom", service.Status.LastError);
        Assert.Null(service.Status.LastSuccessfulSync);
        Assert.True(Assert.Single(_notifications).IsError);
    }

    [Fact]
    public async Task Successful_sync_clears_previous_error()
    {
        _remote.FailWith = new HttpRequestException("boom");
        var service = CreateService();
        await service.InitializeAsync();

        _remote.FailWith = null;
        Assert.True(await service.SyncAsync());

        Assert.Null(service.Status.LastError);
    }

    [Fact]
    public async Task Does_nothing_when_server_sync_is_not_configured()
    {
        _remote = new FakeRemoteSyncClient(isConfigured: false);
        var service = CreateService();
        await service.InitializeAsync();
        await service.SaveAsync(Record(Guid.NewGuid(), "{}", T0));

        Assert.False(await service.SyncAsync());
        Assert.False(service.Status.IsRemoteConfigured);
        Assert.Equal(1, service.Status.PendingChanges);
    }

    [Fact]
    public async Task Last_sync_time_survives_a_restart()
    {
        await CreateService().InitializeAsync();
        _connectivity.Set(false);

        var restarted = CreateService();
        await restarted.InitializeAsync();

        Assert.Equal(T0, restarted.Status.LastSuccessfulSync);
    }
}
