namespace PetAdoption.Application.DTOs;

/// <summary>Endereço retornado pelo serviço externo de CEP.</summary>
public sealed record CepResponse(
    string Cep,
    string Logradouro,
    string? Complemento,
    string? Bairro,
    string Cidade,
    string Uf);
