using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PetAdoption.Domain.Common;
using PetAdoption.Infrastructure.ExternalServices.ViaCep;
using PetAdoption.UnitTests.Fakes;

namespace PetAdoption.UnitTests.Infrastructure;

public class ViaCepServiceTests
{
    private const string RespostaSe = """
        {"cep":"01001-000","logradouro":"Praça da Sé","complemento":"lado ímpar","bairro":"Sé","localidade":"São Paulo","uf":"SP"}
        """;

    private static ViaCepService CreateService(StubHttpMessageHandler handler, IMemoryCache? cache = null) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://viacep.com.br/") },
            cache ?? new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new ViaCepOptions()),
            NullLogger<ViaCepService>.Instance);

    [Fact]
    public async Task ConsultarAsync_CepExistente_MapeiaRespostaDoServicoExterno()
    {
        var handler = StubHttpMessageHandler.Json(RespostaSe);

        var result = await CreateService(handler).ConsultarAsync("01001-000");

        Assert.True(result.IsSuccess);
        Assert.Equal("01001000", result.Value.Cep);
        Assert.Equal("Praça da Sé", result.Value.Logradouro);
        Assert.Equal("São Paulo", result.Value.Cidade);
        Assert.Equal("SP", result.Value.Uf);
        Assert.Equal("https://viacep.com.br/ws/01001000/json/", handler.LastRequest!.RequestUri!.ToString());
    }

    [Theory]
    [InlineData("""{"erro": true}""")]
    [InlineData("""{"erro": "true"}""")]
    public async Task ConsultarAsync_CepInexistente_RetornaNotFound(string json)
    {
        var result = await CreateService(StubHttpMessageHandler.Json(json)).ConsultarAsync("99999999");

        Assert.True(result.IsFailure);
        Assert.Equal("Cep.NaoEncontrado", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
    }

    [Theory]
    [InlineData("123")]
    [InlineData("abcdefgh")]
    [InlineData("")]
    public async Task ConsultarAsync_FormatoInvalido_NaoChamaServicoExterno(string cep)
    {
        var handler = StubHttpMessageHandler.Json(RespostaSe);

        var result = await CreateService(handler).ConsultarAsync(cep);

        Assert.Equal(ErrorType.Validation, result.Error.Type);
        Assert.Equal(0, handler.Calls);
    }

    [Fact]
    public async Task ConsultarAsync_ErroDeRede_RetornaServicoIndisponivel()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("falha de rede"));

        var result = await CreateService(handler).ConsultarAsync("01001000");

        Assert.Equal("Cep.ServicoIndisponivel", result.Error.Code);
        Assert.Equal(ErrorType.ExternalService, result.Error.Type);
    }

    [Fact]
    public async Task ConsultarAsync_Status500_RetornaServicoIndisponivel()
    {
        var handler = StubHttpMessageHandler.Json("{}", HttpStatusCode.InternalServerError);

        var result = await CreateService(handler).ConsultarAsync("01001000");

        Assert.Equal(ErrorType.ExternalService, result.Error.Type);
    }

    [Fact]
    public async Task ConsultarAsync_SegundaConsulta_UsaCacheSemNovaChamadaHttp()
    {
        var handler = StubHttpMessageHandler.Json(RespostaSe);
        var service = CreateService(handler);

        await service.ConsultarAsync("01001000");
        var second = await service.ConsultarAsync("01001-000");

        Assert.True(second.IsSuccess);
        Assert.Equal(1, handler.Calls);
    }
}
