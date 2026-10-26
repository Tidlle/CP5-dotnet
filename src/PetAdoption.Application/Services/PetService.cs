using PetAdoption.Application.DTOs;
using PetAdoption.Application.Mappings;
using PetAdoption.Domain.Common;
using PetAdoption.Domain.Entities;
using PetAdoption.Domain.Errors;
using PetAdoption.Domain.Repositories;

namespace PetAdoption.Application.Services;

public interface IPetService
{
    Task<PageResult<PetResponse>> GetPagedAsync(PetQueryParameters query, CancellationToken ct = default);

    Task<OperationResult<PetResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<OperationResult<PetResponse>> CreateAsync(PetRequest request, CancellationToken ct = default);

    Task<OperationResult<PetResponse>> UpdateAsync(Guid id, PetRequest request, CancellationToken ct = default);

    Task<OperationResult<PetResponse>> AdotarAsync(Guid id, Guid adotanteId, CancellationToken ct = default);

    Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed class PetService(
    IPetRepository petRepository,
    IAbrigoRepository abrigoRepository,
    TimeProvider timeProvider) : IPetService
{
    public Task<PageResult<PetResponse>> GetPagedAsync(PetQueryParameters query, CancellationToken ct = default)
    {
        var filter = new PetFilter(query.Nome, query.Especie, query.Status, query.AbrigoId);
        return petRepository.GetPagedAsync(filter, PageRequest.Create(query.Page, query.PageSize), Projections.PetToResponse, ct);
    }

    // Erro "Pet.NotFound" chega aqui como retorno do repositório (OperationResult) e é repassado ao controller.
    public Task<OperationResult<PetResponse>> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        petRepository.GetByIdAsync(id, Projections.PetToResponse, ct);

    public async Task<OperationResult<PetResponse>> CreateAsync(PetRequest request, CancellationToken ct = default)
    {
        if (!await abrigoRepository.ExistsAsync(request.AbrigoId, ct))
            return DomainErrors.Abrigo.NotFound(request.AbrigoId);

        var petResult = Pet.Create(request.Nome, request.Especie, request.Raca, request.IdadeMeses, request.AbrigoId);
        if (petResult.IsFailure)
            return petResult.Error;

        await petRepository.AddAsync(petResult.Value, ct);
        return await GetByIdAsync(petResult.Value.Id, ct);
    }

    public async Task<OperationResult<PetResponse>> UpdateAsync(Guid id, PetRequest request, CancellationToken ct = default)
    {
        var petResult = await petRepository.GetByIdAsync(id, ct);
        if (petResult.IsFailure)
            return petResult.Error;

        var pet = petResult.Value;
        if (pet.AbrigoId != request.AbrigoId && !await abrigoRepository.ExistsAsync(request.AbrigoId, ct))
            return DomainErrors.Abrigo.NotFound(request.AbrigoId);

        var update = pet.Update(request.Nome, request.Especie, request.Raca, request.IdadeMeses, request.AbrigoId);
        if (update.IsFailure)
            return update.Error;

        await petRepository.UpdateAsync(pet, ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<OperationResult<PetResponse>> AdotarAsync(Guid id, Guid adotanteId, CancellationToken ct = default)
    {
        var petResult = await petRepository.GetByIdAsync(id, ct);
        if (petResult.IsFailure)
            return petResult.Error;

        var pet = petResult.Value;
        var adocao = pet.Adotar(adotanteId, timeProvider.GetUtcNow().UtcDateTime);
        if (adocao.IsFailure)
            return adocao.Error;

        await petRepository.UpdateAsync(pet, ct);
        return await GetByIdAsync(id, ct);
    }

    // O repositório LANÇA EntityNotFoundException quando o pet não existe; a exceção não é capturada aqui
    // e é convertida em 404 (ProblemDetails) pelo GlobalExceptionHandler da API.
    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await petRepository.DeleteAsync(id, ct);
        return OperationResult.Success();
    }
}
