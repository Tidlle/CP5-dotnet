using System.Linq.Expressions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PetAdoption.Application.DTOs;
using PetAdoption.Application.Interfaces;
using PetAdoption.Application.Services;
using PetAdoption.Domain.Common;
using PetAdoption.Domain.Entities;
using PetAdoption.Domain.Errors;
using PetAdoption.Domain.Exceptions;
using PetAdoption.Domain.Repositories;
using PetAdoption.Domain.ValueObjects;

namespace PetAdoption.UnitTests.Application;

public class AbrigoServiceTests
{
    private readonly IAbrigoRepository _abrigoRepository = Substitute.For<IAbrigoRepository>();
    private readonly IPetRepository _petRepository = Substitute.For<IPetRepository>();
    private readonly ICepService _cepService = Substitute.For<ICepService>();
    private readonly AbrigoService _service;

    public AbrigoServiceTests()
    {
        _service = new AbrigoService(_abrigoRepository, _petRepository, _cepService);
        _abrigoRepository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Expression<Func<Abrigo, AbrigoResponse>>>(), Arg.Any<CancellationToken>())
            .Returns(call => OperationResult.Success(new AbrigoResponse(call.Arg<Guid>(), "Abrigo", "11999998888",
                new EnderecoResponse("01001000", "Praça da Sé", "10", null, "Sé", "São Paulo", "SP"), 0, 0, DateTime.UtcNow)));
    }

    private static AbrigoRequest Request(string cep = "01001-000") => new()
    {
        Nome = "Abrigo Central",
        Telefone = "11999998888",
        Cep = cep,
        Numero = "10"
    };

    [Fact]
    public async Task CreateAsync_CepValido_UsaServicoExternoParaPreencherEndereco()
    {
        _cepService.ConsultarAsync("01001-000", Arg.Any<CancellationToken>())
            .Returns(new CepResponse("01001000", "Praça da Sé", null, "Sé", "São Paulo", "SP"));
        Abrigo? persistido = null;
        await _abrigoRepository.AddAsync(Arg.Do<Abrigo>(a => persistido = a), Arg.Any<CancellationToken>());

        var result = await _service.CreateAsync(Request());

        Assert.True(result.IsSuccess);
        Assert.NotNull(persistido);
        Assert.Equal("Praça da Sé", persistido.Endereco.Logradouro);
        Assert.Equal("São Paulo", persistido.Endereco.Cidade);
        Assert.Equal("10", persistido.Endereco.Numero);
    }

    [Fact]
    public async Task CreateAsync_CepNaoEncontrado_RetornaErroSemPersistir()
    {
        _cepService.ConsultarAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(DomainErrors.Cep.NaoEncontrado("99999999"));

        var result = await _service.CreateAsync(Request("99999-999"));

        Assert.True(result.IsFailure);
        Assert.Equal("Cep.NaoEncontrado", result.Error.Code);
        await _abrigoRepository.DidNotReceive().AddAsync(Arg.Any<Abrigo>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_ServicoExternoIndisponivel_RetornaErroExternalService()
    {
        _cepService.ConsultarAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(DomainErrors.Cep.ServicoIndisponivel);

        var result = await _service.CreateAsync(Request());

        Assert.Equal(ErrorType.ExternalService, result.Error.Type);
    }

    [Fact]
    public async Task CreateAsync_NomeDuplicado_PropagaExcecaoLancadaPeloRepositorio()
    {
        _cepService.ConsultarAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new CepResponse("01001000", "Praça da Sé", null, "Sé", "São Paulo", "SP"));
        _abrigoRepository.AddAsync(Arg.Any<Abrigo>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateEntityException(nameof(Abrigo), nameof(Abrigo.Nome), "Abrigo Central"));

        var exception = await Assert.ThrowsAsync<DuplicateEntityException>(() => _service.CreateAsync(Request()));

        Assert.Equal(ErrorType.Conflict, exception.Type);
    }

    [Fact]
    public async Task UpdateAsync_MesmoCep_NaoChamaServicoExterno()
    {
        var endereco = Endereco.Create("01001000", "Praça da Sé", "1", null, "Sé", "São Paulo", "SP").Value;
        var abrigo = Abrigo.Create("Abrigo Central", "11999998888", endereco).Value;
        _abrigoRepository.GetByIdAsync(abrigo.Id, Arg.Any<CancellationToken>()).Returns(abrigo);

        var result = await _service.UpdateAsync(abrigo.Id, Request());

        Assert.True(result.IsSuccess);
        Assert.Equal("10", abrigo.Endereco.Numero);
        await _cepService.DidNotReceive().ConsultarAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_AbrigoComPets_RetornaConflict()
    {
        var id = Guid.NewGuid();
        _petRepository.CountByAbrigoAsync(id, Arg.Any<CancellationToken>()).Returns(3);

        var result = await _service.DeleteAsync(id);

        Assert.True(result.IsFailure);
        Assert.Equal("Abrigo.PossuiPets", result.Error.Code);
        await _abrigoRepository.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetPetsAsync_AbrigoInexistente_RetornaNotFound()
    {
        var id = Guid.NewGuid();
        _abrigoRepository.ExistsAsync(id, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.GetPetsAsync(id, new PetQueryParameters());

        Assert.Equal("Abrigo.NotFound", result.Error.Code);
    }
}
