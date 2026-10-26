using PetAdoption.Domain.Common;
using PetAdoption.Domain.Enums;
using PetAdoption.Domain.Errors;

namespace PetAdoption.Domain.Entities;

/// <summary>Animal cadastrado em um abrigo e disponível (ou não) para adoção.</summary>
public sealed class Pet : BaseEntity
{
    public const int NomeMaxLength = 80;
    public const int IdadeMaximaMeses = 360;

    private Pet()
    {
    }

    public string Nome { get; private set; } = null!;

    public Especie Especie { get; private set; }

    public string? Raca { get; private set; }

    public int IdadeMeses { get; private set; }

    public PetStatus Status { get; private set; } = PetStatus.Disponivel;

    public Guid AbrigoId { get; private set; }

    public Abrigo? Abrigo { get; private set; }

    public Guid? AdotanteId { get; private set; }

    public DateTime? DataAdocao { get; private set; }

    public static OperationResult<Pet> Create(string nome, Especie especie, string? raca, int idadeMeses, Guid abrigoId)
    {
        var validation = Validate(nome, idadeMeses);
        if (validation.IsFailure)
            return validation.Error;

        return new Pet
        {
            Nome = nome.Trim(),
            Especie = especie,
            Raca = string.IsNullOrWhiteSpace(raca) ? null : raca.Trim(),
            IdadeMeses = idadeMeses,
            AbrigoId = abrigoId
        };
    }

    public OperationResult Update(string nome, Especie especie, string? raca, int idadeMeses, Guid abrigoId)
    {
        var validation = Validate(nome, idadeMeses);
        if (validation.IsFailure)
            return validation;

        if (Status == PetStatus.Adotado && abrigoId != AbrigoId)
            return DomainErrors.Pet.AdotadoNaoEditavel;

        Nome = nome.Trim();
        Especie = especie;
        Raca = string.IsNullOrWhiteSpace(raca) ? null : raca.Trim();
        IdadeMeses = idadeMeses;
        AbrigoId = abrigoId;
        Touch();
        return OperationResult.Success();
    }

    public OperationResult Adotar(Guid adotanteId, DateTime dataAdocao)
    {
        if (Status == PetStatus.Adotado)
            return DomainErrors.Pet.JaAdotado;

        Status = PetStatus.Adotado;
        AdotanteId = adotanteId;
        DataAdocao = dataAdocao;
        Touch();
        return OperationResult.Success();
    }

    private static OperationResult Validate(string nome, int idadeMeses)
    {
        if (string.IsNullOrWhiteSpace(nome) || nome.Trim().Length > NomeMaxLength)
            return DomainErrors.Pet.NomeObrigatorio;

        if (idadeMeses is < 0 or > IdadeMaximaMeses)
            return DomainErrors.Pet.IdadeInvalida;

        return OperationResult.Success();
    }
}
