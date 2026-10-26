using Microsoft.AspNetCore.Diagnostics;
using PetAdoption.API.Common;
using PetAdoption.Domain.Common;
using PetAdoption.Domain.Exceptions;

namespace PetAdoption.API.Middleware;

/// <summary>
/// Captura as exceções customizadas lançadas pelas camadas internas (ex.: repositório) e as converte em ProblemDetails.
/// Exceções não previstas viram 500 sem expor detalhes internos.
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        Error error;

        switch (exception)
        {
            case DomainException domainException:
                logger.LogWarning("Erro de domínio {Code}: {Message}", domainException.Code, domainException.Message);
                error = domainException.ToError();
                break;

            case BadHttpRequestException badRequest:
                error = Error.Validation("Request.Invalida", badRequest.Message);
                break;

            default:
                logger.LogError(exception, "Erro não tratado ao processar {Method} {Path}",
                    httpContext.Request.Method, httpContext.Request.Path);
                error = Error.Unexpected("Server.ErroInesperado", "Ocorreu um erro inesperado ao processar a requisição.");
                break;
        }

        var problem = ProblemDetailsMapper.Create(httpContext, error, ProblemDetailsMapper.OriginException);
        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync(problem, options: null, contentType: "application/problem+json", cancellationToken);
        return true;
    }
}
