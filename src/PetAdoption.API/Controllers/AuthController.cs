using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.API.Common;
using PetAdoption.API.Hateoas;
using PetAdoption.Application.DTOs;
using PetAdoption.Application.Services;

namespace PetAdoption.API.Controllers;

[Route("api/auth")]
public sealed class AuthController(IAuthService authService, LinkFactory links) : ApiControllerBase
{
    /// <summary>Cria uma conta com perfil "User" e devolve o token JWT.</summary>
    [HttpPost("register", Name = RouteNames.Register)]
    [AllowAnonymous]
    [ProducesResponseType<Resource<AuthResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Resource<AuthResponse>>> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await authService.RegisterAsync(request, ct);
        if (result.IsFailure)
            return HandleFailure(result.Error);

        return CreatedAtRoute(RouteNames.Me, null, WithLinks(result.Value));
    }

    /// <summary>Autentica e devolve o token JWT (Bearer).</summary>
    [HttpPost("login", Name = RouteNames.Login)]
    [AllowAnonymous]
    [ProducesResponseType<Resource<AuthResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Resource<AuthResponse>>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        return result.IsSuccess ? Ok(WithLinks(result.Value)) : HandleFailure(result.Error);
    }

    /// <summary>Dados do usuário autenticado.</summary>
    [HttpGet("me", Name = RouteNames.Me)]
    [Authorize]
    [ProducesResponseType<Resource<UsuarioResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Resource<UsuarioResponse>>> Me(CancellationToken ct)
    {
        var result = await authService.GetProfileAsync(CurrentUserId, ct);
        if (result.IsFailure)
            return HandleFailure(result.Error);

        return Ok(new Resource<UsuarioResponse>
        {
            Data = result.Value,
            Links =
            [
                links.Create(RouteNames.Me, null, "self", HttpMethods.Get),
                links.Create(RouteNames.GetPets, null, "pets", HttpMethods.Get),
                links.Create(RouteNames.GetAbrigos, null, "abrigos", HttpMethods.Get)
            ]
        });
    }

    private Resource<AuthResponse> WithLinks(AuthResponse response) => new()
    {
        Data = response,
        Links =
        [
            links.Create(RouteNames.Me, null, "profile", HttpMethods.Get),
            links.Create(RouteNames.GetPets, null, "pets", HttpMethods.Get),
            links.Create(RouteNames.GetAbrigos, null, "abrigos", HttpMethods.Get)
        ]
    };
}
