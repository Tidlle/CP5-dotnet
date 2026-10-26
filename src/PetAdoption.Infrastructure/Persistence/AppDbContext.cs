using Microsoft.EntityFrameworkCore;
using PetAdoption.Domain.Entities;

namespace PetAdoption.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Pet> Pets => Set<Pet>();

    public DbSet<Abrigo> Abrigos => Set<Abrigo>();

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
