using System.Linq.Expressions;
using PetAdoption.Domain.Common;
using PetAdoption.Domain.Entities;
using PetAdoption.Domain.Exceptions;

namespace PetAdoption.Domain.Repositories;

public interface IPetRepository
{
    /// <summary>Busca rastreada (para alteração). Retorna erro <c>Pet.NotFound</c> via OperationResult.</summary>
    Task<OperationResult<Pet>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Busca somente leitura com projeção. Retorna erro <c>Pet.NotFound</c> via OperationResult.</summary>
    Task<OperationResult<TResult>> GetByIdAsync<TResult>(Guid id, Expression<Func<Pet, TResult>> selector, CancellationToken ct = default);

    Task<PageResult<TResult>> GetPagedAsync<TResult>(PetFilter filter, PageRequest page, Expression<Func<Pet, TResult>> selector, CancellationToken ct = default);

    Task<int> CountByAbrigoAsync(Guid abrigoId, CancellationToken ct = default);

    Task AddAsync(Pet pet, CancellationToken ct = default);

    Task UpdateAsync(Pet pet, CancellationToken ct = default);

    /// <exception cref="EntityNotFoundException">Quando o pet não existe.</exception>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
