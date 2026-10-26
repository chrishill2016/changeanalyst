using System.Net.Http.Json;
using ChangeAnalyst.Services.Api;
using ChangeAnalyst.Shared.Sync;

namespace ChangeAnalyst.Services.Sync;

public sealed class HttpRemoteSyncClient(IHttpClientFactory httpClientFactory) : IRemoteSyncClient
{
    public bool IsConfigured => true;

    public async Task PushAsync(IReadOnlyList<SyncRecord> records, CancellationToken cancellationToken = default)
    {
        using var response = await Client.PostAsJsonAsync("api/sync/push", new PushRequest(records), cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<PullResponse> PullAsync(DateTimeOffset? since, CancellationToken cancellationToken = default)
    {
        var url = since is null ? "api/sync/pull" : $"api/sync/pull?since={Uri.EscapeDataString(since.Value.ToString("O"))}";
        return await Client.GetFromJsonAsync<PullResponse>(url, cancellationToken)
            ?? throw new InvalidOperationException("Empty sync response.");
    }

    private HttpClient Client => httpClientFactory.CreateClient(ApiHttpClient.Name);
}
