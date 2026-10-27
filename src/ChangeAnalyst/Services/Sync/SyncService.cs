using ChangeAnalyst.Services.Connectivity;
using ChangeAnalyst.Services.Storage;
using ChangeAnalyst.Shared.Sync;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Logging;

namespace ChangeAnalyst.Services.Sync;

public sealed class SyncService(
    ILocalStore store,
    IOutbox outbox,
    IRemoteSyncClient remote,
    IConnectivityService connectivity,
    TimeProvider time,
    ILogger<SyncService> logger) : ISyncService, IDisposable
{
    internal const string LastSuccessfulSyncKey = "lastSuccessfulSync";
    internal const string PullCursorKey = "pullCursor";

    private readonly SemaphoreSlim _gate = new(1, 1);
    private bool _initialized;
    private bool _offlineSinceLastSync;

    public SyncStatus Status { get; private set; } = new(remote.IsConfigured, true, false, 0, null, null);

    public event Action? StatusChanged;

    public event Action<SyncNotification>? Notified;

    public async Task InitializeAsync()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        await connectivity.InitializeAsync();
        connectivity.Changed += OnConnectivityChanged;
        _offlineSinceLastSync = !connectivity.IsOnline;

        SetStatus(Status with
        {
            IsOnline = connectivity.IsOnline,
            PendingChanges = await outbox.CountAsync(),
            LastSuccessfulSync = await store.GetAsync<DateTimeOffset?>(LocalStores.Meta, LastSuccessfulSyncKey),
        });

        await SyncAsync();
    }

    public async Task SaveAsync(SyncRecord record)
    {
        await store.PutAsync(LocalStores.Entities, LocalStores.EntityKey(record.EntityType, record.Id), record);
        await outbox.EnqueueAsync(record);
        SetStatus(Status with { PendingChanges = await outbox.CountAsync() });
        _ = SyncAsync();
    }

    public async Task<bool> SyncAsync()
    {
        if (!remote.IsConfigured || !connectivity.IsOnline || !await _gate.WaitAsync(0))
        {
            return false;
        }

        try
        {
            SetStatus(Status with { IsSyncing = true });

            var pending = await outbox.GetPendingAsync();
            if (pending.Count > 0)
            {
                await remote.PushAsync(pending);
                foreach (var record in pending)
                {
                    await outbox.RemoveIfUnchangedAsync(record);
                }
            }

            var cursor = await store.GetAsync<DateTimeOffset?>(LocalStores.Meta, PullCursorKey);
            var pulled = await remote.PullAsync(cursor);
            foreach (var record in pulled.Records)
            {
                await ApplyIncomingAsync(record);
            }

            var now = time.GetUtcNow();
            await store.PutAsync(LocalStores.Meta, PullCursorKey, pulled.ServerTime);
            await store.PutAsync(LocalStores.Meta, LastSuccessfulSyncKey, now);
            SetStatus(Status with { LastSuccessfulSync = now, LastError = null });

            if (_offlineSinceLastSync)
            {
                _offlineSinceLastSync = false;
                Notified?.Invoke(new SyncNotification(false, pending.Count switch
                {
                    0 => "Back online – everything is up to date.",
                    1 => "Back online – 1 change synced.",
                    var n => $"Back online – {n} changes synced.",
                }));
            }

            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Sync failed");
            var message = ex is AccessTokenNotAvailableException ? "Sign in to sync." : $"Sync failed: {ex.Message}";
            SetStatus(Status with { LastError = message });
            Notified?.Invoke(new SyncNotification(true, message));
            return false;
        }
        finally
        {
            SetStatus(Status with { IsSyncing = false, PendingChanges = await outbox.CountAsync() });
            _gate.Release();
        }
    }

    private async Task ApplyIncomingAsync(SyncRecord incoming)
    {
        var key = LocalStores.EntityKey(incoming.EntityType, incoming.Id);
        var local = await store.GetAsync<SyncRecord>(LocalStores.Entities, key);
        if (!ConflictResolver.IncomingWins(incoming, local))
        {
            return;
        }

        if (incoming.IsDeleted)
        {
            await store.DeleteAsync(LocalStores.Entities, key);
        }
        else
        {
            await store.PutAsync(LocalStores.Entities, key, incoming);
        }

        // The server copy is newer than any local edit, so a queued local change would be stale.
        await outbox.RemoveAsync(incoming.EntityType, incoming.Id);
    }

    private void OnConnectivityChanged(bool isOnline)
    {
        if (!isOnline)
        {
            _offlineSinceLastSync = true;
        }

        SetStatus(Status with { IsOnline = isOnline });
        if (isOnline)
        {
            _ = SyncAsync();
        }
    }

    private void SetStatus(SyncStatus status)
    {
        Status = status;
        StatusChanged?.Invoke();
    }

    public void Dispose()
    {
        connectivity.Changed -= OnConnectivityChanged;
        _gate.Dispose();
    }
}
