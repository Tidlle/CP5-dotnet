using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PetAdoption.Application.Interfaces;
using PetAdoption.Domain.Repositories;
using PetAdoption.Infrastructure.ExternalServices.ViaCep;
using PetAdoption.Infrastructure.Persistence;
using PetAdoption.Infrastructure.Repositories;
using PetAdoption.Infrastructure.Security;

namespace PetAdoption.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection não configurada.");

        // Pool de DbContext: reaproveita instâncias e reduz alocações por requisição.
        services.AddDbContextPool<AppDbContext>(options =>
            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(3)));

        services.AddScoped<IPetRepository, PetRepository>();
        services.AddScoped<IAbrigoRepository, AbrigoRepository>();
        services.AddScoped<IUsuarioRepository, UsuarioRepository>();

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        AddViaCep(services, configuration);

        return services;
    }

    private static void AddViaCep(IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(ViaCepOptions.SectionName);
        services.Configure<ViaCepOptions>(section);
        var options = section.Get<ViaCepOptions>() ?? new ViaCepOptions();

        services.AddMemoryCache();

        services.AddHttpClient<ICepService, ViaCepService>(client =>
            {
                client.BaseAddress = new Uri(options.BaseUrl);
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            })
            // Resiliência (Polly): retry com backoff exponencial, timeout por tentativa/total e circuit breaker.
            .AddStandardResilienceHandler(resilience =>
            {
                resilience.Retry.MaxRetryAttempts = options.MaxRetryAttempts;
                resilience.Retry.UseJitter = true;
                resilience.AttemptTimeout.Timeout = TimeSpan.FromSeconds(options.AttemptTimeoutSeconds);
                resilience.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(options.TotalTimeoutSeconds);
                resilience.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(Math.Max(30, options.AttemptTimeoutSeconds * 2));
            });
    }
}
