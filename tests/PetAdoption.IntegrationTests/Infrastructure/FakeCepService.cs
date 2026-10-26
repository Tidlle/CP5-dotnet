using PetAdoption.Application.DTOs;
using PetAdoption.Application.Interfaces;
using PetAdoption.Domain.Common;
using PetAdoption.Domain.Errors;

namespace PetAdoption.IntegrationTests.Infrastructure;

/// <summary>Substitui o ViaCEP nos testes de integração com respostas determinísticas.</summary>
public sealed class FakeCepService : ICepService
{
    public const string CepInexistente = "00000000";
    public const string CepServicoIndisponivel = "99999000";
    public const string Cidade = "Cidade Teste";

    public Task<OperationResult<CepResponse>> ConsultarAsync(string cep, CancellationToken ct = default)
    {
        var digits = new string((cep ?? string.Empty).Where(char.IsDigit).ToArray());

        OperationResult<CepResponse> result = digits switch
        {
            { Length: not 8 } => DomainErrors.Cep.FormatoInvalido(cep),
            CepInexistente => DomainErrors.Cep.NaoEncontrado(digits),
            CepServicoIndisponivel => DomainErrors.Cep.ServicoIndisponivel,
            _ => new CepResponse(digits, "Rua dos Testes", null, "Centro", Cidade, "SP")
        };

        return Task.FromResult(result);
    }
}
