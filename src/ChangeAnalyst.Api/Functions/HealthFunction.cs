using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace ChangeAnalyst.Api.Functions;

public sealed class HealthFunction
{
    public const string Name = "Health";

    private static readonly string Version =
        typeof(HealthFunction).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

    [Function(Name)]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequest request) =>
        new OkObjectResult(new { status = "ok", version = Version });
}
