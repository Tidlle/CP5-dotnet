using System.ComponentModel.DataAnnotations;
using PetAdoption.Domain.Common;
using PetAdoption.Domain.Enums;

namespace PetAdoption.Application.DTOs;

public sealed record PetResponse(
    Guid Id,
    string Nome,
    Especie Especie,
    string? Raca,
    int IdadeMeses,
    PetStatus Status,
    Guid AbrigoId,
    string AbrigoNome,
    DateTime? DataAdocao,
    DateTime CreatedAt);

public sealed class PetRequest
{
    [Required, StringLength(80)]
    public string Nome { get; set; } = string.Empty;

    [EnumDataType(typeof(Especie))]
    public Especie Especie { get; set; }

    [StringLength(80)]
    public string? Raca { get; set; }

    [Range(0, 360)]
    public int IdadeMeses { get; set; }

    [Required]
    public Guid AbrigoId { get; set; }
}

public sealed class PetQueryParameters
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, PageRequest.MaxPageSize)]
    public int PageSize { get; set; } = PageRequest.DefaultPageSize;

    public string? Nome { get; set; }

    public Especie? Especie { get; set; }

    public PetStatus? Status { get; set; }

    public Guid? AbrigoId { get; set; }
}
