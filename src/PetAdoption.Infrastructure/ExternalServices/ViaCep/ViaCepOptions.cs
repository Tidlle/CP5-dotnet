namespace PetAdoption.Infrastructure.ExternalServices.ViaCep;

public sealed class ViaCepOptions
{
    public const string SectionName = "ExternalServices:ViaCep";

    public string BaseUrl { get; set; } = "https://viacep.com.br/";

    /// <summary>Timeout de cada tentativa (o handler de resiliência faz retry).</summary>
    public int AttemptTimeoutSeconds { get; set; } = 5;

    public int TotalTimeoutSeconds { get; set; } = 20;

    public int MaxRetryAttempts { get; set; } = 3;

    public int CacheMinutes { get; set; } = 360;
}
