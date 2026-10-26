using PetAdoption.API.Common;
using PetAdoption.Application.DTOs;
using PetAdoption.Domain.Common;
using PetAdoption.Domain.Enums;

namespace PetAdoption.API.Hateoas;

/// <summary>
/// Gera os links HATEOAS a partir das rotas nomeadas. Ações administrativas só aparecem para usuários Admin,
/// e ações que dependem do estado do recurso (ex.: adotar) só aparecem quando são possíveis.
/// </summary>
public sealed class LinkFactory(LinkGenerator linkGenerator, IHttpContextAccessor httpContextAccessor)
{
    private HttpContext HttpContext => httpContextAccessor.HttpContext
        ?? throw new InvalidOperationException("LinkFactory só pode ser usado dentro de uma requisição HTTP.");

    private bool IsAdmin => HttpContext.User.IsInRole(Roles.Admin);

    public Link Create(string routeName, object? values, string rel, string method) =>
        new(linkGenerator.GetUriByName(HttpContext, routeName, values) ?? string.Empty, rel, method);

    public Resource<PetResponse> ForPet(PetResponse pet)
    {
        var links = new List<Link>
        {
            Create(RouteNames.GetPetById, new { id = pet.Id }, "self", HttpMethods.Get),
            Create(RouteNames.GetAbrigoById, new { id = pet.AbrigoId }, "abrigo", HttpMethods.Get),
            Create(RouteNames.GetPets, null, "collection", HttpMethods.Get)
        };

        if (pet.Status == PetStatus.Disponivel)
            links.Add(Create(RouteNames.AdoptPet, new { id = pet.Id }, "adotar", HttpMethods.Post));

        if (IsAdmin)
        {
            links.Add(Create(RouteNames.UpdatePet, new { id = pet.Id }, "update", HttpMethods.Put));
            links.Add(Create(RouteNames.DeletePet, new { id = pet.Id }, "delete", HttpMethods.Delete));
        }

        return new Resource<PetResponse> { Data = pet, Links = links };
    }

    public Resource<AbrigoResponse> ForAbrigo(AbrigoResponse abrigo)
    {
        var links = new List<Link>
        {
            Create(RouteNames.GetAbrigoById, new { id = abrigo.Id }, "self", HttpMethods.Get),
            Create(RouteNames.GetAbrigoPets, new { id = abrigo.Id }, "pets", HttpMethods.Get),
            Create(RouteNames.GetAbrigos, null, "collection", HttpMethods.Get)
        };

        if (IsAdmin)
        {
            links.Add(Create(RouteNames.UpdateAbrigo, new { id = abrigo.Id }, "update", HttpMethods.Put));
            if (abrigo.TotalPets == 0)
                links.Add(Create(RouteNames.DeleteAbrigo, new { id = abrigo.Id }, "delete", HttpMethods.Delete));
            links.Add(Create(RouteNames.CreatePet, null, "create-pet", HttpMethods.Post));
        }

        return new Resource<AbrigoResponse> { Data = abrigo, Links = links };
    }

    public PagedResource<PetResponse> ForPetPage(PageResult<PetResponse> page, PetQueryParameters query, Guid? abrigoId = null)
    {
        var routeName = abrigoId is null ? RouteNames.GetPets : RouteNames.GetAbrigoPets;
        var createLink = IsAdmin ? Create(RouteNames.CreatePet, null, "create", HttpMethods.Post) : null;

        return ForPage(page, ForPet, routeName, createLink, p => new
        {
            id = abrigoId,
            page = p,
            pageSize = page.PageSize,
            nome = query.Nome,
            especie = query.Especie,
            status = query.Status,
            abrigoId = abrigoId is null ? query.AbrigoId : null
        });
    }

    public PagedResource<AbrigoResponse> ForAbrigoPage(PageResult<AbrigoResponse> page, AbrigoQueryParameters query)
    {
        var createLink = IsAdmin ? Create(RouteNames.CreateAbrigo, null, "create", HttpMethods.Post) : null;

        return ForPage(page, ForAbrigo, RouteNames.GetAbrigos, createLink, p => new
        {
            page = p,
            pageSize = page.PageSize,
            nome = query.Nome,
            cidade = query.Cidade,
            uf = query.Uf
        });
    }

    private PagedResource<T> ForPage<T>(
        PageResult<T> page,
        Func<T, Resource<T>> itemMapper,
        string routeName,
        Link? createLink,
        Func<int, object> routeValuesForPage)
    {
        var links = new List<Link>
        {
            Create(routeName, routeValuesForPage(page.Page), "self", HttpMethods.Get),
            Create(routeName, routeValuesForPage(1), "first", HttpMethods.Get)
        };

        if (page.HasPrevious)
            links.Add(Create(routeName, routeValuesForPage(Math.Min(page.Page - 1, Math.Max(page.TotalPages, 1))), "previous", HttpMethods.Get));
        if (page.HasNext)
            links.Add(Create(routeName, routeValuesForPage(page.Page + 1), "next", HttpMethods.Get));

        links.Add(Create(routeName, routeValuesForPage(Math.Max(page.TotalPages, 1)), "last", HttpMethods.Get));

        if (createLink is not null)
            links.Add(createLink);

        return new PagedResource<T>
        {
            Items = page.Items.Select(itemMapper).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalItems = page.TotalItems,
            TotalPages = page.TotalPages,
            HasPrevious = page.HasPrevious,
            HasNext = page.HasNext,
            Links = links
        };
    }
}
