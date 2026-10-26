using PetAdoption.Domain.Enums;

namespace PetAdoption.Domain.Repositories;

public sealed record PetFilter(string? Nome = null, Especie? Especie = null, PetStatus? Status = null, Guid? AbrigoId = null);

public sealed record AbrigoFilter(string? Nome = null, string? Cidade = null, string? Uf = null);
