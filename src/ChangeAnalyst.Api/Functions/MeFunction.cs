using ChangeAnalyst.Api.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace ChangeAnalyst.Api.Functions;

public sealed class MeFunction
{
    [Function("Me")]
    public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "me")] HttpRequest request) =>
        new OkObjectResult(UserInfoMapper.FromPrincipal(request.HttpContext.User));
}
