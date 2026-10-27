namespace ChangeAnalyst.Shared.Sync;

/// <summary>
/// A single entity change exchanged between the app and the API.
/// The payload is kept as JSON so the sync pipeline is independent of entity types.
/// </summary>
public sealed record SyncRecord(
    string EntityType,
    Guid Id,
    string Json,
    DateTimeOffset LastModified,
    bool IsDeleted = false);

public sealed record PullResponse(IReadOnlyList<SyncRecord> Records, DateTimeOffset ServerTime);

public sealed record PushRequest(IReadOnlyList<SyncRecord> Records);
