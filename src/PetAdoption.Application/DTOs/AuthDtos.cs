using System.ComponentModel.DataAnnotations;

namespace PetAdoption.Application.DTOs;

public sealed class RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 2)]
    public string Nome { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 6)]
    public string Senha { get; set; } = string.Empty;
}

public sealed class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Senha { get; set; } = string.Empty;
}

public sealed record UsuarioResponse(Guid Id, string Nome, string Email, string Role);

public sealed record AuthResponse(string AccessToken, string TokenType, DateTime ExpiresAt, UsuarioResponse Usuario);

public sealed record TokenResult(string Token, DateTime ExpiresAt);
