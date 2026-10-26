using System.Security.Claims;
using ChangeAnalyst.Shared.Auth;

namespace ChangeAnalyst.Api.Auth;

public static class UserInfoMapper
{
    public static UserInfo FromPrincipal(ClaimsPrincipal principal) => new(
        Name: principal.FindFirst("name")?.Value ?? principal.FindFirst("preferred_username")?.Value ?? "Unknown",
        ObjectId: principal.FindFirst("oid")?.Value ?? "",
        Roles: [.. principal.FindAll("roles").Select(c => c.Value).Order()]);
}
