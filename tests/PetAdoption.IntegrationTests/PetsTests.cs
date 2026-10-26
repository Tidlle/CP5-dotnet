using System.Net;
using System.Net.Http.Json;
using PetAdoption.API.Hateoas;
using PetAdoption.Application.DTOs;
using PetAdoption.Domain.Enums;
using PetAdoption.IntegrationTests.Infrastructure;

namespace PetAdoption.IntegrationTests;

public class PetsTests(PetAdoptionApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Create_EGetById_RetornaPetComLinksHateoas()
    {
        var admin = await AdminClientAsync();
        var abrigo = await CreateAbrigoAsync(admin);
        var created = await CreatePetAsync(admin, abrigo.Data.Id, "Paçoca", Especie.Gato);

        var response = await admin.GetAsync($"/api/pets/{created.Data.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pet = await ReadAsync<Resource<PetResponse>>(response);
        Assert.Equal("Paçoca", pet.Data.Nome);
        Assert.Equal(Especie.Gato, pet.Data.Especie);
        Assert.Equal(PetStatus.Disponivel, pet.Data.Status);
        Assert.Equal(abrigo.Data.Nome, pet.Data.AbrigoNome);
        AssertHasLink(pet.Links, "self", "GET");
        AssertHasLink(pet.Links, "abrigo", "GET");
        AssertHasLink(pet.Links, "adotar", "POST");
        AssertHasLink(pet.Links, "update", "PUT");
        AssertHasLink(pet.Links, "delete", "DELETE");
    }

    [Fact]
    public async Task Create_AbrigoInexistente_Retorna404()
    {
        var admin = await AdminClientAsync();

        var response = await admin.PostAsJsonAsync("/api/pets",
            new { nome = "Rex", especie = "Cachorro", idadeMeses = 10, abrigoId = Guid.NewGuid() });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var (code, _) = await ReadProblemAsync(response);
        Assert.Equal("Abrigo.NotFound", code);
    }

    [Fact]
    public async Task GetById_Inexistente_Retorna404ComErroRetornadoPeloRepositorio()
    {
        var admin = await AdminClientAsync();

        var response = await admin.GetAsync($"/api/pets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var (code, origin) = await ReadProblemAsync(response);
        Assert.Equal("Pet.NotFound", code);
        Assert.Equal("OperationResult", origin);
    }

    [Fact]
    public async Task Delete_Admin_Retorna204_EPetInexistenteRetorna404ViaExcecao()
    {
        var admin = await AdminClientAsync();
        var abrigo = await CreateAbrigoAsync(admin);
        var pet = await CreatePetAsync(admin, abrigo.Data.Id);

        var first = await admin.DeleteAsync($"/api/pets/{pet.Data.Id}");
        var second = await admin.DeleteAsync($"/api/pets/{pet.Data.Id}");

        Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
        var (code, origin) = await ReadProblemAsync(second);
        Assert.Equal("Pet.NotFound", code);
        Assert.Equal("Exception", origin);
    }

    [Fact]
    public async Task Adotar_UsuarioComum_AdotaPet_ESegundaAdocaoRetorna422()
    {
        var admin = await AdminClientAsync();
        var abrigo = await CreateAbrigoAsync(admin);
        var pet = await CreatePetAsync(admin, abrigo.Data.Id);
        var user = await UserClientAsync();

        var first = await user.PostAsync($"/api/pets/{pet.Data.Id}/adocao", null);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var adotado = await ReadAsync<Resource<PetResponse>>(first);
        Assert.Equal(PetStatus.Adotado, adotado.Data.Status);
        Assert.NotNull(adotado.Data.DataAdocao);
        Assert.DoesNotContain(adotado.Links, l => l.Rel == "adotar");

        var second = await user.PostAsync($"/api/pets/{pet.Data.Id}/adocao", null);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, second.StatusCode);
        var (code, _) = await ReadProblemAsync(second);
        Assert.Equal("Pet.JaAdotado", code);
    }

    [Fact]
    public async Task Update_Admin_AtualizaDados()
    {
        var admin = await AdminClientAsync();
        var abrigo = await CreateAbrigoAsync(admin);
        var pet = await CreatePetAsync(admin, abrigo.Data.Id, "Nome Antigo");

        var response = await admin.PutAsJsonAsync($"/api/pets/{pet.Data.Id}",
            new { nome = "Nome Novo", especie = "Coelho", raca = "Angorá", idadeMeses = 7, abrigoId = abrigo.Data.Id });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await ReadAsync<Resource<PetResponse>>(response);
        Assert.Equal("Nome Novo", updated.Data.Nome);
        Assert.Equal(Especie.Coelho, updated.Data.Especie);
        Assert.Equal(7, updated.Data.IdadeMeses);
    }

    [Fact]
    public async Task GetAll_ComFiltros_RetornaPaginaFiltradaComLinks()
    {
        var admin = await AdminClientAsync();
        var abrigo = await CreateAbrigoAsync(admin);
        await CreatePetAsync(admin, abrigo.Data.Id, "Gato A", Especie.Gato);
        await CreatePetAsync(admin, abrigo.Data.Id, "Gato B", Especie.Gato);
        await CreatePetAsync(admin, abrigo.Data.Id, "Cão C", Especie.Cachorro);

        var response = await admin.GetAsync($"/api/pets?abrigoId={abrigo.Data.Id}&especie=Gato&pageSize=1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await ReadAsync<PagedResource<PetResponse>>(response);
        Assert.Equal(2, page.TotalItems);
        Assert.Equal(2, page.TotalPages);
        Assert.Single(page.Items);
        Assert.Equal(Especie.Gato, page.Items[0].Data.Especie);

        var next = page.Links.Single(l => l.Rel == "next");
        Assert.Contains("especie=Gato", next.Href);
        Assert.Contains($"abrigoId={abrigo.Data.Id}", next.Href);
    }
}
