using System.Security.Claims;
using System.Security.Cryptography;
using ChangeAnalyst.Api.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace ChangeAnalyst.Api.Tests.Auth;

public class TokenValidatorTests
{
    private const string TenantId = "11111111-1111-1111-1111-111111111111";
    private const string ClientId = "22222222-2222-2222-2222-222222222222";
    private const string Audience = "api://" + ClientId;

    private static readonly RsaSecurityKey SigningKey = new(RSA.Create(2048)) { KeyId = "test-key" };

    private readonly TokenValidator _validator = new(
        Options.Create(new AuthOptions { TenantId = TenantId, Audience = Audience, ClientId = ClientId }),
        new StaticKeys(SigningKey));

    private static string CreateToken(
        string? issuer = null,
        string audience = Audience,
        string scope = AuthOptions.RequiredScope,
        SecurityKey? key = null,
        DateTime? expires = null,
        params string[] roles)
    {
        var claims = new Dictionary<string, object>
        {
            ["name"] = "Ada Lovelace",
            ["oid"] = "33333333-3333-3333-3333-333333333333",
            ["scp"] = scope,
        };
        if (roles.Length > 0)
        {
            claims["roles"] = roles;
        }

        var now = DateTime.UtcNow;
        return new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = issuer ?? $"https://login.microsoftonline.com/{TenantId}/v2.0",
            Audience = audience,
            Claims = claims,
            NotBefore = now.AddMinutes(-10),
            IssuedAt = now.AddMinutes(-10),
            Expires = expires ?? now.AddHours(1),
            SigningCredentials = new SigningCredentials(key ?? SigningKey, SecurityAlgorithms.RsaSha256),
        });
    }

    [Fact]
    public async Task Accepts_a_valid_v2_token()
    {
        var principal = await _validator.ValidateAsync(CreateToken(audience: ClientId));

        Assert.NotNull(principal);
        Assert.Equal("Ada Lovelace", principal.Identity?.Name);
    }

    [Fact]
    public async Task Accepts_a_valid_v1_token()
    {
        var token = CreateToken(issuer: $"https://sts.windows.net/{TenantId}/");

        Assert.NotNull(await _validator.ValidateAsync(token));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-a-jwt")]
    public async Task Rejects_missing_or_malformed_tokens(string? token) =>
        Assert.Null(await _validator.ValidateAsync(token));

    [Fact]
    public async Task Rejects_a_token_for_another_api() =>
        Assert.Null(await _validator.ValidateAsync(CreateToken(audience: "api://someone-else")));

    [Fact]
    public async Task Rejects_a_token_from_another_tenant() =>
        Assert.Null(await _validator.ValidateAsync(CreateToken(issuer: "https://login.microsoftonline.com/other-tenant/v2.0")));

    [Fact]
    public async Task Rejects_a_token_signed_with_an_unknown_key() =>
        Assert.Null(await _validator.ValidateAsync(CreateToken(key: new RsaSecurityKey(RSA.Create(2048)) { KeyId = "other" })));

    [Fact]
    public async Task Rejects_an_expired_token() =>
        Assert.Null(await _validator.ValidateAsync(CreateToken(expires: DateTime.UtcNow.AddMinutes(-6))));

    [Fact]
    public async Task Rejects_a_token_without_the_required_scope() =>
        Assert.Null(await _validator.ValidateAsync(CreateToken(scope: "User.Read")));

    [Fact]
    public async Task Accepts_the_scope_among_several()
    {
        var token = CreateToken(scope: $"User.Read {AuthOptions.RequiredScope}");

        Assert.NotNull(await _validator.ValidateAsync(token));
    }

    [Fact]
    public async Task Maps_the_caller_to_user_info()
    {
        var principal = await _validator.ValidateAsync(CreateToken(roles: ["Submitter", "Analyst"]));

        var user = UserInfoMapper.FromPrincipal(principal!);

        Assert.Equal("Ada Lovelace", user.Name);
        Assert.Equal("33333333-3333-3333-3333-333333333333", user.ObjectId);
        Assert.Equal(["Analyst", "Submitter"], user.Roles);
    }

    private sealed class StaticKeys(SecurityKey key) : ISigningKeyProvider
    {
        public Task<IEnumerable<SecurityKey>> GetSigningKeysAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IEnumerable<SecurityKey>>([key]);
    }
}
