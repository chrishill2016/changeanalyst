using ChangeAnalyst.Services.Sync;
using ChangeAnalyst.Shared.Sync;
using ChangeAnalyst.Tests.Fakes;

namespace ChangeAnalyst.Tests.Sync;

public class OutboxTests
{
    private static readonly DateTimeOffset T0 = new(2026, 1, 1, 9, 0, 0, TimeSpan.Zero);
    private readonly Outbox _outbox = new(new InMemoryLocalStore());

    [Fact]
    public async Task Later_change_to_same_entity_replaces_earlier_one()
    {
        var id = Guid.NewGuid();
        await _outbox.EnqueueAsync(new SyncRecord("Test", id, "{\"v\":1}", T0));
        await _outbox.EnqueueAsync(new SyncRecord("Test", id, "{\"v\":2}", T0.AddMinutes(1)));

        var pending = Assert.Single(await _outbox.GetPendingAsync());
        Assert.Equal("{\"v\":2}", pending.Json);
    }

    [Fact]
    public async Task Pending_changes_are_returned_oldest_first()
    {
        await _outbox.EnqueueAsync(new SyncRecord("Test", Guid.NewGuid(), "late", T0.AddMinutes(5)));
        await _outbox.EnqueueAsync(new SyncRecord("Test", Guid.NewGuid(), "early", T0));

        Assert.Equal(["early", "late"], (await _outbox.GetPendingAsync()).Select(r => r.Json));
    }

    [Fact]
    public async Task RemoveIfUnchanged_removes_the_pushed_version()
    {
        var record = new SyncRecord("Test", Guid.NewGuid(), "{}", T0);
        await _outbox.EnqueueAsync(record);

        await _outbox.RemoveIfUnchangedAsync(record);

        Assert.Equal(0, await _outbox.CountAsync());
    }

    [Fact]
    public async Task RemoveIfUnchanged_keeps_an_edit_made_during_the_push()
    {
        var pushed = new SyncRecord("Test", Guid.NewGuid(), "old", T0);
        await _outbox.EnqueueAsync(pushed with { Json = "new", LastModified = T0.AddSeconds(5) });

        await _outbox.RemoveIfUnchangedAsync(pushed);

        Assert.Equal("new", Assert.Single(await _outbox.GetPendingAsync()).Json);
    }
}
