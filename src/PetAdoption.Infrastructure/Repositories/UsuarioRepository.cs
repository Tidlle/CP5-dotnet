using Microsoft.EntityFrameworkCore;
using PetAdoption.Domain.Common;
using PetAdoption.Domain.Entities;
using PetAdoption.Domain.Errors;
using PetAdoption.Domain.Exceptions;
using PetAdoption.Domain.Repositories;
using PetAdoption.Infrastructure.Persistence;

namespace PetAdoption.Infrastructure.Repositories;

public sealed class UsuarioRepository(AppDbContext context) : IUsuarioRepository
{
    // Consulta compilada: o login é o caminho mais quente da API, então a tradução LINQ -> SQL é feita uma única vez.
    private static readonly Func<AppDbContext, string, CancellationToken, Task<Usuario?>> GetByEmailQuery =
        EF.CompileAsyncQuery((AppDbContext db, string email, CancellationToken ct) =>
            db.Usuarios.AsNoTracking().FirstOrDefault(u => u.Email == email));

    public async Task<OperationResult<Usuario>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var usuario = await context.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id, ct);

        return usuario is null
            ? OperationResult.Failure<Usuario>(DomainErrors.Usuario.NotFound(id))
            : OperationResult.Success(usuario);
    }

    public async Task<OperationResult<Usuario>> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = Usuario.NormalizeEmail(email);
        var usuario = await GetByEmailQuery(context, normalized, ct);

        return usuario is null
            ? OperationResult.Failure<Usuario>(DomainErrors.Usuario.EmailNotFound(normalized))
            : OperationResult.Success(usuario);
    }

    public async Task AddAsync(Usuario usuario, CancellationToken ct = default)
    {
        if (await context.Usuarios.AnyAsync(u => u.Email == usuario.Email, ct))
            throw new DuplicateEntityException(nameof(Usuario), nameof(Usuario.Email), usuario.Email);

        context.Usuarios.Add(usuario);
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            context.Entry(usuario).State = EntityState.Detached;
            throw new DuplicateEntityException(nameof(Usuario), nameof(Usuario.Email), usuario.Email, ex);
        }
    }
}
