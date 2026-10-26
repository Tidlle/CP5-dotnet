using System.Net;
using System.Net.Http.Json;
using PetAdoption.API.Hateoas;
using PetAdoption.Application.DTOs;
using PetAdoption.Domain.Common;
using PetAdoption.IntegrationTests.Infrastructure;

namespace PetAdoption.IntegrationTests;

public class AuthTests(PetAdoptionApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Root_SemToken_RetornaLinksDeNavegacao()
    {
        var response = await AnonymousClient().GetAsync("/api");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var links = await ReadAsync<List<Link>>(response);
        AssertHasLink(links, "login", "POST");
        AssertHasLink(links, "pets", "GET");
    }

    [Fact]
    public async Task Login_Admin_RetornaTokenJwtComPerfilAdmin()
    {
        var response = await AnonymousClient().PostAsJsonAsync("/api/auth/login",
            new { email = "admin@petadoption.com", senha = "Admin@123" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var auth = await ReadAsync<Resource<AuthResponse>>(response);
        Assert.False(string.IsNullOrWhiteSpace(auth.Data.AccessToken));
        Assert.Equal("Bearer", auth.Data.TokenType);
        Assert.Equal(Roles.Admin, auth.Data.Usuario.Role);
        AssertHasLink(auth.Links, "profile", "GET");
    }

    [Fact]
    public async Task Login_SenhaInvalida_Retorna401ComErroCustomizado()
    {
        var response = await AnonymousClient().PostAsJsonAsync("/api/auth/login",
            new { email = "admin@petadoption.com", senha = "senha-errada" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var (code, origin) = await ReadProblemAsync(response);
        Assert.Equal("Auth.CredenciaisInvalidas", code);
        Assert.Equal("OperationResult", origin);
    }

    [Fact]
    public async Task Register_EmailDuplicado_Retorna409ConvertidoDaExcecaoDoRepositorio()
    {
        var body = new { nome = "Maria", email = UniqueEmail(), senha = "Senha@123" };
        var first = await AnonymousClient().PostAsJsonAsync("/api/auth/register", body);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);

        var second = await AnonymousClient().PostAsJsonAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
        var (code, origin) = await ReadProblemAsync(second);
        Assert.Equal("Usuario.Duplicate", code);
        Assert.Equal("OperationResult", origin);
    }

    [Fact]
    public async Task Me_ComToken_RetornaUsuarioAutenticado()
    {
        var client = await UserClientAsync();

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var me = await ReadAsync<Resource<UsuarioResponse>>(response);
        Assert.Equal(Roles.User, me.Data.Role);
    }

    [Theory]
    [InlineData("/api/pets")]
    [InlineData("/api/abrigos")]
    [InlineData("/api/auth/me")]
    [InlineData("/api/cep/01001000")]
    public async Task EndpointProtegido_SemToken_Retorna401(string url)
    {
        var response = await AnonymousClient().GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        var (code, _) = await ReadProblemAsync(response);
        Assert.Equal("Auth.NaoAutenticado", code);
    }

    [Fact]
    public async Task TokenInvalido_Retorna401()
    {
        var response = await ClientWithToken("token.invalido.123").GetAsync("/api/pets");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UsuarioComum_EndpointDeAdmin_Retorna403()
    {
        var client = await UserClientAsync();

        var response = await client.PostAsJsonAsync("/api/abrigos",
            new { nome = UniqueName(), telefone = "11999998888", cep = "01001000" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var (code, _) = await ReadProblemAsync(response);
        Assert.Equal("Auth.AcessoNegado", code);
    }

    [Fact]
    public async Task UsuarioComum_PodeListarPets_SemLinksAdministrativos()
    {
        var client = await UserClientAsync();

        var response = await client.GetAsync("/api/pets?pageSize=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var page = await ReadAsync<PagedResource<PetResponse>>(response);
        Assert.All(page.Items, item =>
        {
            Assert.DoesNotContain(item.Links, l => l.Rel is "update" or "delete");
        });
        Assert.DoesNotContain(page.Links, l => l.Rel == "create");
    }
}
