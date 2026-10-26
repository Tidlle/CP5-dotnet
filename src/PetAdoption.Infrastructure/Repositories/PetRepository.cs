using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PetAdoption.Domain.Common;
using PetAdoption.Domain.Entities;
using PetAdoption.Domain.Errors;
using PetAdoption.Domain.Exceptions;
using PetAdoption.Domain.Repositories;
using PetAdoption.Infrastructure.Persistence;

namespace PetAdoption.Infrastructure.Repositories;

public sealed class PetRepository(AppDbContext context) : IPetRepository
{
    public async Task<OperationResult<Pet>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var pet = await context.Pets.FirstOrDefaultAsync(p => p.Id == id, ct);

        // Erro customizado RETORNADO pelo repositório.
        return pet is null
            ? OperationResult.Failure<Pet>(DomainErrors.Pet.NotFound(id))
            : OperationResult.Success(pet);
    }

    public async Task<OperationResult<TResult>> GetByIdAsync<TResult>(
        Guid id, Expression<Func<Pet, TResult>> selector, CancellationToken ct = default)
    {
        var result = await context.Pets
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(selector)
            .ToListAsync(ct);

        return result.Count == 0
            ? OperationResult.Failure<TResult>(DomainErrors.Pet.NotFound(id))
            : OperationResult.Success(result[0]);
    }

    public async Task<PageResult<TResult>> GetPagedAsync<TResult>(
        PetFilter filter, PageRequest page, Expression<Func<Pet, TResult>> selector, CancellationToken ct = default)
    {
        var query = context.Pets.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(filter.Nome))
            query = query.Where(p => p.Nome.Contains(filter.Nome));
        if (filter.Especie is not null)
            query = query.Where(p => p.Especie == filter.Especie);
        if (filter.Status is not null)
            query = query.Where(p => p.Status == filter.Status);
        if (filter.AbrigoId is not null)
            query = query.Where(p => p.AbrigoId == filter.AbrigoId);

        var total = await query.CountAsync(ct);
        if (total == 0)
            return new PageResult<TResult>([], page.Page, page.PageSize, 0);

        // Paginação executada no banco (OFFSET/FETCH) com projeção direta para o DTO.
        var items = await query
            .OrderBy(p => p.Nome)
            .ThenBy(p => p.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .Select(selector)
            .ToListAsync(ct);

        return new PageResult<TResult>(items, page.Page, page.PageSize, total);
    }

    public Task<int> CountByAbrigoAsync(Guid abrigoId, CancellationToken ct = default) =>
        context.Pets.CountAsync(p => p.AbrigoId == abrigoId, ct);

    public async Task AddAsync(Pet pet, CancellationToken ct = default)
    {
        context.Pets.Add(pet);
        await context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Pet pet, CancellationToken ct = default)
    {
        if (context.Entry(pet).State == EntityState.Detached)
            context.Pets.Update(pet);

        await context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        // DELETE direto no banco, sem carregar a entidade (ExecuteDelete).
        var affected = await context.Pets.Where(p => p.Id == id).ExecuteDeleteAsync(ct);

        // Erro customizado LANÇADO pelo repositório.
        if (affected == 0)
            throw new EntityNotFoundException(nameof(Pet), id);
    }
}
