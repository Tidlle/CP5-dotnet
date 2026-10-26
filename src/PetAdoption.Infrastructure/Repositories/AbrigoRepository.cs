using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PetAdoption.Domain.Common;
using PetAdoption.Domain.Entities;
using PetAdoption.Domain.Errors;
using PetAdoption.Domain.Exceptions;
using PetAdoption.Domain.Repositories;
using PetAdoption.Infrastructure.Persistence;

namespace PetAdoption.Infrastructure.Repositories;

public sealed class AbrigoRepository(AppDbContext context) : IAbrigoRepository
{
    public async Task<OperationResult<Abrigo>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var abrigo = await context.Abrigos.FirstOrDefaultAsync(a => a.Id == id, ct);

        return abrigo is null
            ? OperationResult.Failure<Abrigo>(DomainErrors.Abrigo.NotFound(id))
            : OperationResult.Success(abrigo);
    }

    public async Task<OperationResult<TResult>> GetByIdAsync<TResult>(
        Guid id, Expression<Func<Abrigo, TResult>> selector, CancellationToken ct = default)
    {
        var result = await context.Abrigos
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(selector)
            .ToListAsync(ct);

        return result.Count == 0
            ? OperationResult.Failure<TResult>(DomainErrors.Abrigo.NotFound(id))
            : OperationResult.Success(result[0]);
    }

    public async Task<PageResult<TResult>> GetPagedAsync<TResult>(
        AbrigoFilter filter, PageRequest page, Expression<Func<Abrigo, TResult>> selector, CancellationToken ct = default)
    {
        var query = context.Abrigos.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Nome))
            query = query.Where(a => a.Nome.Contains(filter.Nome));
        if (!string.IsNullOrWhiteSpace(filter.Cidade))
            query = query.Where(a => a.Endereco.Cidade == filter.Cidade);
        if (!string.IsNullOrWhiteSpace(filter.Uf))
            query = query.Where(a => a.Endereco.Uf == filter.Uf.ToUpper());

        var total = await query.CountAsync(ct);
        if (total == 0)
            return new PageResult<TResult>([], page.Page, page.PageSize, 0);

        var items = await query
            .OrderBy(a => a.Nome)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(selector)
            .ToListAsync(ct);

        return new PageResult<TResult>(items, page.Page, page.PageSize, total);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken ct = default) =>
        context.Abrigos.AnyAsync(a => a.Id == id, ct);

    public async Task AddAsync(Abrigo abrigo, CancellationToken ct = default)
    {
        if (await context.Abrigos.AnyAsync(a => a.Nome == abrigo.Nome, ct))
            throw new DuplicateEntityException(nameof(Abrigo), nameof(Abrigo.Nome), abrigo.Nome);

        context.Abrigos.Add(abrigo);
        await SaveAsync(abrigo, ct);
    }

    public async Task UpdateAsync(Abrigo abrigo, CancellationToken ct = default)
    {
        if (await context.Abrigos.AnyAsync(a => a.Nome == abrigo.Nome && a.Id != abrigo.Id, ct))
            throw new DuplicateEntityException(nameof(Abrigo), nameof(Abrigo.Nome), abrigo.Nome);

        if (context.Entry(abrigo).State == EntityState.Detached)
            context.Abrigos.Update(abrigo);

        await SaveAsync(abrigo, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var affected = await context.Abrigos.Where(a => a.Id == id).ExecuteDeleteAsync(ct);
        if (affected == 0)
            throw new EntityNotFoundException(nameof(Abrigo), id);
    }

    private async Task SaveAsync(Abrigo abrigo, CancellationToken ct)
    {
        try
        {
            await context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueConstraintViolation())
        {
            // Corrida entre a verificação e o INSERT: o índice único do banco garante a regra.
            context.Entry(abrigo).State = EntityState.Detached;
            throw new DuplicateEntityException(nameof(Abrigo), nameof(Abrigo.Nome), abrigo.Nome, ex);
        }
    }
}
