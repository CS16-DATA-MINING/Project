using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionRecherche.Data;

public sealed class EtapeValidationConfiguration
    : IEntityTypeConfiguration<EtapeValidation>
{
    public void Configure(EntityTypeBuilder<EtapeValidation> builder)
    {
        builder.ToTable(
            "EtapesValidation",
            table => table.HasCheckConstraint(
                "CK_EtapesValidation_CommentaireRejet",
                "([Decision] <> N'Rejetee') OR " +
                "([Commentaire] IS NOT NULL " +
                "AND LEN(LTRIM(RTRIM([Commentaire]))) > 0)"));

        builder.HasKey(etape => etape.Id);

        builder.Property(etape => etape.Niveau)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(etape => etape.Decision)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(etape => etape.Commentaire)
            .HasMaxLength(1000);

        builder.Property(etape => etape.DateDecisionUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(etape => etape.DemandePublication)
            .WithMany(demande => demande.EtapesValidation)
            .HasForeignKey(etape => etape.DemandePublicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(etape => etape.Validateur)
            .WithMany()
            .HasForeignKey(etape => etape.ValidateurId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(etape => new
        {
            etape.DemandePublicationId,
            etape.NumeroCycleValidation,
            etape.Niveau
        }).IsUnique();

        builder.HasIndex(etape => etape.DateDecisionUtc);
    }
}

public sealed class AnomalieConfiguration
    : IEntityTypeConfiguration<Anomalie>
{
    public void Configure(EntityTypeBuilder<Anomalie> builder)
    {
        builder.ToTable(
            "Anomalies",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_Anomalies_Duree",
                    "[DureeJours] >= 0");

                table.HasCheckConstraint(
                    "CK_Anomalies_Traitement",
                    "([Statut] = N'Ouverte' " +
                    "AND [DateTraitementUtc] IS NULL " +
                    "AND [TraiteeParId] IS NULL) OR " +
                    "([Statut] = N'Traitee' " +
                    "AND [DateTraitementUtc] IS NOT NULL " +
                    "AND [TraiteeParId] IS NOT NULL " +
                    "AND [CommentaireTraitement] IS NOT NULL " +
                    "AND LEN(LTRIM(RTRIM([CommentaireTraitement]))) > 0)");
            });

        builder.HasKey(anomalie => anomalie.Id);

        builder.Property(anomalie => anomalie.Type)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(anomalie => anomalie.Statut)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(StatutAnomalie.Ouverte);

        builder.Property(anomalie => anomalie.Message)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(anomalie => anomalie.CommentaireTraitement)
            .HasMaxLength(1000);

        builder.Property(anomalie => anomalie.DateDetectionUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(anomalie => anomalie.DemandePublication)
            .WithMany(demande => demande.Anomalies)
            .HasForeignKey(anomalie => anomalie.DemandePublicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(anomalie => anomalie.TraiteePar)
            .WithMany()
            .HasForeignKey(anomalie => anomalie.TraiteeParId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(anomalie => new
        {
            anomalie.DemandePublicationId,
            anomalie.Type
        })
        .IsUnique()
        .HasFilter("[Statut] = N'Ouverte'");

        builder.HasIndex(anomalie => anomalie.DateDetectionUtc);
    }
}

public sealed class HistoriqueStatutEditeurConfiguration
    : IEntityTypeConfiguration<HistoriqueStatutEditeur>
{
    public void Configure(
        EntityTypeBuilder<HistoriqueStatutEditeur> builder)
    {
        builder.ToTable("HistoriquesStatutEditeur");

        builder.HasKey(historique => historique.Id);

        builder.Property(historique => historique.AncienStatut)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(historique => historique.NouveauStatut)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(historique => historique.Commentaire)
            .HasMaxLength(1000);

        builder.Property(historique => historique.DateChangementUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(historique => historique.DemandePublication)
            .WithMany(demande => demande.HistoriqueStatutsEditeur)
            .HasForeignKey(historique => historique.DemandePublicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(historique => historique.ModifiePar)
            .WithMany()
            .HasForeignKey(historique => historique.ModifieParId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(historique => new
        {
            historique.DemandePublicationId,
            historique.DateChangementUtc
        });
    }
}

public sealed class ParametreSystemeConfiguration
    : IEntityTypeConfiguration<ParametreSysteme>
{
    public void Configure(EntityTypeBuilder<ParametreSysteme> builder)
    {
        builder.ToTable(
            "ParametresSysteme",
            table => table.HasCheckConstraint(
                "CK_ParametresSysteme_ValeurPositive",
                "[Valeur] > 0"));

        builder.HasKey(parametre => parametre.Cle);

        builder.Property(parametre => parametre.Cle)
            .HasMaxLength(50);

        builder.Property(parametre => parametre.Unite)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(parametre => parametre.Description)
            .HasMaxLength(300)
            .IsRequired();

        builder.HasData(
            new ParametreSysteme
            {
                Cle = "MaxJournaux",
                Valeur = 3,
                Unite = UniteParametre.Nombre,
                Description =
                    "Nombre maximal de journaux ciblés par demande."
            },
            new ParametreSysteme
            {
                Cle = "D1",
                Valeur = 30,
                Unite = UniteParametre.Jours,
                Description =
                    "Délai maximal avant déclaration de la soumission réelle."
            },
            new ParametreSysteme
            {
                Cle = "D2",
                Valeur = 6,
                Unite = UniteParametre.Mois,
                Description =
                    "Délai maximal sans évolution du statut éditeur."
            });
    }
}