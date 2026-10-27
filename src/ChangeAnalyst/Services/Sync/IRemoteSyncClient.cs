using ChangeAnalyst.Shared.Sync;

namespace ChangeAnalyst.Services.Sync;

/// <summary>The remote side of sync (the Functions API).</summary>
public interface IRemoteSyncClient
{
    bool IsConfigured { get; }

    Task PushAsync(IReadOnlyList<SyncRecord> records, CancellationToken cancellationToken = default);

    Task<PullResponse> PullAsync(DateTimeOffset? since, CancellationToken cancellationToken = default);
}

/// <summary>Used until the API and sign-in are configured: the app works local-only.</summary>
public sealed class UnconfiguredRemoteSyncClient : IRemoteSyncClient
{
    public bool IsConfigured => false;

    public Task PushAsync(IReadOnlyList<SyncRecord> records, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("The API is not configured.");

    public Task<PullResponse> PullAsync(DateTimeOffset? since, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("The API is not configured.");
}
