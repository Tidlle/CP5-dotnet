using NSubstitute;
using NSubstitute.ExceptionExtensions;
using PetAdoption.Application.DTOs;
using PetAdoption.Application.Interfaces;
using PetAdoption.Application.Services;
using PetAdoption.Domain.Common;
using PetAdoption.Domain.Entities;
using PetAdoption.Domain.Errors;
using PetAdoption.Domain.Exceptions;
using PetAdoption.Domain.Repositories;

namespace PetAdoption.UnitTests.Application;

public class AuthServiceTests
{
    private readonly IUsuarioRepository _usuarioRepository = Substitute.For<IUsuarioRepository>();
    private readonly IPasswordHasher _hasher = Substitute.For<IPasswordHasher>();
    private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _service = new AuthService(_usuarioRepository, _hasher, _tokenService);
        _hasher.Hash(Arg.Any<string>()).Returns("hash");
        _tokenService.GenerateToken(Arg.Any<Usuario>()).Returns(new TokenResult("jwt-token", DateTime.UtcNow.AddHours(1)));
    }

    [Fact]
    public async Task RegisterAsync_DadosValidos_RetornaTokenComPerfilUser()
    {
        var result = await _service.RegisterAsync(new RegisterRequest { Nome = "Ana", Email = "ANA@Email.com", Senha = "123456" });

        Assert.True(result.IsSuccess);
        Assert.Equal("jwt-token", result.Value.AccessToken);
        Assert.Equal(Roles.User, result.Value.Usuario.Role);
        Assert.Equal("ana@email.com", result.Value.Usuario.Email);
    }

    [Fact]
    public async Task RegisterAsync_EmailDuplicado_ConverteExcecaoDoRepositorioEmOperationResult()
    {
        _usuarioRepository.AddAsync(Arg.Any<Usuario>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new DuplicateEntityException(nameof(Usuario), nameof(Usuario.Email), "ana@email.com"));

        var result = await _service.RegisterAsync(new RegisterRequest { Nome = "Ana", Email = "ana@email.com", Senha = "123456" });

        Assert.True(result.IsFailure);
        Assert.Equal("Usuario.Duplicate", result.Error.Code);
        Assert.Equal(ErrorType.Conflict, result.Error.Type);
    }

    [Fact]
    public async Task LoginAsync_SenhaIncorreta_RetornaCredenciaisInvalidas()
    {
        var usuario = Usuario.Create("Ana", "ana@email.com", "hash").Value;
        _usuarioRepository.GetByEmailAsync("ana@email.com", Arg.Any<CancellationToken>()).Returns(usuario);
        _hasher.Verify("errada", "hash").Returns(false);

        var result = await _service.LoginAsync(new LoginRequest { Email = "ana@email.com", Senha = "errada" });

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorType.Unauthorized, result.Error.Type);
        _tokenService.DidNotReceive().GenerateToken(Arg.Any<Usuario>());
    }

    [Fact]
    public async Task LoginAsync_UsuarioInexistente_NaoRevelaQueOEmailNaoExiste()
    {
        _usuarioRepository.GetByEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(DomainErrors.Usuario.EmailNotFound("x@x.com"));

        var result = await _service.LoginAsync(new LoginRequest { Email = "x@x.com", Senha = "123" });

        Assert.Equal(DomainErrors.Usuario.CredenciaisInvalidas, result.Error);
    }

    [Fact]
    public async Task LoginAsync_CredenciaisValidas_RetornaToken()
    {
        var usuario = Usuario.Create("Admin", "admin@email.com", "hash", Roles.Admin).Value;
        _usuarioRepository.GetByEmailAsync("admin@email.com", Arg.Any<CancellationToken>()).Returns(usuario);
        _hasher.Verify("Admin@123", "hash").Returns(true);

        var result = await _service.LoginAsync(new LoginRequest { Email = " Admin@Email.com ", Senha = "Admin@123" });

        Assert.True(result.IsSuccess);
        Assert.Equal("Bearer", result.Value.TokenType);
        Assert.Equal(Roles.Admin, result.Value.Usuario.Role);
    }
}
