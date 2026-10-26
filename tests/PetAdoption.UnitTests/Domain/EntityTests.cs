using PetAdoption.Domain.Entities;
using PetAdoption.Domain.Enums;
using PetAdoption.Domain.ValueObjects;

namespace PetAdoption.UnitTests.Domain;

public class PetTests
{
    [Fact]
    public void Create_DadosValidos_CriaPetDisponivel()
    {
        var result = Pet.Create("  Thor ", Especie.Cachorro, "Labrador", 24, Guid.NewGuid());

        Assert.True(result.IsSuccess);
        Assert.Equal("Thor", result.Value.Nome);
        Assert.Equal(PetStatus.Disponivel, result.Value.Status);
    }

    [Theory]
    [InlineData("", 10, "Pet.NomeObrigatorio")]
    [InlineData("Rex", -1, "Pet.IdadeInvalida")]
    [InlineData("Rex", 361, "Pet.IdadeInvalida")]
    public void Create_DadosInvalidos_RetornaErroDeValidacao(string nome, int idade, string expectedCode)
    {
        var result = Pet.Create(nome, Especie.Gato, null, idade, Guid.NewGuid());

        Assert.True(result.IsFailure);
        Assert.Equal(expectedCode, result.Error.Code);
    }

    [Fact]
    public void Adotar_PetDisponivel_AlteraStatus()
    {
        var pet = Pet.Create("Luna", Especie.Gato, null, 12, Guid.NewGuid()).Value;
        var adotante = Guid.NewGuid();
        var data = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

        var result = pet.Adotar(adotante, data);

        Assert.True(result.IsSuccess);
        Assert.Equal(PetStatus.Adotado, pet.Status);
        Assert.Equal(adotante, pet.AdotanteId);
        Assert.Equal(data, pet.DataAdocao);
    }

    [Fact]
    public void Adotar_PetJaAdotado_RetornaErroDeRegraDeNegocio()
    {
        var pet = Pet.Create("Luna", Especie.Gato, null, 12, Guid.NewGuid()).Value;
        pet.Adotar(Guid.NewGuid(), DateTime.UtcNow);

        var result = pet.Adotar(Guid.NewGuid(), DateTime.UtcNow);

        Assert.True(result.IsFailure);
        Assert.Equal("Pet.JaAdotado", result.Error.Code);
    }
}

public class AbrigoTests
{
    private static Endereco EnderecoValido() =>
        Endereco.Create("01001-000", "Praça da Sé", "100", null, "Sé", "São Paulo", "sp").Value;

    [Fact]
    public void Create_DadosValidos_NormalizaTelefoneEUf()
    {
        var result = Abrigo.Create("Abrigo Central", "(11) 98888-7777", EnderecoValido());

        Assert.True(result.IsSuccess);
        Assert.Equal("11988887777", result.Value.Telefone);
        Assert.Equal("SP", result.Value.Endereco.Uf);
        Assert.Equal("01001000", result.Value.Endereco.Cep);
    }

    [Fact]
    public void Create_TelefoneInvalido_RetornaErro()
    {
        var result = Abrigo.Create("Abrigo Central", "1234", EnderecoValido());

        Assert.True(result.IsFailure);
        Assert.Equal("Abrigo.TelefoneInvalido", result.Error.Code);
    }

    [Fact]
    public void Endereco_SemCidade_RetornaErro()
    {
        var result = Endereco.Create("01001000", "Rua A", null, null, null, "", "SP");

        Assert.True(result.IsFailure);
        Assert.Equal("Endereco.Incompleto", result.Error.Code);
    }
}
