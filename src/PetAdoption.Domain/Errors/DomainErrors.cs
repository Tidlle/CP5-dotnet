using PetAdoption.Domain.Common;

namespace PetAdoption.Domain.Errors;

/// <summary>
/// Catálogo central de erros customizados retornados via <see cref="OperationResult"/>.
/// </summary>
public static class DomainErrors
{
    public static class Pet
    {
        public static Error NotFound(Guid id) =>
            Error.NotFound("Pet.NotFound", $"Pet com id '{id}' não foi encontrado.");

        public static readonly Error NomeObrigatorio =
            Error.Validation("Pet.NomeObrigatorio", "O nome do pet é obrigatório e deve ter até 80 caracteres.");

        public static readonly Error IdadeInvalida =
            Error.Validation("Pet.IdadeInvalida", "A idade (em meses) deve estar entre 0 e 360.");

        public static readonly Error JaAdotado =
            Error.BusinessRule("Pet.JaAdotado", "Este pet já foi adotado e não está mais disponível.");

        public static readonly Error AdotadoNaoEditavel =
            Error.BusinessRule("Pet.AdotadoNaoEditavel", "Pets já adotados não podem ser transferidos de abrigo.");
    }

    public static class Abrigo
    {
        public static Error NotFound(Guid id) =>
            Error.NotFound("Abrigo.NotFound", $"Abrigo com id '{id}' não foi encontrado.");

        public static readonly Error NomeObrigatorio =
            Error.Validation("Abrigo.NomeObrigatorio", "O nome do abrigo é obrigatório e deve ter até 120 caracteres.");

        public static readonly Error TelefoneInvalido =
            Error.Validation("Abrigo.TelefoneInvalido", "O telefone deve conter entre 10 e 11 dígitos.");

        public static Error PossuiPets(int quantidade) =>
            Error.Conflict("Abrigo.PossuiPets", $"O abrigo possui {quantidade} pet(s) vinculado(s) e não pode ser removido.");
    }

    public static class Usuario
    {
        public static Error NotFound(Guid id) =>
            Error.NotFound("Usuario.NotFound", $"Usuário com id '{id}' não foi encontrado.");

        public static Error EmailNotFound(string email) =>
            Error.NotFound("Usuario.NotFound", $"Usuário com e-mail '{email}' não foi encontrado.");

        public static readonly Error CredenciaisInvalidas =
            Error.Unauthorized("Auth.CredenciaisInvalidas", "E-mail ou senha inválidos.");

        public static readonly Error DadosInvalidos =
            Error.Validation("Usuario.DadosInvalidos", "Nome e e-mail são obrigatórios.");
    }

    public static class Endereco
    {
        public static readonly Error Incompleto =
            Error.Validation("Endereco.Incompleto", "CEP, logradouro, cidade e UF são obrigatórios.");
    }

    public static class Cep
    {
        public static Error FormatoInvalido(string? cep) =>
            Error.Validation("Cep.FormatoInvalido", $"O CEP '{cep}' é inválido. Informe 8 dígitos.");

        public static Error NaoEncontrado(string cep) =>
            Error.NotFound("Cep.NaoEncontrado", $"O CEP '{cep}' não foi encontrado no serviço externo.");

        public static readonly Error ServicoIndisponivel =
            Error.ExternalService("Cep.ServicoIndisponivel", "O serviço externo de CEP está indisponível no momento. Tente novamente mais tarde.");
    }
}
