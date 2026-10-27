using ChangeAnalyst.Services.Storage;
using ChangeAnalyst.Shared.Sync;

namespace ChangeAnalyst.Services.Sync;

public sealed class Outbox(ILocalStore store) : IOutbox
{
    public Task EnqueueAsync(SyncRecord record) =>
        store.PutAsync(LocalStores.Outbox, LocalStores.EntityKey(record.EntityType, record.Id), record);

    public async Task<IReadOnlyList<SyncRecord>> GetPendingAsync() =>
        [.. (await store.GetAllAsync<SyncRecord>(LocalStores.Outbox)).OrderBy(r => r.LastModified)];

    public async Task<int> CountAsync() => (await store.GetAllAsync<SyncRecord>(LocalStores.Outbox)).Count;

    public async Task RemoveIfUnchangedAsync(SyncRecord pushed)
    {
        var key = LocalStores.EntityKey(pushed.EntityType, pushed.Id);
        var current = await store.GetAsync<SyncRecord>(LocalStores.Outbox, key);
        if (current is not null && current.LastModified <= pushed.LastModified)
        {
            await store.DeleteAsync(LocalStores.Outbox, key);
        }
    }

    public Task RemoveAsync(string entityType, Guid id) =>
        store.DeleteAsync(LocalStores.Outbox, LocalStores.EntityKey(entityType, id));
}
