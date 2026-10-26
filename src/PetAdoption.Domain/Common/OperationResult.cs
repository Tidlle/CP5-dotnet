namespace PetAdoption.Domain.Common;

/// <summary>
/// Padrão Operation Result: encapsula sucesso/falha de uma operação sem usar exceções para fluxo de controle.
/// </summary>
public class OperationResult
{
    protected OperationResult(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
            throw new InvalidOperationException("Um resultado de sucesso não pode conter erro.");

        if (!isSuccess && error == Error.None)
            throw new InvalidOperationException("Um resultado de falha precisa conter um erro.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public Error Error { get; }

    public static OperationResult Success() => new(true, Error.None);

    public static OperationResult Failure(Error error) => new(false, error);

    public static OperationResult<T> Success<T>(T value) => new(value, true, Error.None);

    public static OperationResult<T> Failure<T>(Error error) => new(default, false, error);

    public static implicit operator OperationResult(Error error) => Failure(error);
}

/// <summary>
/// Operation Result com payload.
/// </summary>
public sealed class OperationResult<T> : OperationResult
{
    private readonly T? _value;

    internal OperationResult(T? value, bool isSuccess, Error error) : base(isSuccess, error)
    {
        _value = value;
    }

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Não é possível acessar o valor de um resultado de falha.");

    public OperationResult<TOut> Map<TOut>(Func<T, TOut> map) =>
        IsSuccess ? Success(map(Value)) : Failure<TOut>(Error);

    public static implicit operator OperationResult<T>(T value) => Success(value);

    public static implicit operator OperationResult<T>(Error error) => Failure<T>(error);
}
