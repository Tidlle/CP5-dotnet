namespace PetAdoption.API.Common;

/// <summary>Nomes das rotas usados na geração dos links HATEOAS.</summary>
public static class RouteNames
{
    public const string Root = nameof(Root);

    public const string Register = nameof(Register);
    public const string Login = nameof(Login);
    public const string Me = nameof(Me);

    public const string GetPets = nameof(GetPets);
    public const string GetPetById = nameof(GetPetById);
    public const string CreatePet = nameof(CreatePet);
    public const string UpdatePet = nameof(UpdatePet);
    public const string AdoptPet = nameof(AdoptPet);
    public const string DeletePet = nameof(DeletePet);

    public const string GetAbrigos = nameof(GetAbrigos);
    public const string GetAbrigoById = nameof(GetAbrigoById);
    public const string GetAbrigoPets = nameof(GetAbrigoPets);
    public const string CreateAbrigo = nameof(CreateAbrigo);
    public const string UpdateAbrigo = nameof(UpdateAbrigo);
    public const string DeleteAbrigo = nameof(DeleteAbrigo);

    public const string GetCep = nameof(GetCep);
}
