namespace PetAdoption.Infrastructure.Security;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "PetAdoption.API";

    public string Audience { get; set; } = "PetAdoption.Clients";

    /// <summary>Chave simétrica HMAC-SHA256 (mínimo 32 caracteres).</summary>
    public string Key { get; set; } = string.Empty;

    public int ExpirationMinutes { get; set; } = 60;
}
