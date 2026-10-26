using PetAdoption.Application.DTOs;
using PetAdoption.Application.Interfaces;
using PetAdoption.Application.Mappings;
using PetAdoption.Domain.Common;
using PetAdoption.Domain.Entities;
using PetAdoption.Domain.Errors;
using PetAdoption.Domain.Repositories;
using PetAdoption.Domain.ValueObjects;

namespace PetAdoption.Application.Services;

public interface IAbrigoService
{
    Task<PageResult<AbrigoResponse>> GetPagedAsync(AbrigoQueryParameters query, CancellationToken ct = default);

    Task<OperationResult<AbrigoResponse>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<OperationResult<PageResult<PetResponse>>> GetPetsAsync(Guid abrigoId, PetQueryParameters query, CancellationToken ct = default);

    Task<OperationResult<AbrigoResponse>> CreateAsync(AbrigoRequest request, CancellationToken ct = default);

    Task<OperationResult<AbrigoResponse>> UpdateAsync(Guid id, AbrigoRequest request, CancellationToken ct = default);

    Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed class AbrigoService(
    IAbrigoRepository abrigoRepository,
    IPetRepository petRepository,
    ICepService cepService) : IAbrigoService
{
    public Task<PageResult<AbrigoResponse>> GetPagedAsync(AbrigoQueryParameters query, CancellationToken ct = default)
    {
        var filter = new AbrigoFilter(query.Nome, query.Cidade, query.Uf);
        return abrigoRepository.GetPagedAsync(filter, PageRequest.Create(query.Page, query.PageSize), Projections.AbrigoToResponse, ct);
    }

    public Task<OperationResult<AbrigoResponse>> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        abrigoRepository.GetByIdAsync(id, Projections.AbrigoToResponse, ct);

    public async Task<OperationResult<PageResult<PetResponse>>> GetPetsAsync(
        Guid abrigoId, PetQueryParameters query, CancellationToken ct = default)
    {
        if (!await abrigoRepository.ExistsAsync(abrigoId, ct))
            return DomainErrors.Abrigo.NotFound(abrigoId);

        var filter = new PetFilter(query.Nome, query.Especie, query.Status, abrigoId);
        return await petRepository.GetPagedAsync(filter, PageRequest.Create(query.Page, query.PageSize), Projections.PetToResponse, ct);
    }

    public async Task<OperationResult<AbrigoResponse>> CreateAsync(AbrigoRequest request, CancellationToken ct = default)
    {
        // Chamada ao serviço externo (ViaCEP) para completar o endereço.
        var cep = await cepService.ConsultarAsync(request.Cep, ct);
        if (cep.IsFailure)
            return cep.Error;

        var endereco = Endereco.Create(cep.Value.Cep, cep.Value.Logradouro, request.Numero, request.Complemento,
            cep.Value.Bairro, cep.Value.Cidade, cep.Value.Uf);
        if (endereco.IsFailure)
            return endereco.Error;

        var abrigo = Abrigo.Create(request.Nome, request.Telefone, endereco.Value);
        if (abrigo.IsFailure)
            return abrigo.Error;

        // DuplicateEntityException (nome repetido) é lançada pelo repositório e tratada pelo handler global (409).
        await abrigoRepository.AddAsync(abrigo.Value, ct);
        return await GetByIdAsync(abrigo.Value.Id, ct);
    }

    public async Task<OperationResult<AbrigoResponse>> UpdateAsync(Guid id, AbrigoRequest request, CancellationToken ct = default)
    {
        var abrigoResult = await abrigoRepository.GetByIdAsync(id, ct);
        if (abrigoResult.IsFailure)
            return abrigoResult.Error;

        var abrigo = abrigoResult.Value;
        var endereco = await ResolveEnderecoAsync(abrigo.Endereco, request, ct);
        if (endereco.IsFailure)
            return endereco.Error;

        var update = abrigo.Update(request.Nome, request.Telefone, endereco.Value);
        if (update.IsFailure)
            return update.Error;

        await abrigoRepository.UpdateAsync(abrigo, ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<OperationResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var totalPets = await petRepository.CountByAbrigoAsync(id, ct);
        if (totalPets > 0)
            return DomainErrors.Abrigo.PossuiPets(totalPets);

        // EntityNotFoundException é lançada pelo repositório quando o abrigo não existe.
        await abrigoRepository.DeleteAsync(id, ct);
        return OperationResult.Success();
    }

    private async Task<OperationResult<Endereco>> ResolveEnderecoAsync(
        Endereco atual, AbrigoRequest request, CancellationToken ct)
    {
        var cepDigits = new string(request.Cep.Where(char.IsDigit).ToArray());
        if (cepDigits == atual.Cep)
        {
            return Endereco.Create(atual.Cep, atual.Logradouro, request.Numero, request.Complemento,
                atual.Bairro, atual.Cidade, atual.Uf);
        }

        var cep = await cepService.ConsultarAsync(request.Cep, ct);
        if (cep.IsFailure)
            return cep.Error;

        return Endereco.Create(cep.Value.Cep, cep.Value.Logradouro, request.Numero, request.Complemento,
            cep.Value.Bairro, cep.Value.Cidade, cep.Value.Uf);
    }
}
