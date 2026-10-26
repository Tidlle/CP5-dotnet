namespace PetAdoption.Domain.Common;

/// <summary>
/// Categoria do erro. É usada pela camada de apresentação para escolher o status HTTP.
/// </summary>
public enum ErrorType
{
    Validation,
    NotFound,
    Conflict,
    BusinessRule,
    Unauthorized,
    Forbidden,
    ExternalService,
    Unexpected
}

/// <summary>
/// Erro de domínio/aplicação transportado pelo <see cref="OperationResult"/>.
/// </summary>
public sealed record Error(
    string Code,
    string Message,
    ErrorType Type,
    IReadOnlyDictionary<string, string[]>? Details = null)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Unexpected);

    public static Error Validation(string code, string message, IReadOnlyDictionary<string, string[]>? details = null) =>
        new(code, message, ErrorType.Validation, details);

    public static Error NotFound(string code, string message) => new(code, message, ErrorType.NotFound);

    public static Error Conflict(string code, string message) => new(code, message, ErrorType.Conflict);

    public static Error BusinessRule(string code, string message) => new(code, message, ErrorType.BusinessRule);

    public static Error Unauthorized(string code, string message) => new(code, message, ErrorType.Unauthorized);

    public static Error Forbidden(string code, string message) => new(code, message, ErrorType.Forbidden);

    public static Error ExternalService(string code, string message) => new(code, message, ErrorType.ExternalService);

    public static Error Unexpected(string code, string message) => new(code, message, ErrorType.Unexpected);
}
