namespace ChangeAnalyst.Api.Auth;

/// <summary>Entra ID settings for validating access tokens. App settings: Auth__TenantId, Auth__Audience, Auth__ClientId.</summary>
public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    /// <summary>The scope the web app requests; tokens without it are rejected.</summary>
    public const string RequiredScope = "access_as_user";

    public string TenantId { get; set; } = "";

    /// <summary>The API's Application ID URI, e.g. <c>api://&lt;guid&gt;</c>.</summary>
    public string Audience { get; set; } = "";

    /// <summary>The API registration's client id (v2 tokens use this as the audience).</summary>
    public string ClientId { get; set; } = "";

    /// <summary>Issuers for v1 and v2 tokens from this tenant.</summary>
    public IReadOnlyList<string> ValidIssuers =>
    [
        $"https://login.microsoftonline.com/{TenantId}/v2.0",
        $"https://sts.windows.net/{TenantId}/",
    ];

    public IReadOnlyList<string> ValidAudiences =>
        [.. new[] { Audience, ClientId }.Where(a => !string.IsNullOrWhiteSpace(a))];
}
