using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ChangeAnalyst.Api.Auth;

public interface ITokenValidator
{
    /// <summary>Returns the caller, or null if the token is missing, invalid or lacks the required scope.</summary>
    Task<ClaimsPrincipal?> ValidateAsync(string? bearerToken, CancellationToken cancellationToken = default);
}

public sealed class TokenValidator(IOptions<AuthOptions> options, ISigningKeyProvider keys) : ITokenValidator
{
    private readonly JsonWebTokenHandler _handler = new();

    public async Task<ClaimsPrincipal?> ValidateAsync(string? bearerToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            return null;
        }

        var parameters = new TokenValidationParameters
        {
            ValidIssuers = options.Value.ValidIssuers,
            ValidAudiences = options.Value.ValidAudiences,
            IssuerSigningKeys = await keys.GetSigningKeysAsync(cancellationToken),
            NameClaimType = "name",
            RoleClaimType = "roles",
        };

        var result = await _handler.ValidateTokenAsync(bearerToken, parameters);
        if (!result.IsValid)
        {
            return null;
        }

        var principal = new ClaimsPrincipal(result.ClaimsIdentity);
        return HasRequiredScope(principal) ? principal : null;
    }

    private static bool HasRequiredScope(ClaimsPrincipal principal) =>
        principal.FindAll("scp")
            .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Contains(AuthOptions.RequiredScope);
}
