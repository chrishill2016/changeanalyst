using ChangeAnalyst.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;

namespace ChangeAnalyst.Services.Api;

public static class ApiHttpClient
{
    public const string Name = "Api";
}

/// <summary>Attaches an access token for the API's scope to calls to the API's base URL.</summary>
public sealed class ApiAuthorizationMessageHandler : AuthorizationMessageHandler
{
    public ApiAuthorizationMessageHandler(IAccessTokenProvider provider, NavigationManager navigation, ApiOptions options)
        : base(provider, navigation)
    {
        ConfigureHandler(authorizedUrls: [options.BaseUrl], scopes: [options.Scope]);
    }
}
