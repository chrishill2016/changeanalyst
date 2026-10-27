using ChangeAnalyst.Shared.Sync;

namespace ChangeAnalyst.Services.Sync;

/// <summary>Last-write-wins on <see cref="SyncRecord.LastModified"/> (UTC).</summary>
public static class ConflictResolver
{
    /// <summary>
    /// True when a record pulled from the server should replace the local copy.
    /// Ties keep the local copy: it is the same version echoed back, or a local edit not yet pushed.
    /// </summary>
    public static bool IncomingWins(SyncRecord incoming, SyncRecord? local) =>
        local is null || incoming.LastModified > local.LastModified;
}
