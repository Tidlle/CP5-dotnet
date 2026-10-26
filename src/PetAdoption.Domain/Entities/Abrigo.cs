using PetAdoption.Domain.Common;
using PetAdoption.Domain.Errors;
using PetAdoption.Domain.ValueObjects;

namespace PetAdoption.Domain.Entities;

/// <summary>Abrigo (ONG/canil) que mantém pets disponíveis para adoção.</summary>
public sealed class Abrigo : BaseEntity
{
    public const int NomeMaxLength = 120;

    private readonly List<Pet> _pets = [];

    private Abrigo()
    {
    }

    public string Nome { get; private set; } = null!;

    public string Telefone { get; private set; } = null!;

    public Endereco Endereco { get; private set; } = null!;

    public IReadOnlyCollection<Pet> Pets => _pets;

    public static OperationResult<Abrigo> Create(string nome, string telefone, Endereco endereco)
    {
        var validation = Validate(nome, telefone);
        if (validation.IsFailure)
            return validation.Error;

        return new Abrigo
        {
            Nome = nome.Trim(),
            Telefone = OnlyDigits(telefone),
            Endereco = endereco
        };
    }

    public OperationResult Update(string nome, string telefone, Endereco endereco)
    {
        var validation = Validate(nome, telefone);
        if (validation.IsFailure)
            return validation;

        Nome = nome.Trim();
        Telefone = OnlyDigits(telefone);
        Endereco = endereco;
        Touch();
        return OperationResult.Success();
    }

    private static OperationResult Validate(string nome, string telefone)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > NomeMaxLength)
            return DomainErrors.Abrigo.NomeObrigatorio;

        var digits = OnlyDigits(telefone);
        if (digits.Length is < 10 or > 11)
            return DomainErrors.Abrigo.TelefoneInvalido;

        return OperationResult.Success();
    }

    private static string OnlyDigits(string? value) =>
        new((value ?? string.Empty).Where(char.IsDigit).ToArray());
}
