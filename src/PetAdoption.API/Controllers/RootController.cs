using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.API.Common;
using PetAdoption.API.Hateoas;

namespace PetAdoption.API.Controllers;

[Route("api")]
[AllowAnonymous]
public sealed class RootController(LinkFactory links) : ApiControllerBase
{
    /// <summary>Ponto de entrada da API: lista os recursos navegáveis (HATEOAS).</summary>
    [HttpGet(Name = RouteNames.Root)]
    [ProducesResponseType<IReadOnlyList<Link>>(StatusCodes.Status200OK)]
    public ActionResult<IReadOnlyList<Link>> Get() => Ok(new[]
    {
        links.Create(RouteNames.Root, null, "self", HttpMethods.Get),
        links.Create(RouteNames.Register, null, "register", HttpMethods.Post),
        links.Create(RouteNames.Login, null, "login", HttpMethods.Post),
        links.Create(RouteNames.Me, null, "profile", HttpMethods.Get),
        links.Create(RouteNames.GetPets, null, "pets", HttpMethods.Get),
        links.Create(RouteNames.GetAbrigos, null, "abrigos", HttpMethods.Get)
    });
}
