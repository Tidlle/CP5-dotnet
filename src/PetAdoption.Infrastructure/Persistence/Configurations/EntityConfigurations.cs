using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PetAdoption.Domain.Entities;

namespace PetAdoption.Infrastructure.Persistence.Configurations;

internal sealed class AbrigoConfiguration : IEntityTypeConfiguration<Abrigo>
{
    public void Configure(EntityTypeBuilder<Abrigo> builder)
    {
        builder.ToTable("Abrigos");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Nome).HasMaxLength(Abrigo.NomeMaxLength).IsRequired();
        builder.Property(a => a.Telefone).HasMaxLength(11).IsUnicode(false).IsRequired();

        // Índice único: garante a regra de nome exclusivo e acelera a busca por nome.
        builder.HasIndex(a => a.Nome).IsUnique();

        builder.OwnsOne(a => a.Endereco, endereco =>
        {
            endereco.Property(e => e.Cep).HasColumnName("Cep").HasMaxLength(8).IsUnicode(false).IsRequired();
            endereco.Property(e => e.Logradouro).HasColumnName("Logradouro").HasMaxLength(150).IsRequired();
            endereco.Property(e => e.Numero).HasColumnName("Numero").HasMaxLength(20);
            endereco.Property(e => e.Complemento).HasColumnName("Complemento").HasMaxLength(100);
            endereco.Property(e => e.Bairro).HasColumnName("Bairro").HasMaxLength(100);
            endereco.Property(e => e.Cidade).HasColumnName("Cidade").HasMaxLength(100).IsRequired();
            endereco.Property(e => e.Uf).HasColumnName("Uf").HasMaxLength(2).IsUnicode(false).IsRequired();

            endereco.HasIndex(e => new { e.Uf, e.Cidade });
        });
        builder.Navigation(a => a.Endereco).IsRequired();

        builder.HasMany(a => a.Pets)
            .WithOne(p => p.Abrigo)
            .HasForeignKey(p => p.AbrigoId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(a => a.Pets).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class PetConfiguration : IEntityTypeConfiguration<Pet>
{
    public void Configure(EntityTypeBuilder<Pet> builder)
    {
        builder.ToTable("Pets");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Nome).HasMaxLength(Pet.NomeMaxLength).IsRequired();
        builder.Property(p => p.Raca).HasMaxLength(80);
        builder.Property(p => p.Especie).HasConversion<string>().HasMaxLength(20).IsUnicode(false);
        builder.Property(p => p.Status).HasConversion<string>().HasMaxLength(20).IsUnicode(false);

        // Índices para os filtros mais usados na listagem paginada.
        builder.HasIndex(p => new { p.AbrigoId, p.Status });
        builder.HasIndex(p => new { p.Especie, p.Status });
        builder.HasIndex(p => p.Nome);

        builder.HasOne<Usuario>()
            .WithMany()
            .HasForeignKey(p => p.AdotanteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("Usuarios");
        builder.HasKey(u => u.Id);

        builder.Property(u => u.Nome).HasMaxLength(100).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(150).IsRequired();
        builder.Property(u => u.SenhaHash).HasMaxLength(256).IsUnicode(false).IsRequired();
        builder.Property(u => u.Role).HasMaxLength(20).IsUnicode(false).IsRequired();

        builder.HasIndex(u => u.Email).IsUnique();
    }
}
