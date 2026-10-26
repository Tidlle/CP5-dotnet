using Microsoft.AspNetCore.Mvc;
using PetAdoption.Domain.Common;

namespace PetAdoption.API.Common;

/// <summary>Converte um <see cref="Error"/> customizado em ProblemDetails (RFC 7807).</summary>
public static class ProblemDetailsMapper
{
    public const string OriginOperationResult = "OperationResult";
    public const string OriginException = "Exception";

    public static int ToStatusCode(this ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.BusinessRule => StatusCodes.Status422UnprocessableEntity,
        ErrorType.ExternalService => StatusCodes.Status503ServiceUnavailable,
        _ => StatusCodes.Status500InternalServerError
    };

    private static string ToTitle(this ErrorType type) => type switch
    {
        ErrorType.Validation => "Requisição inválida",
        ErrorType.Unauthorized => "Não autenticado",
        ErrorType.Forbidden => "Acesso negado",
        ErrorType.NotFound => "Recurso não encontrado",
        ErrorType.Conflict => "Conflito",
        ErrorType.BusinessRule => "Regra de negócio violada",
        ErrorType.ExternalService => "Serviço externo indisponível",
        _ => "Erro interno"
    };

    public static ProblemDetails Create(HttpContext httpContext, Error error, string origin)
    {
        var status = error.Type.ToStatusCode();

        ProblemDetails problem = error.Details is { Count: > 0 }
            ? new ValidationProblemDetails(error.Details.ToDictionary(d => d.Key, d => d.Value))
            : new ProblemDetails();

        problem.Status = status;
        problem.Title = error.Type.ToTitle();
        problem.Detail = error.Message;
        problem.Type = $"https://httpstatuses.io/{status}";
        problem.Instance = httpContext.Request.Path;
        problem.Extensions["code"] = error.Code;
        problem.Extensions["origin"] = origin;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        return problem;
    }
}
