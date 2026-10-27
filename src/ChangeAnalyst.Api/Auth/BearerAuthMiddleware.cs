using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace ChangeAnalyst.Api.Auth;

/// <summary>Rejects HTTP calls without a valid Entra access token, except for anonymous functions.</summary>
public sealed class BearerAuthMiddleware(ITokenValidator validator) : IFunctionsWorkerMiddleware
{
    /// <summary>Function names that do not require sign-in.</summary>
    internal static readonly HashSet<string> AnonymousFunctions = [Functions.HealthFunction.Name];

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext is null || AnonymousFunctions.Contains(context.FunctionDefinition.Name))
        {
            await next(context);
            return;
        }

        var principal = await validator.ValidateAsync(GetBearerToken(httpContext.Request), context.CancellationToken);
        if (principal is null)
        {
            httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        httpContext.User = principal;
        await next(context);
    }

    internal static string? GetBearerToken(HttpRequest request)
    {
        var header = request.Headers.Authorization.ToString();
        return header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header["Bearer ".Length..].Trim() : null;
    }
}
