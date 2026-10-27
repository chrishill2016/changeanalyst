using System.Text.Json;
using ChangeAnalyst.Services.Storage;

namespace ChangeAnalyst.Tests.Fakes;

/// <summary>Stores JSON strings like the IndexedDB store does, so serialisation is exercised too.</summary>
public sealed class InMemoryLocalStore : ILocalStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Dictionary<string, Dictionary<string, string>> _stores = [];

    public Task<T?> GetAsync<T>(string store, string key) =>
        Task.FromResult(Store(store).TryGetValue(key, out var json) ? JsonSerializer.Deserialize<T>(json, JsonOptions) : default);

    public Task<IReadOnlyList<T>> GetAllAsync<T>(string store) =>
        Task.FromResult<IReadOnlyList<T>>([.. Store(store).Values.Select(json => JsonSerializer.Deserialize<T>(json, JsonOptions)!)]);

    public Task PutAsync<T>(string store, string key, T value)
    {
        Store(store)[key] = JsonSerializer.Serialize(value, JsonOptions);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string store, string key)
    {
        Store(store).Remove(key);
        return Task.CompletedTask;
    }

    private Dictionary<string, string> Store(string name) =>
        _stores.TryGetValue(name, out var store) ? store : _stores[name] = [];
}
