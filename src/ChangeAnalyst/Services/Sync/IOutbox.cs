using ChangeAnalyst.Shared.Sync;

namespace ChangeAnalyst.Services.Sync;

/// <summary>Local changes waiting to be pushed. One entry per entity; a newer change replaces an older one.</summary>
public interface IOutbox
{
    Task EnqueueAsync(SyncRecord record);

    Task<IReadOnlyList<SyncRecord>> GetPendingAsync();

    Task<int> CountAsync();

    /// <summary>Removes the entry after a push, unless it was edited again while the push was in flight.</summary>
    Task RemoveIfUnchangedAsync(SyncRecord pushed);

    Task RemoveAsync(string entityType, Guid id);
}
