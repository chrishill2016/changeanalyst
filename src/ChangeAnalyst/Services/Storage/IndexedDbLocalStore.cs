using System.Text.Json;
using Microsoft.JSInterop;

namespace ChangeAnalyst.Services.Storage;

public sealed class IndexedDbLocalStore(IJSRuntime js) : ILocalStore, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly Lazy<Task<IJSObjectReference>> _module = new(
        () => js.InvokeAsync<IJSObjectReference>("import", "./js/localStore.js").AsTask());

    public async Task<T?> GetAsync<T>(string store, string key)
    {
        var module = await _module.Value;
        var json = await module.InvokeAsync<string?>("get", store, key);
        return json is null ? default : JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    public async Task<IReadOnlyList<T>> GetAllAsync<T>(string store)
    {
        var module = await _module.Value;
        var items = await module.InvokeAsync<string[]>("getAll", store);
        return [.. items.Select(json => JsonSerializer.Deserialize<T>(json, JsonOptions)!)];
    }

    public async Task PutAsync<T>(string store, string key, T value)
    {
        var module = await _module.Value;
        await module.InvokeVoidAsync("put", store, key, JsonSerializer.Serialize(value, JsonOptions));
    }

    public async Task DeleteAsync(string store, string key)
    {
        var module = await _module.Value;
        await module.InvokeVoidAsync("remove", store, key);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module.IsValueCreated)
        {
            await (await _module.Value).DisposeAsync();
        }
    }
}
