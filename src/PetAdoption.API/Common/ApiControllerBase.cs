using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.Domain.Common;

namespace PetAdoption.API.Common;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    /// <summary>Transforma a falha de um OperationResult na resposta HTTP correspondente.</summary>
    protected ObjectResult HandleFailure(Error error)
    {
        var problem = ProblemDetailsMapper.Create(HttpContext, error, ProblemDetailsMapper.OriginOperationResult);
        return new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { "application/problem+json" }
        };
    }

    protected Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;
}
