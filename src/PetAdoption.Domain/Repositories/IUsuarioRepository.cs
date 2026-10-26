using PetAdoption.Domain.Common;
using PetAdoption.Domain.Entities;
using PetAdoption.Domain.Exceptions;

namespace PetAdoption.Domain.Repositories;

public interface IUsuarioRepository
{
    /// <summary>Retorna erro <c>Usuario.NotFound</c> via OperationResult.</summary>
    Task<OperationResult<Usuario>> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Retorna erro <c>Usuario.NotFound</c> via OperationResult.</summary>
    Task<OperationResult<Usuario>> GetByEmailAsync(string email, CancellationToken ct = default);

    /// <exception cref="DuplicateEntityException">Quando o e-mail já está cadastrado.</exception>
    Task AddAsync(Usuario usuario, CancellationToken ct = default);
}
