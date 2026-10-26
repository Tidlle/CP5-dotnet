using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PetAdoption.Application.Interfaces;
using PetAdoption.Infrastructure.Persistence;

namespace PetAdoption.IntegrationTests.Infrastructure;

/// <summary>
/// Sobe a API completa em memória usando o MESMO banco SQL Server da aplicação (ConnectionStrings:DefaultConnection).
/// Apenas o serviço externo (ViaCEP) é substituído por um fake, para que os testes não dependam da internet.
/// Os dados criados pelos testes usam prefixos próprios e são removidos ao final.
/// </summary>
public sealed class PetAdoptionApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string TestPrefix = "ITEST ";
    public const string TestEmailDomain = "@integration.test";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICepService>();
            services.AddSingleton<ICepService, FakeCepService>();

            if (StreamResponseBodyStartupFilter.IsRequired)
                services.AddTransient<IStartupFilter, StreamResponseBodyStartupFilter>();
        });
    }

    public async Task InitializeAsync()
    {
        // Acessar Services inicia o host: migrations + seed são aplicados no banco da aplicação.
        _ = Services;
        await CleanupAsync();
    }

    async Task IAsyncLifetime.DisposeAsync() => await CleanupAsync();

    private async Task CleanupAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await db.Pets.Where(p => p.Abrigo!.Nome.StartsWith(TestPrefix)).ExecuteDeleteAsync();
        await db.Abrigos.Where(a => a.Nome.StartsWith(TestPrefix)).ExecuteDeleteAsync();
        await db.Usuarios.Where(u => u.Email.EndsWith(TestEmailDomain)).ExecuteDeleteAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<PetAdoptionApiFactory>
{
    public const string Name = "API";
}
