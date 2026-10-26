using PetAdoption.Domain.Common;

namespace PetAdoption.UnitTests.Domain;

public class PageResultTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(1, 10, 1)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(95, 20, 5)]
    public void TotalPages_DeveSerCalculadoPeloTotalDeItens(int totalItems, int pageSize, int expectedPages)
    {
        var page = new PageResult<int>([], 1, pageSize, totalItems);

        Assert.Equal(expectedPages, page.TotalPages);
    }

    [Fact]
    public void Navegacao_PaginaDoMeio_TemAnteriorEProxima()
    {
        var page = new PageResult<int>([4, 5, 6], 2, 3, 9);

        Assert.True(page.HasPrevious);
        Assert.True(page.HasNext);
    }

    [Fact]
    public void Navegacao_UltimaPagina_NaoTemProxima()
    {
        var page = new PageResult<int>([7, 8, 9], 3, 3, 9);

        Assert.True(page.HasPrevious);
        Assert.False(page.HasNext);
    }

    [Fact]
    public void Map_PreservaMetadados()
    {
        var page = new PageResult<int>([1, 2], 1, 2, 5).Map(i => $"item {i}");

        Assert.Equal(["item 1", "item 2"], page.Items);
        Assert.Equal(5, page.TotalItems);
        Assert.Equal(3, page.TotalPages);
    }

    [Theory]
    [InlineData(null, null, 1, PageRequest.DefaultPageSize)]
    [InlineData(0, 0, 1, PageRequest.DefaultPageSize)]
    [InlineData(-3, 500, 1, PageRequest.MaxPageSize)]
    [InlineData(4, 25, 4, 25)]
    public void PageRequest_NormalizaValores(int? page, int? size, int expectedPage, int expectedSize)
    {
        var request = PageRequest.Create(page, size);

        Assert.Equal(expectedPage, request.Page);
        Assert.Equal(expectedSize, request.PageSize);
        Assert.Equal((expectedPage - 1) * expectedSize, request.Skip);
    }
}
