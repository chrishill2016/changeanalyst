using Microsoft.JSInterop;

namespace ChangeAnalyst.Services.Pwa;

public sealed class BrowserUpdateService(IJSRuntime js) : IUpdateService, IAsyncDisposable
{
    private DotNetObjectReference<BrowserUpdateService>? _selfRef;
    private IJSObjectReference? _module;

    public bool IsUpdateAvailable { get; private set; }

    public event Action? UpdateAvailable;

    public async Task InitializeAsync()
    {
        if (_module is not null)
        {
            return;
        }

        _module = await js.InvokeAsync<IJSObjectReference>("import", "./js/pwa.js");
        _selfRef = DotNetObjectReference.Create(this);
        await _module.InvokeVoidAsync("watchUpdates", _selfRef);
    }

    [JSInvokable]
    public void OnUpdateAvailable()
    {
        IsUpdateAvailable = true;
        UpdateAvailable?.Invoke();
    }

    public async Task ApplyUpdateAsync()
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("applyUpdate");
        }
    }

    public async ValueTask DisposeAsync()
    {
        _selfRef?.Dispose();
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }
    }
}
