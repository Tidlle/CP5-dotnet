using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PetAdoption.Application.Interfaces;
using PetAdoption.Domain.Common;
using PetAdoption.Domain.Entities;
using PetAdoption.Domain.Enums;
using PetAdoption.Domain.ValueObjects;

namespace PetAdoption.Infrastructure.Persistence;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public string AdminNome { get; set; } = "Administrador";

    public string AdminEmail { get; set; } = "admin@petadoption.com";

    public string AdminPassword { get; set; } = string.Empty;

    public bool SampleData { get; set; } = true;
}

/// <summary>Aplica as migrations e popula o usuário administrador e dados de exemplo.</summary>
public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct = default)
    {
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var seed = scope.ServiceProvider.GetRequiredService<IOptions<SeedOptions>>().Value;
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DatabaseInitializer));

        await context.Database.MigrateAsync(ct);

        var adminEmail = Usuario.NormalizeEmail(seed.AdminEmail);
        if (!string.IsNullOrWhiteSpace(seed.AdminPassword) &&
            !await context.Usuarios.AnyAsync(u => u.Email == adminEmail, ct))
        {
            context.Usuarios.Add(Usuario.Create(seed.AdminNome, adminEmail, hasher.Hash(seed.AdminPassword), Roles.Admin).Value);
            logger.LogInformation("Usuário administrador {Email} criado", adminEmail);
        }

        if (seed.SampleData)
            await SeedSampleDataAsync(context, ct);

        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedSampleDataAsync(AppDbContext context, CancellationToken ct)
    {
        var abrigos = new[]
        {
            (Nome: "Abrigo Patas Felizes", Telefone: "11987654321",
                Endereco: Endereco.Create("01310100", "Avenida Paulista", "1578", null, "Bela Vista", "São Paulo", "SP").Value,
                Pets: new (string Nome, Especie Especie, string? Raca, int Idade)[]
                {
                    ("Thor", Especie.Cachorro, "Labrador", 24), ("Luna", Especie.Gato, "Siamês", 12),
                    ("Bob", Especie.Cachorro, "Vira-lata", 36), ("Mia", Especie.Gato, null, 6),
                    ("Pipoca", Especie.Coelho, "Mini Lop", 10), ("Rex", Especie.Cachorro, "Pastor Alemão", 48),
                    ("Nina", Especie.Gato, "Persa", 30), ("Fred", Especie.Cachorro, "Beagle", 18)
                }),
            (Nome: "Lar dos Bigodes", Telefone: "2134567890",
                Endereco: Endereco.Create("20040020", "Avenida Rio Branco", "156", null, "Centro", "Rio de Janeiro", "RJ").Value,
                Pets: new (string Nome, Especie Especie, string? Raca, int Idade)[]
                {
                    ("Mel", Especie.Cachorro, "Golden Retriever", 60), ("Simba", Especie.Gato, "Maine Coon", 20),
                    ("Kiwi", Especie.Ave, "Calopsita", 14), ("Toby", Especie.Cachorro, "Poodle", 84)
                })
        };

        foreach (var item in abrigos)
        {
            if (await context.Abrigos.AnyAsync(a => a.Nome == item.Nome, ct))
                continue;

            var abrigo = Abrigo.Create(item.Nome, item.Telefone, item.Endereco).Value;
            context.Abrigos.Add(abrigo);

            foreach (var pet in item.Pets)
                context.Pets.Add(Pet.Create(pet.Nome, pet.Especie, pet.Raca, pet.Idade, abrigo.Id).Value);
        }
    }
}
