using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace ChangeAnalyst.Api.Auth;

public interface ISigningKeyProvider
{
    Task<IEnumerable<SecurityKey>> GetSigningKeysAsync(CancellationToken cancellationToken);
}

/// <summary>Fetches (and caches) the tenant's token signing keys from Entra ID metadata.</summary>
public sealed class EntraSigningKeyProvider(IOptions<AuthOptions> options) : ISigningKeyProvider
{
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _configuration = new(
        $"https://login.microsoftonline.com/{options.Value.TenantId}/v2.0/.well-known/openid-configuration",
        new OpenIdConnectConfigurationRetriever(),
        new HttpDocumentRetriever());

    public async Task<IEnumerable<SecurityKey>> GetSigningKeysAsync(CancellationToken cancellationToken) =>
        (await _configuration.GetConfigurationAsync(cancellationToken)).SigningKeys;
}
