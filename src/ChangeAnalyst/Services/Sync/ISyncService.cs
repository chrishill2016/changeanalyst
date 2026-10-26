using ChangeAnalyst.Shared.Sync;

namespace ChangeAnalyst.Services.Sync;

public interface ISyncService
{
    SyncStatus Status { get; }

    event Action? StatusChanged;

    /// <summary>Raised when a sync fails, or completes after the app was offline.</summary>
    event Action<SyncNotification>? Notified;

    /// <summary>Loads sync state, starts watching connectivity and runs the start-up sync.</summary>
    Task InitializeAsync();

    /// <summary>Writes a change locally and queues it for the server.</summary>
    Task SaveAsync(SyncRecord record);

    /// <summary>Pushes the outbox and pulls server changes. Returns false if skipped or failed.</summary>
    Task<bool> SyncAsync();
}
