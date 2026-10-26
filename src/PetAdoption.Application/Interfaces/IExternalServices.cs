using PetAdoption.Application.DTOs;
using PetAdoption.Domain.Common;
using PetAdoption.Domain.Entities;

namespace PetAdoption.Application.Interfaces;

/// <summary>Consulta de CEP em serviço externo (implementada na Infrastructure).</summary>
public interface ICepService
{
    Task<OperationResult<CepResponse>> ConsultarAsync(string cep, CancellationToken ct = default);
}

public interface ITokenService
{
    TokenResult GenerateToken(Usuario usuario);
}

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
