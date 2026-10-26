using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using PetAdoption.Domain.Common;
using PetAdoption.Domain.Entities;
using PetAdoption.Infrastructure.Security;
using PetAdoption.UnitTests.Fakes;

namespace PetAdoption.UnitTests.Infrastructure;

public class Pbkdf2PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _hasher = new();

    [Fact]
    public void Verify_SenhaCorreta_RetornaTrue()
    {
        var hash = _hasher.Hash("Senha@123");

        Assert.True(_hasher.Verify("Senha@123", hash));
        Assert.False(_hasher.Verify("senha@123", hash));
    }

    [Fact]
    public void Hash_MesmaSenha_GeraHashesDiferentesPorCausaDoSalt()
    {
        Assert.NotEqual(_hasher.Hash("abc123"), _hasher.Hash("abc123"));
    }

    [Fact]
    public void Verify_HashMalformado_RetornaFalse()
    {
        Assert.False(_hasher.Verify("abc", "formato-invalido"));
    }
}

public class JwtTokenServiceTests
{
    [Fact]
    public void GenerateToken_IncluiClaimsDeIdentidadeEPerfil()
    {
        var agora = new DateTimeOffset(2026, 10, 25, 12, 0, 0, TimeSpan.Zero);
        var settings = new JwtSettings
        {
            Issuer = "issuer",
            Audience = "audience",
            Key = new string('k', 40),
            ExpirationMinutes = 30
        };
        var service = new JwtTokenService(Options.Create(settings), new FixedTimeProvider(agora));
        var usuario = Usuario.Create("Admin", "admin@email.com", "hash", Roles.Admin).Value;

        var result = service.GenerateToken(usuario);
        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Token);

        Assert.Equal(agora.UtcDateTime.AddMinutes(30), result.ExpiresAt);
        Assert.Equal("issuer", token.Issuer);
        Assert.Equal(usuario.Id.ToString(), token.Subject);
        Assert.Contains(token.Claims, c => c.Type == CustomClaims.Role && c.Value == Roles.Admin);
        Assert.Contains(token.Claims, c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "admin@email.com");
    }
}
