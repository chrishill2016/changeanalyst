using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace ChangeAnalyst.Services;

/// <summary>Used when sign-in is not configured yet: everyone is anonymous.</summary>
public sealed class AnonymousAuthenticationStateProvider : AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity())));
}
