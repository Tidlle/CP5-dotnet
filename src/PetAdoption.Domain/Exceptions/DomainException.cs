using PetAdoption.Domain.Common;

namespace PetAdoption.Domain.Exceptions;

/// <summary>
/// Base das exceções customizadas. Carrega código e tipo para que o handler global gere a resposta HTTP correta.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string code, string message, ErrorType type, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        Type = type;
    }

    public string Code { get; }

    public ErrorType Type { get; }

    public Error ToError() => new(Code, Message, Type);
}

/// <summary>Lançada pelo repositório quando a entidade solicitada não existe.</summary>
public sealed class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, object key)
        : base($"{entityName}.NotFound", $"{entityName} com id '{key}' não foi encontrado(a).", ErrorType.NotFound)
    {
        EntityName = entityName;
        Key = key;
    }

    public string EntityName { get; }

    public object Key { get; }
}

/// <summary>Lançada pelo repositório quando uma restrição de unicidade seria violada.</summary>
public sealed class DuplicateEntityException : DomainException
{
    public DuplicateEntityException(string entityName, string field, object value, Exception? innerException = null)
        : base($"{entityName}.Duplicate", $"Já existe {entityName} com {field} '{value}'.", ErrorType.Conflict, innerException)
    {
        EntityName = entityName;
        Field = field;
    }

    public string EntityName { get; }

    public string Field { get; }
}

/// <summary>Violação de regra de negócio.</summary>
public sealed class BusinessRuleException : DomainException
{
    public BusinessRuleException(string code, string message)
        : base(code, message, ErrorType.BusinessRule)
    {
    }
}
