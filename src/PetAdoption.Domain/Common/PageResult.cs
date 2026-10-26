namespace PetAdoption.Domain.Common;

/// <summary>
/// Resultado paginado padronizado: dados da página + metadados (página, itens, registros e total de páginas).
/// </summary>
public sealed class PageResult<T>
{
    public PageResult(IReadOnlyList<T> items, int page, int pageSize, int totalItems)
    {
        Items = items;
        Page = page;
        PageSize = pageSize;
        TotalItems = totalItems;
    }

    public IReadOnlyList<T> Items { get; }

    public int Page { get; }

    public int PageSize { get; }

    public int TotalItems { get; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize);

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;

    public PageResult<TOut> Map<TOut>(Func<T, TOut> map) =>
        new(Items.Select(map).ToList(), Page, PageSize, TotalItems);

    public static PageResult<T> Empty(PageRequest request) => new([], request.Page, request.PageSize, 0);
}

/// <summary>
/// Parâmetros de paginação normalizados (página mínima 1, tamanho entre 1 e <see cref="MaxPageSize"/>).
/// </summary>
public readonly record struct PageRequest
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 50;

    private PageRequest(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }

    public int Page { get; }

    public int PageSize { get; }

    public int Skip => (Page - 1) * PageSize;

    public static PageRequest Create(int? page, int? pageSize)
    {
        var normalizedPage = page is null or < 1 ? 1 : page.Value;
        var normalizedSize = pageSize switch
        {
            null or < 1 => DefaultPageSize,
            > MaxPageSize => MaxPageSize,
            _ => pageSize.Value
        };

        return new PageRequest(normalizedPage, normalizedSize);
    }
}
