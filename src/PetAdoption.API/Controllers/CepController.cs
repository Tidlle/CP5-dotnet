using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.API.Common;
using PetAdoption.API.Hateoas;
using PetAdoption.Application.DTOs;
using PetAdoption.Application.Interfaces;
using PetAdoption.Domain.Common;

namespace PetAdoption.API.Controllers;

[Route("api/cep")]
[Authorize]
public sealed class CepController(ICepService cepService, LinkFactory links) : ApiControllerBase
{
    /// <summary>Consulta um CEP no serviço externo ViaCEP (com retry, timeout, circuit breaker e cache).</summary>
    [HttpGet("{cep}", Name = RouteNames.GetCep)]
    [ProducesResponseType<Resource<CepResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<Resource<CepResponse>>> Get(string cep, CancellationToken ct)
    {
        var result = await cepService.ConsultarAsync(cep, ct);
        if (result.IsFailure)
            return HandleFailure(result.Error);

        var resourceLinks = new List<Link>
        {
            links.Create(RouteNames.GetCep, new { cep = result.Value.Cep }, "self", HttpMethods.Get),
            links.Create(RouteNames.GetAbrigos, new { cidade = result.Value.Cidade }, "abrigos-na-cidade", HttpMethods.Get)
        };

        if (User.IsInRole(Roles.Admin))
            resourceLinks.Add(links.Create(RouteNames.CreateAbrigo, null, "create-abrigo", HttpMethods.Post));

        return Ok(new Resource<CepResponse> { Data = result.Value, Links = resourceLinks });
    }
}
