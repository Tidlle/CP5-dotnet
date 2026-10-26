using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PetAdoption.Application.DTOs;
using PetAdoption.Application.Interfaces;
using PetAdoption.Domain.Common;
using PetAdoption.Domain.Errors;
using Polly;

namespace PetAdoption.Infrastructure.ExternalServices.ViaCep;

/// <summary>
/// Integração com a API pública ViaCEP (https://viacep.com.br).
/// Retry, timeout e circuit breaker são aplicados pelo resilience handler configurado no HttpClient;
/// respostas válidas ficam em cache para evitar chamadas repetidas.
/// </summary>
public sealed class ViaCepService(
    HttpClient httpClient,
    IMemoryCache cache,
    IOptions<ViaCepOptions> options,
    ILogger<ViaCepService> logger) : ICepService
{
    public async Task<OperationResult<CepResponse>> ConsultarAsync(string cep, CancellationToken ct = default)
    {
        var digits = new string((cep ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length != 8)
            return DomainErrors.Cep.FormatoInvalido(cep);

        var cacheKey = $"viacep:{digits}";
        if (cache.TryGetValue(cacheKey, out CepResponse? cached) && cached is not null)
        {
            logger.LogDebug("CEP {Cep} obtido do cache", digits);
            return cached;
        }

        try
        {
            using var response = await httpClient.GetAsync($"ws/{digits}/json/", ct);

            if (response.StatusCode == HttpStatusCode.BadRequest)
                return DomainErrors.Cep.FormatoInvalido(cep);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("ViaCEP respondeu {StatusCode} para o CEP {Cep}", (int)response.StatusCode, digits);
                return DomainErrors.Cep.ServicoIndisponivel;
            }

            var payload = await response.Content.ReadFromJsonAsync<ViaCepPayload>(ct);

            // O ViaCEP responde 200 com {"erro": true} quando o CEP não existe.
            if (payload is null || payload.Erro is not null || string.IsNullOrWhiteSpace(payload.Localidade))
                return DomainErrors.Cep.NaoEncontrado(digits);

            var endereco = new CepResponse(
                digits,
                payload.Logradouro ?? string.Empty,
                NullIfEmpty(payload.Complemento),
                NullIfEmpty(payload.Bairro),
                payload.Localidade,
                payload.Uf ?? string.Empty);

            cache.Set(cacheKey, endereco, TimeSpan.FromMinutes(options.Value.CacheMinutes));
            return endereco;
        }
        catch (Exception ex) when (IsTransient(ex) && !ct.IsCancellationRequested)
        {
            logger.LogError(ex, "Falha ao consultar o ViaCEP para o CEP {Cep}", digits);
            return DomainErrors.Cep.ServicoIndisponivel;
        }
    }

    private static bool IsTransient(Exception ex) =>
        ex is HttpRequestException or TaskCanceledException or ExecutionRejectedException or JsonException;

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private sealed class ViaCepPayload
    {
        [JsonPropertyName("cep")] public string? Cep { get; set; }
        [JsonPropertyName("logradouro")] public string? Logradouro { get; set; }
        [JsonPropertyName("complemento")] public string? Complemento { get; set; }
        [JsonPropertyName("bairro")] public string? Bairro { get; set; }
        [JsonPropertyName("localidade")] public string? Localidade { get; set; }
        [JsonPropertyName("uf")] public string? Uf { get; set; }

        // Pode vir como booleano (true) ou string ("true"), por isso JsonElement.
        [JsonPropertyName("erro")] public JsonElement? Erro { get; set; }
    }
}
