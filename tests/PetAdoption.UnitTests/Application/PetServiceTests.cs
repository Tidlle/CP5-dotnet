using System.Linq.Expressions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PetAdoption.Application.DTOs;
using PetAdoption.Application.Services;
using PetAdoption.Domain.Common;
using PetAdoption.Domain.Entities;
using PetAdoption.Domain.Enums;
using PetAdoption.Domain.Errors;
using PetAdoption.Domain.Exceptions;
using PetAdoption.Domain.Repositories;
using PetAdoption.UnitTests.Fakes;

namespace PetAdoption.UnitTests.Application;

public class PetServiceTests
{
    private static readonly DateTimeOffset Agora = new(2026, 10, 20, 15, 0, 0, TimeSpan.Zero);

    private readonly IPetRepository _petRepository = Substitute.For<IPetRepository>();
    private readonly IAbrigoRepository _abrigoRepository = Substitute.For<IAbrigoRepository>();
    private readonly PetService _service;

    public PetServiceTests()
    {
        _service = new PetService(_petRepository, _abrigoRepository, new FixedTimeProvider(Agora));
    }

    private static PetRequest Request(Guid abrigoId, string nome = "Thor") => new()
    {
        Nome = nome,
        Especie = Especie.Cachorro,
        Raca = "Labrador",
        IdadeMeses = 24,
        AbrigoId = abrigoId
    };

    private void ConfigureProjection(OperationResult<PetResponse> result) =>
        _petRepository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<Expression<Func<Pet, PetResponse>>>(), Arg.Any<CancellationToken>())
            .Returns(result);

    [Fact]
    public async Task CreateAsync_AbrigoInexistente_RetornaNotFoundSemPersistir()
    {
        var abrigoId = Guid.NewGuid();
        _abrigoRepository.ExistsAsync(abrigoId, Arg.Any<CancellationToken>()).Returns(false);

        var result = await _service.CreateAsync(Request(abrigoId));

        Assert.True(result.IsFailure);
        Assert.Equal("Abrigo.NotFound", result.Error.Code);
        Assert.Equal(ErrorType.NotFound, result.Error.Type);
        await _petRepository.DidNotReceive().AddAsync(Arg.Any<Pet>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_NomeInvalido_RetornaErroDeValidacao()
    {
        var abrigoId = Guid.NewGuid();
        _abrigoRepository.ExistsAsync(abrigoId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await _service.CreateAsync(Request(abrigoId, nome: " "));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Validation, result.Error.Type);
        await _petRepository.DidNotReceive().AddAsync(Arg.Any<Pet>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_DadosValidos_PersisteERetornaPet()
    {
        var abrigoId = Guid.NewGuid();
        _abrigoRepository.ExistsAsync(abrigoId, Arg.Any<CancellationToken>()).Returns(true);
        var response = new PetResponse(Guid.NewGuid(), "Thor", Especie.Cachorro, "Labrador", 24,
            PetStatus.Disponivel, abrigoId, "Abrigo", null, Agora.UtcDateTime);
        ConfigureProjection(response);

        var result = await _service.CreateAsync(Request(abrigoId));

        Assert.True(result.IsSuccess);
        Assert.Equal("Thor", result.Value.Nome);
        await _petRepository.Received(1).AddAsync(
            Arg.Is<Pet>(p => p.Nome == "Thor" && p.AbrigoId == abrigoId), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_RepositorioRetornaErro_PropagaErroCustomizado()
    {
        var id = Guid.NewGuid();
        ConfigureProjection(DomainErrors.Pet.NotFound(id));

        var result = await _service.GetByIdAsync(id);

        Assert.True(result.IsFailure);
        Assert.Equal("Pet.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task AdotarAsync_PetDisponivel_RegistraAdocaoComDataAtual()
    {
        var pet = Pet.Create("Luna", Especie.Gato, null, 12, Guid.NewGuid()).Value;
        var adotante = Guid.NewGuid();
        _petRepository.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);
        ConfigureProjection(new PetResponse(pet.Id, pet.Nome, pet.Especie, null, 12, PetStatus.Adotado,
            pet.AbrigoId, "Abrigo", Agora.UtcDateTime, pet.CreatedAt));

        var result = await _service.AdotarAsync(pet.Id, adotante);

        Assert.True(result.IsSuccess);
        Assert.Equal(PetStatus.Adotado, pet.Status);
        Assert.Equal(Agora.UtcDateTime, pet.DataAdocao);
        Assert.Equal(adotante, pet.AdotanteId);
        await _petRepository.Received(1).UpdateAsync(pet, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AdotarAsync_PetJaAdotado_RetornaErroDeRegraDeNegocio()
    {
        var pet = Pet.Create("Luna", Especie.Gato, null, 12, Guid.NewGuid()).Value;
        pet.Adotar(Guid.NewGuid(), DateTime.UtcNow);
        _petRepository.GetByIdAsync(pet.Id, Arg.Any<CancellationToken>()).Returns(pet);

        var result = await _service.AdotarAsync(pet.Id, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.BusinessRule, result.Error.Type);
        await _petRepository.DidNotReceive().UpdateAsync(Arg.Any<Pet>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_RepositorioLancaEntityNotFound_PropagaExcecaoCustomizada()
    {
        var id = Guid.NewGuid();
        _petRepository.DeleteAsync(id, Arg.Any<CancellationToken>())
            .ThrowsAsync(new EntityNotFoundException(nameof(Pet), id));

        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => _service.DeleteAsync(id));

        Assert.Equal("Pet.NotFound", exception.Code);
        Assert.Equal(ErrorType.NotFound, exception.Type);
    }

    [Fact]
    public async Task GetPagedAsync_RepassaFiltrosEPaginacaoNormalizada()
    {
        var abrigoId = Guid.NewGuid();
        var query = new PetQueryParameters { Page = 2, PageSize = 500, Especie = Especie.Gato, AbrigoId = abrigoId };
        _petRepository
            .GetPagedAsync(Arg.Any<PetFilter>(), Arg.Any<PageRequest>(), Arg.Any<Expression<Func<Pet, PetResponse>>>(), Arg.Any<CancellationToken>())
            .Returns(new PageResult<PetResponse>([], 2, PageRequest.MaxPageSize, 0));

        await _service.GetPagedAsync(query);

        await _petRepository.Received(1).GetPagedAsync(
            Arg.Is<PetFilter>(f => f.Especie == Especie.Gato && f.AbrigoId == abrigoId),
            Arg.Is<PageRequest>(p => p.Page == 2 && p.PageSize == PageRequest.MaxPageSize),
            Arg.Any<Expression<Func<Pet, PetResponse>>>(),
            Arg.Any<CancellationToken>());
    }
}
