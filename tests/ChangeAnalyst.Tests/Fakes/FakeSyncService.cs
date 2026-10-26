using ChangeAnalyst.Services.Sync;
using ChangeAnalyst.Shared.Sync;

namespace ChangeAnalyst.Tests.Fakes;

public sealed class FakeSyncService : ISyncService
{
    public SyncStatus Status { get; private set; } = new(true, true, false, 0, null, null);

    public event Action? StatusChanged;

    public event Action<SyncNotification>? Notified;

    public int SyncCalls { get; private set; }

    public void SetStatus(SyncStatus status)
    {
        Status = status;
        StatusChanged?.Invoke();
    }

    public void Notify(SyncNotification notification) => Notified?.Invoke(notification);

    public Task InitializeAsync() => Task.CompletedTask;

    public Task SaveAsync(SyncRecord record) => Task.CompletedTask;

    public Task<bool> SyncAsync()
    {
        SyncCalls++;
        return Task.FromResult(true);
    }
}
