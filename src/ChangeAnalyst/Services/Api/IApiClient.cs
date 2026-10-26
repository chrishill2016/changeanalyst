using System.Net.Http.Json;
using ChangeAnalyst.Shared.Auth;

namespace ChangeAnalyst.Services.Api;

public interface IApiClient
{
    bool IsConfigured { get; }

    /// <summary>The signed-in user as the API sees them (name, object id, app roles).</summary>
    Task<UserInfo> GetMeAsync(CancellationToken cancellationToken = default);
}

public sealed class HttpApiClient(IHttpClientFactory httpClientFactory) : IApiClient
{
    public bool IsConfigured => true;

    public async Task<UserInfo> GetMeAsync(CancellationToken cancellationToken = default) =>
        await httpClientFactory.CreateClient(ApiHttpClient.Name).GetFromJsonAsync<UserInfo>("api/me", cancellationToken)
        ?? throw new InvalidOperationException("Empty response from the API.");
}

public sealed class UnconfiguredApiClient : IApiClient
{
    public bool IsConfigured => false;

    public Task<UserInfo> GetMeAsync(CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("The API is not configured.");
}
