using ChangeAnalyst;
using ChangeAnalyst.Services;
using ChangeAnalyst.Services.Api;
using ChangeAnalyst.Services.Connectivity;
using ChangeAnalyst.Services.Pwa;
using ChangeAnalyst.Services.Storage;
using ChangeAnalyst.Services.Sync;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiOptions = builder.Configuration.GetSection("Api").Get<ApiOptions>() ?? new ApiOptions();
var config = new AppConfig(
    AuthConfigured: !string.IsNullOrWhiteSpace(builder.Configuration["AzureAd:ClientId"]),
    ApiConfigured: apiOptions.IsConfigured);

builder.Services.AddSingleton(config);
builder.Services.AddSingleton(apiOptions);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddMudServices();

// Sign-in (Entra ID). Until a client id is configured the app runs signed-out and local-only.
builder.Services.AddCascadingAuthenticationState();
if (config.AuthConfigured)
{
    builder.Services.AddMsalAuthentication(options =>
    {
        builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
        // localStorage keeps the user signed in across restarts, including offline.
        options.ProviderOptions.Cache.CacheLocation = "localStorage";
        options.ProviderOptions.LoginMode = "redirect";
        if (apiOptions.IsConfigured)
        {
            options.ProviderOptions.DefaultAccessTokenScopes.Add(apiOptions.Scope);
        }
    });
}
else
{
    builder.Services.AddAuthorizationCore();
    builder.Services.AddScoped<AuthenticationStateProvider, AnonymousAuthenticationStateProvider>();
}

// API access.
if (config.RemoteEnabled)
{
    builder.Services.AddScoped<ApiAuthorizationMessageHandler>();
    builder.Services.AddHttpClient(ApiHttpClient.Name, client => client.BaseAddress = new Uri(apiOptions.BaseUrl))
        .AddHttpMessageHandler<ApiAuthorizationMessageHandler>();
    builder.Services.AddScoped<IRemoteSyncClient, HttpRemoteSyncClient>();
    builder.Services.AddScoped<IApiClient, HttpApiClient>();
}
else
{
    builder.Services.AddScoped<IRemoteSyncClient, UnconfiguredRemoteSyncClient>();
    builder.Services.AddScoped<IApiClient, UnconfiguredApiClient>();
}

// Offline-first storage and sync.
builder.Services.AddScoped<ILocalStore, IndexedDbLocalStore>();
builder.Services.AddScoped<IOutbox, Outbox>();
builder.Services.AddScoped<IConnectivityService, BrowserConnectivityService>();
builder.Services.AddScoped<ISyncService, SyncService>();
builder.Services.AddScoped<IUpdateService, BrowserUpdateService>();
builder.Services.AddScoped<ThemeService>();

await builder.Build().RunAsync();
