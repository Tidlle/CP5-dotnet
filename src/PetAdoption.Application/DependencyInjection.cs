using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PetAdoption.Application.Services;

namespace PetAdoption.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<IPetService, PetService>();
        services.AddScoped<IAbrigoService, AbrigoService>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
