using PetAdoption.Domain.Common;

namespace PetAdoption.API.Hateoas;

/// <summary>Link de hipermídia: endereço, relação com o recurso atual e verbo HTTP.</summary>
public sealed record Link(string Href, string Rel, string Method);

/// <summary>Recurso individual acompanhado dos seus links.</summary>
public sealed class Resource<T>
{
    public required T Data { get; init; }

    public IReadOnlyList<Link> Links { get; init; } = [];
}

/// <summary>Representação HTTP do <see cref="PageResult{T}"/> com links de navegação entre páginas.</summary>
public sealed class PagedResource<T>
{
    public required IReadOnlyList<Resource<T>> Items { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalItems { get; init; }

    public int TotalPages { get; init; }

    public bool HasPrevious { get; init; }

    public bool HasNext { get; init; }

    public IReadOnlyList<Link> Links { get; init; } = [];
}
