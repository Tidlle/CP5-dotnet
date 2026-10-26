using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace PetAdoption.IntegrationTests.Infrastructure;

/// <summary>
/// Compatibilidade para quando os testes (net8.0) são executados por roll-forward em um runtime mais novo
/// (ex.: máquina só com .NET 10). O TestServer do pacote 8.x não implementa PipeWriter.UnflushedBytes,
/// exigido pelo serializador JSON do MVC 10. Envolver o corpo da resposta em um StreamResponseBodyFeature
/// resolve isso. No runtime .NET 8 este filtro não é registrado.
/// </summary>
internal sealed class StreamResponseBodyStartupFilter : IStartupFilter
{
    public static bool IsRequired => Environment.Version.Major > 8;

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, nextMiddleware) =>
        {
            var original = context.Features.Get<IHttpResponseBodyFeature>()!;
            var streamFeature = new StreamResponseBodyFeature(original.Stream, original);
            context.Features.Set<IHttpResponseBodyFeature>(streamFeature);

            try
            {
                await nextMiddleware(context);
                await streamFeature.CompleteAsync();
            }
            finally
            {
                context.Features.Set(original);
            }
        });

        next(app);
    };
}
