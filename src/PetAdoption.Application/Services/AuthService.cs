using PetAdoption.Application.DTOs;
using PetAdoption.Application.Interfaces;
using PetAdoption.Application.Mappings;
using PetAdoption.Domain.Common;
using PetAdoption.Domain.Entities;
using PetAdoption.Domain.Errors;
using PetAdoption.Domain.Exceptions;
using PetAdoption.Domain.Repositories;

namespace PetAdoption.Application.Services;

public interface IAuthService
{
    Task<OperationResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    Task<OperationResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task<OperationResult<UsuarioResponse>> GetProfileAsync(Guid usuarioId, CancellationToken ct = default);
}

public sealed class AuthService(
    IUsuarioRepository usuarioRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService) : IAuthService
{
    public async Task<OperationResult<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var usuario = Usuario.Create(request.Nome, request.Email, passwordHasher.Hash(request.Senha), Roles.User);
        if (usuario.IsFailure)
            return usuario.Error;

        try
        {
            await usuarioRepository.AddAsync(usuario.Value, ct);
        }
        catch (DuplicateEntityException ex)
        {
            // Exceção customizada lançada pelo repositório convertida em OperationResult.
            return ex.ToError();
        }

        return BuildAuthResponse(usuario.Value);
    }

    public async Task<OperationResult<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var usuario = await usuarioRepository.GetByEmailAsync(Usuario.NormalizeEmail(request.Email), ct);

        // Não revela se o e-mail existe: qualquer falha vira "credenciais inválidas".
        if (usuario.IsFailure || !passwordHasher.Verify(request.Senha, usuario.Value.SenhaHash))
            return DomainErrors.Usuario.CredenciaisInvalidas;

        return BuildAuthResponse(usuario.Value);
    }

    public async Task<OperationResult<UsuarioResponse>> GetProfileAsync(Guid usuarioId, CancellationToken ct = default)
    {
        var usuario = await usuarioRepository.GetByIdAsync(usuarioId, ct);
        return usuario.Map(u => u.ToResponse());
    }

    private AuthResponse BuildAuthResponse(Usuario usuario)
    {
        var token = tokenService.GenerateToken(usuario);
        return new AuthResponse(token.Token, "Bearer", token.ExpiresAt, usuario.ToResponse());
    }
}
