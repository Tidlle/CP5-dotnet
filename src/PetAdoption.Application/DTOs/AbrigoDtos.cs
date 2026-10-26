using System.ComponentModel.DataAnnotations;
using PetAdoption.Domain.Common;

namespace PetAdoption.Application.DTOs;

public sealed record EnderecoResponse(
    string Cep,
    string Logradouro,
    string? Numero,
    string? Complemento,
    string? Bairro,
    string Cidade,
    string Uf);

public sealed record AbrigoResponse(
    Guid Id,
    string Nome,
    string Telefone,
    EnderecoResponse Endereco,
    int TotalPets,
    int PetsDisponiveis,
    DateTime CreatedAt);

public sealed class AbrigoRequest
{
    [Required, StringLength(120)]
    public string Nome { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string Telefone { get; set; } = string.Empty;

    /// <summary>CEP usado para buscar o endereço no serviço externo (ViaCEP).</summary>
    [Required, RegularExpression(@"^\d{5}-?\d{3}$", ErrorMessage = "O CEP deve estar no formato 00000-000 ou 00000000.")]
    public string Cep { get; set; } = string.Empty;

    [StringLength(20)]
    public string? Numero { get; set; }

    [StringLength(100)]
    public string? Complemento { get; set; }
}

public sealed class AbrigoQueryParameters
{
    [Range(1, int.MaxValue)]
    public int Page { get; set; } = 1;

    [Range(1, PageRequest.MaxPageSize)]
    public int PageSize { get; set; } = PageRequest.DefaultPageSize;

    public string? Nome { get; set; }

    public string? Cidade { get; set; }

    [StringLength(2)]
    public string? Uf { get; set; }
}
