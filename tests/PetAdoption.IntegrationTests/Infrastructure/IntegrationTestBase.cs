using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PetAdoption.API.Hateoas;
using PetAdoption.Application.DTOs;
using PetAdoption.Domain.Enums;

namespace PetAdoption.IntegrationTests.Infrastructure;

[Collection(ApiCollection.Name)]
public abstract class IntegrationTestBase(PetAdoptionApiFactory factory)
{
    protected static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    protected PetAdoptionApiFactory Factory { get; } = factory;

    protected HttpClient AnonymousClient() => Factory.CreateClient();

    protected async Task<HttpClient> AdminClientAsync()
    {
        var config = Factory.Services.GetRequiredService<IConfiguration>();
        var token = await LoginAsync(config["Seed:AdminEmail"]!, config["Seed:AdminPassword"]!);
        return ClientWithToken(token);
    }

    protected async Task<HttpClient> UserClientAsync()
    {
        var response = await AnonymousClient().PostAsJsonAsync("/api/auth/register", new
        {
            nome = "Usuário de Teste",
            email = UniqueEmail(),
            senha = "Senha@123"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var auth = await ReadAsync<Resource<AuthResponse>>(response);
        return ClientWithToken(auth.Data.AccessToken);
    }

    protected async Task<string> LoginAsync(string email, string senha)
    {
        var response = await AnonymousClient().PostAsJsonAsync("/api/auth/login", new { email, senha });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await ReadAsync<Resource<AuthResponse>>(response)).Data.AccessToken;
    }

    protected HttpClient ClientWithToken(string token)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected static string UniqueName(string label = "Abrigo") =>
        $"{PetAdoptionApiFactory.TestPrefix}{label} {Guid.NewGuid():N}";

    protected static string UniqueEmail() => $"user{Guid.NewGuid():N}{PetAdoptionApiFactory.TestEmailDomain}";

    protected static async Task<Resource<AbrigoResponse>> CreateAbrigoAsync(HttpClient admin, string? nome = null, string cep = "01001000")
    {
        var response = await admin.PostAsJsonAsync("/api/abrigos", new
        {
            nome = nome ?? UniqueName(),
            telefone = "11999998888",
            cep,
            numero = "100"
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadAsync<Resource<AbrigoResponse>>(response);
    }

    protected static async Task<Resource<PetResponse>> CreatePetAsync(
        HttpClient admin, Guid abrigoId, string nome = "Thor", Especie especie = Especie.Cachorro)
    {
        var response = await admin.PostAsJsonAsync("/api/pets", new
        {
            nome,
            especie = especie.ToString(),
            raca = "SRD",
            idadeMeses = 12,
            abrigoId
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await ReadAsync<Resource<PetResponse>>(response);
    }

    protected static async Task<T> ReadAsync<T>(HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(Json);
        Assert.NotNull(value);
        return value;
    }

    /// <summary>Lê o ProblemDetails e devolve (code, origin).</summary>
    protected static async Task<(string Code, string Origin)> ReadProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (doc.RootElement.GetProperty("code").GetString()!, doc.RootElement.GetProperty("origin").GetString()!);
    }

    protected static void AssertHasLink(IEnumerable<Link> links, string rel, string method)
    {
        Assert.Contains(links, l => l.Rel == rel && l.Method == method && Uri.IsWellFormedUriString(l.Href, UriKind.Absolute));
    }
}
