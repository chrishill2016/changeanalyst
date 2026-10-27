using Microsoft.JSInterop;

namespace ChangeAnalyst.Services.Connectivity;

public sealed class BrowserConnectivityService(IJSRuntime js) : IConnectivityService, IAsyncDisposable
{
    private DotNetObjectReference<BrowserConnectivityService>? _selfRef;
    private IJSObjectReference? _module;

    public bool IsOnline { get; private set; } = true;

    public event Action<bool>? Changed;

    public async Task InitializeAsync()
    {
        if (_module is not null)
        {
            return;
        }

        _module = await js.InvokeAsync<IJSObjectReference>("import", "./js/pwa.js");
        _selfRef = DotNetObjectReference.Create(this);
        IsOnline = await _module.InvokeAsync<bool>("watchConnectivity", _selfRef);
    }

    [JSInvokable]
    public void OnConnectivityChanged(bool isOnline)
    {
        if (isOnline == IsOnline)
        {
            return;
        }

        IsOnline = isOnline;
        Changed?.Invoke(isOnline);
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
