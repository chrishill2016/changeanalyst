namespace ChangeAnalyst.Services;

/// <summary>Which external pieces are configured in appsettings.json.</summary>
public sealed record AppConfig(bool AuthConfigured, bool ApiConfigured)
{
    public bool RemoteEnabled => AuthConfigured && ApiConfigured;
}

public sealed class ApiOptions
{
    public string BaseUrl { get; set; } = "";

    /// <summary>Delegated scope on the API registration, e.g. <c>api://&lt;guid&gt;/access_as_user</c>.</summary>
    public string Scope { get; set; } = "";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(BaseUrl) && !string.IsNullOrWhiteSpace(Scope);
}
