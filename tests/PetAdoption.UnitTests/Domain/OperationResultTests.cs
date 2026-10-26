using PetAdoption.Domain.Common;

namespace PetAdoption.UnitTests.Domain;

public class OperationResultTests
{
    [Fact]
    public void Success_DeveConterValorESemErro()
    {
        OperationResult<int> result = 42;

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
        Assert.Equal(Error.None, result.Error);
    }

    [Fact]
    public void Failure_DeveConterErroEImpedirAcessoAoValor()
    {
        var error = Error.NotFound("Teste.NotFound", "não encontrado");
        OperationResult<string> result = error;

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Map_EmSucesso_TransformaValor()
    {
        var result = OperationResult.Success(10).Map(v => v * 2);

        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Value);
    }

    [Fact]
    public void Map_EmFalha_PropagaOMesmoErro()
    {
        var error = Error.Conflict("Teste.Conflict", "conflito");

        var result = OperationResult.Failure<int>(error).Map(v => v.ToString());

        Assert.True(result.IsFailure);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void Failure_ComErrorNone_LancaExcecao()
    {
        Assert.Throws<InvalidOperationException>(() => OperationResult.Failure(Error.None));
    }
}
