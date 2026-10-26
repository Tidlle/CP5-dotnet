using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.API.Common;
using PetAdoption.API.Hateoas;
using PetAdoption.Application.DTOs;
using PetAdoption.Application.Services;
using PetAdoption.Domain.Common;

namespace PetAdoption.API.Controllers;

[Route("api/abrigos")]
[Authorize]
public sealed class AbrigosController(IAbrigoService abrigoService, LinkFactory links) : ApiControllerBase
{
    /// <summary>Lista abrigos com paginação (PageResult), filtros e links HATEOAS.</summary>
    [HttpGet(Name = RouteNames.GetAbrigos)]
    [ProducesResponseType<PagedResource<AbrigoResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResource<AbrigoResponse>>> GetAll([FromQuery] AbrigoQueryParameters query, CancellationToken ct)
    {
        var page = await abrigoService.GetPagedAsync(query, ct);
        return Ok(links.ForAbrigoPage(page, query));
    }

    [HttpGet("{id:guid}", Name = RouteNames.GetAbrigoById)]
    [ProducesResponseType<Resource<AbrigoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Resource<AbrigoResponse>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await abrigoService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(links.ForAbrigo(result.Value)) : HandleFailure(result.Error);
    }

    /// <summary>Lista os pets de um abrigo (paginado).</summary>
    [HttpGet("{id:guid}/pets", Name = RouteNames.GetAbrigoPets)]
    [ProducesResponseType<PagedResource<PetResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PagedResource<PetResponse>>> GetPets(Guid id, [FromQuery] PetQueryParameters query, CancellationToken ct)
    {
        var result = await abrigoService.GetPetsAsync(id, query, ct);
        return result.IsSuccess ? Ok(links.ForPetPage(result.Value, query, id)) : HandleFailure(result.Error);
    }

    /// <summary>Cadastra um abrigo (somente Admin). O endereço é preenchido pelo serviço externo ViaCEP.</summary>
    [HttpPost(Name = RouteNames.CreateAbrigo)]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<Resource<AbrigoResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<Resource<AbrigoResponse>>> Create([FromBody] AbrigoRequest request, CancellationToken ct)
    {
        var result = await abrigoService.CreateAsync(request, ct);
        if (result.IsFailure)
            return HandleFailure(result.Error);

        return CreatedAtRoute(RouteNames.GetAbrigoById, new { id = result.Value.Id }, links.ForAbrigo(result.Value));
    }

    [HttpPut("{id:guid}", Name = RouteNames.UpdateAbrigo)]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<Resource<AbrigoResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Resource<AbrigoResponse>>> Update(Guid id, [FromBody] AbrigoRequest request, CancellationToken ct)
    {
        var result = await abrigoService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(links.ForAbrigo(result.Value)) : HandleFailure(result.Error);
    }

    /// <summary>Remove um abrigo sem pets (somente Admin).</summary>
    [HttpDelete("{id:guid}", Name = RouteNames.DeleteAbrigo)]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await abrigoService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : HandleFailure(result.Error);
    }
}
