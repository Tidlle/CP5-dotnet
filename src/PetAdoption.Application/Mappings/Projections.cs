using System.Linq.Expressions;
using PetAdoption.Application.DTOs;
using PetAdoption.Domain.Entities;
using PetAdoption.Domain.Enums;

namespace PetAdoption.Application.Mappings;

/// <summary>
/// Projeções traduzidas pelo EF Core para SQL: só as colunas necessárias são lidas e nenhuma entidade é rastreada.
/// </summary>
public static class Projections
{
    public static readonly Expression<Func<Pet, PetResponse>> PetToResponse = p => new PetResponse(
        p.Id,
        p.Nome,
        p.Especie,
        p.Raca,
        p.IdadeMeses,
        p.Status,
        p.AbrigoId,
        p.Abrigo!.Nome,
        p.DataAdocao,
        p.CreatedAt);

    public static readonly Expression<Func<Abrigo, AbrigoResponse>> AbrigoToResponse = a => new AbrigoResponse(
        a.Id,
        a.Nome,
        a.Telefone,
        new EnderecoResponse(
            a.Endereco.Cep,
            a.Endereco.Logradouro,
            a.Endereco.Numero,
            a.Endereco.Complemento,
            a.Endereco.Bairro,
            a.Endereco.Cidade,
            a.Endereco.Uf),
        a.Pets.Count(),
        a.Pets.Count(p => p.Status == PetStatus.Disponivel),
        a.CreatedAt);

    public static UsuarioResponse ToResponse(this Usuario usuario) =>
        new(usuario.Id, usuario.Nome, usuario.Email, usuario.Role);
}
