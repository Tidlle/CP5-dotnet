using System.Net;
using System.Net.Http.Json;
using PetAdoption.API.Hateoas;
using PetAdoption.Application.DTOs;
using PetAdoption.IntegrationTests.Infrastructure;

namespace PetAdoption.IntegrationTests;

public class AbrigosTests(PetAdoptionApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Create_Admin_Retorna201ComEnderecoDoServicoExternoELinks()
    {
        var admin = await AdminClientAsync();
        var nome = UniqueName();

        var response = await admin.PostAsJsonAsync("/api/abrigos",
            new { nome, telefone = "(11) 99999-8888", cep = "01310-100", numero = "1578" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var abrigo = await ReadAsync<Resource<AbrigoResponse>>(response);
        Assert.Equal(nome, abrigo.Data.Nome);
        Assert.Equal("01310100", abrigo.Data.Endereco.Cep);
        Assert.Equal(FakeCepService.Cidade, abrigo.Data.Endereco.Cidade);
        Assert.Equal("1578", abrigo.Data.Endereco.Numero);
        AssertHasLink(abrigo.Links, "self", "GET");
        AssertHasLink(abrigo.Links, "pets", "GET");
        AssertHasLink(abrigo.Links, "update", "PUT");
        AssertHasLink(abrigo.Links, "delete", "DELETE");
    }

    [Fact]
    public async Task Create_NomeDuplicado_Retorna409ViaExcecaoLancadaNoRepositorio()
    {
        var admin = await AdminClientAsync();
        var nome = UniqueName();
        await CreateAbrigoAsync(admin, nome);

        var response = await admin.PostAsJsonAsync("/api/abrigos",
            new { nome, telefone = "11999998888", cep = "01001000" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var (code, origin) = await ReadProblemAsync(response);
        Assert.Equal("Abrigo.Duplicate", code);
        Assert.Equal("Exception", origin);
    }

    [Theory]
    [InlineData(FakeCepService.CepInexistente, HttpStatusCode.NotFound, "Cep.NaoEncontrado")]
    [InlineData(FakeCepService.CepServicoIndisponivel, HttpStatusCode.ServiceUnavailable, "Cep.ServicoIndisponivel")]
    public async Task Create_FalhaNoServicoExterno_RetornaErroCustomizado(string cep, HttpStatusCode status, string expectedCode)
    {
        var admin = await AdminClientAsync();

        var response = await admin.PostAsJsonAsync("/api/abrigos",
            new { nome = UniqueName(), telefone = "11999998888", cep });

        Assert.Equal(status, response.StatusCode);
        var (code, origin) = await ReadProblemAsync(response);
        Assert.Equal(expectedCode, code);
        Assert.Equal("OperationResult", origin);
    }

    [Fact]
    public async Task Create_DadosInvalidos_Retorna400()
    {
        var admin = await AdminClientAsync();

        var response = await admin.PostAsJsonAsync("/api/abrigos", new { nome = "", telefone = "", cep = "abc" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_Paginado_RetornaPageResultComLinksDeNavegacao()
    {
        var admin = await AdminClientAsync();
        var marcador = Guid.NewGuid().ToString("N")[..8];
        for (var i = 1; i <= 3; i++)
            await CreateAbrigoAsync(admin, $"{PetAdoptionApiFactory.TestPrefix}Pag {marcador} {i}");

        var response = await admin.GetAsync($"/api/abrigos?nome={marcador}&page=1&pageSize=2");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await ReadAsync<PagedResource<AbrigoResponse>>(response);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(1, page.Page);
        Assert.Equal(2, page.PageSize);
        Assert.Equal(3, page.TotalItems);
        Assert.Equal(2, page.TotalPages);
        Assert.True(page.HasNext);
        Assert.False(page.HasPrevious);
        AssertHasLink(page.Links, "self", "GET");
        AssertHasLink(page.Links, "next", "GET");
        AssertHasLink(page.Links, "last", "GET");
        AssertHasLink(page.Links, "create", "POST");
        Assert.DoesNotContain(page.Links, l => l.Rel == "previous");

        var next = page.Links.Single(l => l.Rel == "next");
        Assert.Contains("page=2", next.Href);

        var secondPage = await ReadAsync<PagedResource<AbrigoResponse>>(await admin.GetAsync(next.Href));
        Assert.Single(secondPage.Items);
        Assert.False(secondPage.HasNext);
        AssertHasLink(secondPage.Links, "previous", "GET");
    }

    [Fact]
    public async Task GetById_Inexistente_Retorna404()
    {
        var admin = await AdminClientAsync();

        var response = await admin.GetAsync($"/api/abrigos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var (code, _) = await ReadProblemAsync(response);
        Assert.Equal("Abrigo.NotFound", code);
    }

    [Fact]
    public async Task Delete_AbrigoComPets_Retorna409()
    {
        var admin = await AdminClientAsync();
        var abrigo = await CreateAbrigoAsync(admin);
        await CreatePetAsync(admin, abrigo.Data.Id);

        var response = await admin.DeleteAsync($"/api/abrigos/{abrigo.Data.Id}");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var (code, _) = await ReadProblemAsync(response);
        Assert.Equal("Abrigo.PossuiPets", code);
    }

    [Fact]
    public async Task Delete_AbrigoSemPets_Retorna204()
    {
        var admin = await AdminClientAsync();
        var abrigo = await CreateAbrigoAsync(admin);

        var response = await admin.DeleteAsync($"/api/abrigos/{abrigo.Data.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/api/abrigos/{abrigo.Data.Id}")).StatusCode);
    }

    [Fact]
    public async Task GetPets_RetornaSomentePetsDoAbrigo()
    {
        var admin = await AdminClientAsync();
        var abrigo = await CreateAbrigoAsync(admin);
        await CreatePetAsync(admin, abrigo.Data.Id, "Bidu");
        await CreatePetAsync(admin, abrigo.Data.Id, "Amora");

        var response = await admin.GetAsync($"/api/abrigos/{abrigo.Data.Id}/pets");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await ReadAsync<PagedResource<PetResponse>>(response);
        Assert.Equal(2, page.TotalItems);
        Assert.All(page.Items, p => Assert.Equal(abrigo.Data.Id, p.Data.AbrigoId));
        Assert.Equal(["Amora", "Bidu"], page.Items.Select(p => p.Data.Nome));
    }
}
