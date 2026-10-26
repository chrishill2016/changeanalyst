namespace ChangeAnalyst.Services.Storage;

/// <summary>Key/value storage on the device (IndexedDB in the browser).</summary>
public interface ILocalStore
{
    Task<T?> GetAsync<T>(string store, string key);
    Task<IReadOnlyList<T>> GetAllAsync<T>(string store);
    Task PutAsync<T>(string store, string key, T value);
    Task DeleteAsync(string store, string key);
}

public static class LocalStores
{
    /// <summary>Synced entities, keyed by <c>{EntityType}:{Id}</c>.</summary>
    public const string Entities = "entities";

    /// <summary>Changes waiting to be pushed, keyed like <see cref="Entities"/>.</summary>
    public const string Outbox = "outbox";

    /// <summary>App settings and sync bookkeeping.</summary>
    public const string Meta = "meta";

    public static string EntityKey(string entityType, Guid id) => $"{entityType}:{id}";
}
