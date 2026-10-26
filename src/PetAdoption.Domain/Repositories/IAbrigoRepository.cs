using System.Linq.Expressions;
using PetAdoption.Domain.Common;
using PetAdoption.Domain.Entities;
using PetAdoption.Domain.Exceptions;

namespace PetAdoption.Domain.Repositories;

public interface IAbrigoRepository
{
    /// <summary>Busca rastreada (para alteração). Retorna erro <c>Abrigo.NotFound</c> via OperationResult.</summary>
    Task<OperationResult<Abrigo>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Busca somente leitura com projeção. Retorna erro <c>Abrigo.NotFound</c> via OperationResult.</summary>
    Task<OperationResult<TResult>> GetByIdAsync<TResult>(Guid id, Expression<Func<Abrigo, TResult>> selector, CancellationToken ct = default);

    Task<PageResult<TResult>> GetPagedAsync<TResult>(AbrigoFilter filter, PageRequest page, Expression<Func<Abrigo, TResult>> selector, CancellationToken ct = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken ct = default);

    /// <exception cref="DuplicateEntityException">Quando já existe abrigo com o mesmo nome.</exception>
    Task AddAsync(Abrigo abrigo, CancellationToken ct = default);

    /// <exception cref="DuplicateEntityException">Quando já existe outro abrigo com o mesmo nome.</exception>
    Task UpdateAsync(Abrigo abrigo, CancellationToken ct = default);

    /// <exception cref="EntityNotFoundException">Quando o abrigo não existe.</exception>
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
