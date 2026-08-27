using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GestionRecherche.Data;

public sealed class ApplicationUserConfihuration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {

        builder.Property(user => user.Nom)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(user => user.Prenom)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(user => user.GradeActuel)
            .HasMaxLength(100);

        builder.Property(user => user.EstActif)
            .HasDefaultValue(true);

        builder.HasOne(user => user.Laboratoire)
            .WithMany(laboratoire => laboratoire.Membres)
            .HasForeignKey(user => user.LaboratoireId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(user => user.Uer)
            .WithMany(uer => uer.Membres)
            .HasForeignKey(user => user.UerId)
            .OnDelete(DeleteBehavior.Restrict);
    }

 

}
public sealed class UserConifugration : IEntityTypeConfiguration<Uer>
{
    
         public void Configure(EntityTypeBuilder<Uer> builder)
    {





builder.HasKey(uer => uer.Id);

        builder.Property(uer => uer.Code)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(uer => uer.Nom)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(uer => uer.EstActive)
            .HasDefaultValue(true);

        builder.HasIndex(uer => uer.Code)
            .IsUnique();

       

    builder.HasOne(uer => uer.ChefUer)
    .WithMany()
    .HasForeignKey(uer => uer.ChefUerId)
    .IsRequired()
    .OnDelete(DeleteBehavior.Restrict);
       
        
    }


}



public sealed class DemandePublicationConfiguration
    : IEntityTypeConfiguration<DemandePublication>
{
    public void Configure(EntityTypeBuilder<DemandePublication> builder)
    {
        builder.ToTable(
            "DemandesPublication",
            table => table.HasCheckConstraint(
                "CK_DemandesPublication_NumeroCycle",
                "[NumeroCycleValidation] >= 1"));

        builder.HasKey(demande => demande.Id);

        builder.Property(demande => demande.Titre)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(demande => demande.Abstract)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(demande => demande.StatutValidationInterne)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(demande => demande.StatutValidationInterne)
            .HasDefaultValue(StatutValidationInterne.EnAttenteLabo);

        builder.Property(demande => demande.StatutEditeurCourant)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(demande => demande.DateCreationUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(demande => demande.NumeroSuivi)
            .HasMaxLength(200);

        builder.Property(demande => demande.NomOriginalPreuve)
            .HasMaxLength(255);

        builder.Property(demande => demande.CheminStockagePreuve)
            .HasMaxLength(500);

        builder.Property(demande => demande.TypeContenuPreuve)
            .HasMaxLength(100);

        builder.HasOne(demande => demande.Auteur)
            .WithMany(user => user.DemandesPublication)
            .HasForeignKey(demande => demande.AuteurId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(demande => demande.AuteurId);

        builder.HasIndex(demande => demande.StatutValidationInterne);

        builder.HasIndex(demande => demande.DateAutorisationConseilUtc);

        builder.HasIndex(demande => demande.DateDerniereMiseAJourUtc);
    }
    
}


public sealed class JournalCibleDemandeConfiguration
    : IEntityTypeConfiguration<JournalCibleDemande>
{
    public void Configure(EntityTypeBuilder<JournalCibleDemande> builder)
    {
        builder.ToTable(
            "JournauxCiblesDemande",
            table =>
            {
                table.HasCheckConstraint(
                    "CK_JournauxCiblesDemande_Ordre",
                    "[OrdrePreference] >= 1");

                table.HasCheckConstraint(
                    "CK_JournauxCiblesDemande_Source",
                    "([EstNouveauJournal] = 0 AND [JournalId] IS NOT NULL) OR " +
                    "([EstNouveauJournal] = 1 " +
                    "AND [NomJournalSaisiManuellement] IS NOT NULL " +
                    "AND [EditeurSaisiManuellement] IS NOT NULL " +
                    "AND [IssnSaisiManuellement] IS NOT NULL)");
            });

        builder.HasKey(cible => cible.Id);

        builder.Property(cible => cible.NomJournalSaisiManuellement)
            .HasMaxLength(250);

        builder.Property(cible => cible.NomJournalNormalise)
            .HasMaxLength(250);

        builder.Property(cible => cible.EditeurSaisiManuellement)
            .HasMaxLength(200);

        builder.Property(cible => cible.IssnSaisiManuellement)
            .HasMaxLength(20);

        builder.Property(cible => cible.IssnNormalise)
            .HasMaxLength(20);

        builder.Property(cible => cible.StatutModeration)
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasDefaultValue(StatutModeration.NonRequise);

        builder.Property(cible => cible.DateCreationUtc)
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasOne(cible => cible.DemandePublication)
            .WithMany(demande => demande.JournauxCibles)
            .HasForeignKey(cible => cible.DemandePublicationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cible => cible.Journal)
            .WithMany(journal => journal.DemandesCibles)
            .HasForeignKey(cible => cible.JournalId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(cible => new
        {
            cible.DemandePublicationId,
            cible.OrdrePreference
        }).IsUnique();

        builder.HasIndex(cible => cible.IssnNormalise);
    }
  

}

public sealed class JournalConfiguration
    : IEntityTypeConfiguration<Journal>
{
    public void Configure(
        EntityTypeBuilder<Journal> builder)
    {
        builder.ToTable("Journaux");

        builder.HasKey(journal => journal.Id);

        builder.Property(journal => journal.Nom)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(journal => journal.NomNormalise)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(journal => journal.Editeur)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(journal => journal.Issn)
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(journal => journal.IssnNormalise)
            .HasMaxLength(20)
            .IsRequired();

        builder.HasIndex(journal => journal.IssnNormalise)
            .IsUnique();

        builder.HasIndex(journal => journal.NomNormalise);
    }
}


