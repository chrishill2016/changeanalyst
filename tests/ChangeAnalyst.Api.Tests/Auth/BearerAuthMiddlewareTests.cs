using ChangeAnalyst.Api.Auth;
using Microsoft.AspNetCore.Http;

namespace ChangeAnalyst.Api.Tests.Auth;

public class BearerAuthMiddlewareTests
{
    [Theory]
    [InlineData("Bearer abc.def.ghi", "abc.def.ghi")]
    [InlineData("bearer abc", "abc")]
    [InlineData("Basic dXNlcjpwYXNz", null)]
    [InlineData("", null)]
    public void Reads_the_bearer_token_from_the_authorization_header(string header, string? expected)
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = header;

        Assert.Equal(expected, BearerAuthMiddleware.GetBearerToken(context.Request));
    }

    [Fact]
    public void Only_the_health_check_is_anonymous() =>
        Assert.Equal(["Health"], BearerAuthMiddleware.AnonymousFunctions);
}
