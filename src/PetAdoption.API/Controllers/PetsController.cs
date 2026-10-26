using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PetAdoption.API.Common;
using PetAdoption.API.Hateoas;
using PetAdoption.Application.DTOs;
using PetAdoption.Application.Services;
using PetAdoption.Domain.Common;

namespace PetAdoption.API.Controllers;

[Route("api/pets")]
[Authorize]
public sealed class PetsController(IPetService petService, LinkFactory links) : ApiControllerBase
{
    /// <summary>Lista pets com paginação (PageResult), filtros e links HATEOAS.</summary>
    [HttpGet(Name = RouteNames.GetPets)]
    [ProducesResponseType<PagedResource<PetResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResource<PetResponse>>> GetAll([FromQuery] PetQueryParameters query, CancellationToken ct)
    {
        var page = await petService.GetPagedAsync(query, ct);
        return Ok(links.ForPetPage(page, query));
    }

    /// <summary>Busca um pet pelo id.</summary>
    [HttpGet("{id:guid}", Name = RouteNames.GetPetById)]
    [ProducesResponseType<Resource<PetResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Resource<PetResponse>>> GetById(Guid id, CancellationToken ct)
    {
        var result = await petService.GetByIdAsync(id, ct);
        return result.IsSuccess ? Ok(links.ForPet(result.Value)) : HandleFailure(result.Error);
    }

    /// <summary>Cadastra um pet (somente Admin).</summary>
    [HttpPost(Name = RouteNames.CreatePet)]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<Resource<PetResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Resource<PetResponse>>> Create([FromBody] PetRequest request, CancellationToken ct)
    {
        var result = await petService.CreateAsync(request, ct);
        if (result.IsFailure)
            return HandleFailure(result.Error);

        return CreatedAtRoute(RouteNames.GetPetById, new { id = result.Value.Id }, links.ForPet(result.Value));
    }

    /// <summary>Atualiza um pet (somente Admin).</summary>
    [HttpPut("{id:guid}", Name = RouteNames.UpdatePet)]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType<Resource<PetResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Resource<PetResponse>>> Update(Guid id, [FromBody] PetRequest request, CancellationToken ct)
    {
        var result = await petService.UpdateAsync(id, request, ct);
        return result.IsSuccess ? Ok(links.ForPet(result.Value)) : HandleFailure(result.Error);
    }

    /// <summary>Adota um pet em nome do usuário autenticado. Retorna 422 se o pet já foi adotado.</summary>
    [HttpPost("{id:guid}/adocao", Name = RouteNames.AdoptPet)]
    [ProducesResponseType<Resource<PetResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<Resource<PetResponse>>> Adotar(Guid id, CancellationToken ct)
    {
        var result = await petService.AdotarAsync(id, CurrentUserId, ct);
        return result.IsSuccess ? Ok(links.ForPet(result.Value)) : HandleFailure(result.Error);
    }

    /// <summary>Remove um pet (somente Admin). Pet inexistente gera exceção customizada tratada globalmente (404).</summary>
    [HttpDelete("{id:guid}", Name = RouteNames.DeletePet)]
    [Authorize(Roles = Roles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var result = await petService.DeleteAsync(id, ct);
        return result.IsSuccess ? NoContent() : HandleFailure(result.Error);
    }
}
