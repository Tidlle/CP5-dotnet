using PetAdoption.Domain.Common;
using PetAdoption.Domain.Errors;

namespace PetAdoption.Domain.Entities;

/// <summary>Usuário da API (autenticação via JWT).</summary>
public sealed class Usuario : BaseEntity
{
    private Usuario()
    {
    }

    public string Nome { get; private set; } = null!;

    public string Email { get; private set; } = null!;

    public string SenhaHash { get; private set; } = null!;

    public string Role { get; private set; } = Roles.User;

    public static OperationResult<Usuario> Create(string nome, string email, string senhaHash, string role = Roles.User)
    {
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(email))
            return DomainErrors.Usuario.DadosInvalidos;

        return new Usuario
        {
            Nome = nome.Trim(),
            Email = NormalizeEmail(email),
            SenhaHash = senhaHash,
            Role = role
        };
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
