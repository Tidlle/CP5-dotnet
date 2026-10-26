using PetAdoption.Domain.Common;
using PetAdoption.Domain.Errors;

namespace PetAdoption.Domain.ValueObjects;

/// <summary>Value object de endereço (persistido como owned type do Abrigo).</summary>
public sealed class Endereco
{
    private Endereco()
    {
    }

    public string Cep { get; private init; } = null!;
    public string Logradouro { get; private init; } = null!;
    public string? Numero { get; private init; }
    public string? Complemento { get; private init; }
    public string? Bairro { get; private init; }
    public string Cidade { get; private init; } = null!;
    public string Uf { get; private init; } = null!;

    public static OperationResult<Endereco> Create(
        string cep, string logradouro, string? numero, string? complemento, string? bairro, string cidade, string uf)
    {
        var digits = new string((cep ?? string.Empty).Where(char.IsDigit).ToArray());

        if (digits.Length != 8 || string.IsNullOrWhiteSpace(logradouro) ||
            string.IsNullOrWhiteSpace(cidade) || string.IsNullOrWhiteSpace(uf))
        {
            return DomainErrors.Endereco.Incompleto;
        }

        return new Endereco
        {
            Cep = digits,
            Logradouro = logradouro.Trim(),
            Numero = string.IsNullOrWhiteSpace(numero) ? null : numero.Trim(),
            Complemento = string.IsNullOrWhiteSpace(complemento) ? null : complemento.Trim(),
            Bairro = string.IsNullOrWhiteSpace(bairro) ? null : bairro.Trim(),
            Cidade = cidade.Trim(),
            Uf = uf.Trim().ToUpperInvariant()
        };
    }
}
