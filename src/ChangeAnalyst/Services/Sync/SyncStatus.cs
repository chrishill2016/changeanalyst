namespace ChangeAnalyst.Services.Sync;

public sealed record SyncStatus(
    bool IsRemoteConfigured,
    bool IsOnline,
    bool IsSyncing,
    int PendingChanges,
    DateTimeOffset? LastSuccessfulSync,
    string? LastError);

public sealed record SyncNotification(bool IsError, string Message);
