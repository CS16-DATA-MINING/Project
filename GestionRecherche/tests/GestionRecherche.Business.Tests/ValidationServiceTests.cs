
using GestionRecherche.Data;

using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionRecherche.Business.Tests;

public sealed class ValidationServiceTests
{
    [Fact]
    public async Task ValiderAsync_CasNormal_ChangeStatutEtCreeHistorique()
    {
        await using AppDbContext dbContext =
            CreerContexte();

        DemandePublication demande =
            CreerDemande(
                StatutValidationInterne.EnAttenteLabo);

        dbContext.DemandesPublication.Add(demande);
        await dbContext.SaveChangesAsync();

        var instant = new DateTimeOffset(
            2026, 8, 15, 10, 0, 0,
            TimeSpan.Zero);

        var service = new ValidationService(
            dbContext,
            new HorlogeFixe(instant));

        ResultatValidation resultat =
            await service.ValiderAsync(
                demande.Id,
                "chef-labo-1",
                NiveauValidation.Labo,
                "Dossier conforme.");

        Assert.True(resultat.EstReussi);
        Assert.Equal(
            CodesValidation.Succes,
            resultat.Code);

        Assert.Equal(
            StatutValidationInterne.EnAttenteUer,
            demande.StatutValidationInterne);

        EtapeValidation etape =
            await dbContext.EtapesValidation
                .SingleAsync();

        Assert.Equal(demande.Id,
            etape.DemandePublicationId);

        Assert.Equal(
            "chef-labo-1",
            etape.ValidateurId);

        Assert.Equal(
            NiveauValidation.Labo,
            etape.Niveau);

        Assert.Equal(
            DecisionValidation.Validee,
            etape.Decision);

        Assert.Equal(
            "Dossier conforme.",
            etape.Commentaire);

        Assert.Equal(
            instant.UtcDateTime,
            etape.DateDecisionUtc);

        Assert.Equal(
            demande.NumeroCycleValidation,
            etape.NumeroCycleValidation);
    }

    [Fact]
    public async Task RejeterAsync_AvecCommentaire_RejetteEtHistorise()
    {
        await using AppDbContext dbContext =
            CreerContexte();

        DemandePublication demande =
            CreerDemande(
                StatutValidationInterne.EnAttenteUer);

        dbContext.DemandesPublication.Add(demande);
        await dbContext.SaveChangesAsync();

        var service = new ValidationService(
            dbContext,
            TimeProvider.System);

        ResultatValidation resultat =
            await service.RejeterAsync(
                demande.Id,
                "chef-uer-1",
                NiveauValidation.Uer,
                "L’abstract doit être corrigé.");

        Assert.True(resultat.EstReussi);

        Assert.Equal(
            StatutValidationInterne.Rejetee,
            demande.StatutValidationInterne);

        EtapeValidation etape =
            await dbContext.EtapesValidation
                .SingleAsync();

        Assert.Equal(
            NiveauValidation.Uer,
            etape.Niveau);

        Assert.Equal(
            DecisionValidation.Rejetee,
            etape.Decision);

        Assert.Equal(
            "L’abstract doit être corrigé.",
            etape.Commentaire);
    }

    [Fact]
    public async Task RejeterAsync_SansCommentaire_RefuseLaDecision()
    {
        await using AppDbContext dbContext =
            CreerContexte();

        DemandePublication demande =
            CreerDemande(
                StatutValidationInterne.EnAttenteLabo);

        dbContext.DemandesPublication.Add(demande);
        await dbContext.SaveChangesAsync();

        var service = new ValidationService(
            dbContext,
            TimeProvider.System);

        ResultatValidation resultat =
            await service.RejeterAsync(
                demande.Id,
                "chef-labo-1",
                NiveauValidation.Labo,
                "   ");

        Assert.False(resultat.EstReussi);

        Assert.Equal(
            CodesValidation.CommentaireRejetObligatoire,
            resultat.Code);

        Assert.Equal(
            StatutValidationInterne.EnAttenteLabo,
            demande.StatutValidationInterne);

        Assert.Equal(
            0,
            await dbContext.EtapesValidation.CountAsync());
    }

    [Fact]
    public async Task ValiderAsync_MauvaisNiveau_NeModifieRien()
    {
        await using AppDbContext dbContext =
            CreerContexte();

        DemandePublication demande =
            CreerDemande(
                StatutValidationInterne.EnAttenteUer);

        dbContext.DemandesPublication.Add(demande);
        await dbContext.SaveChangesAsync();

        var service = new ValidationService(
            dbContext,
            TimeProvider.System);

        ResultatValidation resultat =
            await service.ValiderAsync(
                demande.Id,
                "chef-labo-1",
                NiveauValidation.Labo,
                "Validation du niveau labo impossible.");

        Assert.False(resultat.EstReussi);

        Assert.Equal(
            CodesValidation.NiveauIncorrect,
            resultat.Code);

        Assert.Equal(
            StatutValidationInterne.EnAttenteUer,
            demande.StatutValidationInterne);

        Assert.Equal(
            0,
            await dbContext.EtapesValidation.CountAsync());
    }

    [Fact]
    public async Task ValiderAsync_DeuxFoisAuMemeNiveau_RefuseLaDeuxieme()
    {
        await using AppDbContext dbContext =
            CreerContexte();

        DemandePublication demande =
            CreerDemande(
                StatutValidationInterne.EnAttenteLabo);

        dbContext.DemandesPublication.Add(demande);
        await dbContext.SaveChangesAsync();

        var service = new ValidationService(
            dbContext,
            TimeProvider.System);

        ResultatValidation premierResultat =
            await service.ValiderAsync(
                demande.Id,
                "chef-labo-1",
                NiveauValidation.Labo,
                "Approbé");

        ResultatValidation deuxiemeResultat =
            await service.ValiderAsync(
                demande.Id,
                "chef-labo-1",
                NiveauValidation.Labo,
                "Approbé");

        Assert.True(premierResultat.EstReussi);

        Assert.False(deuxiemeResultat.EstReussi);

        Assert.Equal(
            CodesValidation.NiveauIncorrect,
            deuxiemeResultat.Code);

        Assert.Equal(
            StatutValidationInterne.EnAttenteUer,
            demande.StatutValidationInterne);

        Assert.Equal(
            1,
            await dbContext.EtapesValidation.CountAsync());
    }

    [Fact]
    public async Task ValiderAsync_DemandeIntrouvable_RetourneEchec()
    {
        await using AppDbContext dbContext =
            CreerContexte();

        var service = new ValidationService(
            dbContext,
            TimeProvider.System);

        ResultatValidation resultat =
            await service.ValiderAsync(
                99999,
                "chef-labo-1",
                NiveauValidation.Labo,
                "");

        Assert.False(resultat.EstReussi);

        Assert.Equal(
            CodesValidation.DemandeIntrouvable,
            resultat.Code);

        Assert.Equal(
            0,
            await dbContext.EtapesValidation.CountAsync());
    }

    private static AppDbContext CreerContexte()
    {
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(
                    $"validation-{Guid.NewGuid()}")
                .Options;

        return new AppDbContext(options);
    }

    private static DemandePublication CreerDemande(
        StatutValidationInterne statut)
    {
        return new DemandePublication
        {
            Titre =
                "Article scientifique de test",

            Abstract =
                "Résumé scientifique utilisé pour les tests.",

            AuteurId =
                "chercheur-1",

            StatutValidationInterne =
                statut,

            NumeroCycleValidation =
                1
        };
    }

    private sealed class HorlogeFixe : TimeProvider
    {
        private readonly DateTimeOffset _instant;

        public HorlogeFixe(DateTimeOffset instant)
        {
            _instant = instant;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _instant;
        }
    }
[Fact]
public async Task ValiderAsync_NiveauConseil_AutoriseEtRemplitDateAutorisation()
{
    await using AppDbContext dbContext = CreerContexte();

    DemandePublication demande =
        CreerDemande(StatutValidationInterne.EnAttenteConseil);

    dbContext.DemandesPublication.Add(demande);
    await dbContext.SaveChangesAsync();

    var instant = new DateTimeOffset(
        2026, 8, 20, 10, 0, 0, TimeSpan.Zero);

    var service = new ValidationService(
        dbContext, new HorlogeFixe(instant));

    ResultatValidation resultat =
        await service.ValiderAsync(
            demande.Id,
            "secretariat-1",
            NiveauValidation.Conseil,
            "Avis favorable du Conseil.");

    Assert.True(resultat.EstReussi);

    Assert.Equal(
        StatutValidationInterne.Autorisee,
        demande.StatutValidationInterne);

    // La date = l'instant EXACT de l'horloge fixe.
    // C'est le point de départ du délai D1.
    Assert.Equal(
        instant.UtcDateTime,
        demande.DateAutorisationConseilUtc);

    EtapeValidation etape =
        await dbContext.EtapesValidation.SingleAsync();

    Assert.Equal(NiveauValidation.Conseil, etape.Niveau);
    Assert.Equal(DecisionValidation.Validee, etape.Decision);
}

[Fact]
public async Task ValiderAsync_ConseilAvecNouveauJournal_PasseEnModerationSansCreerJournal()
{
    await using AppDbContext dbContext = CreerContexte();

    // Un journal officiel déjà en base (choisi par l'auteur).
    var journalOfficiel = new Journal
    {
        Nom = "Journal Officiel Existant",
        NomNormalise = "journal officiel existant",
        Editeur = "Editeur X",
        Issn = "1234-5678",
        IssnNormalise = "12345678"
    };
    dbContext.Journaux.Add(journalOfficiel);

    DemandePublication demande =
        CreerDemande(StatutValidationInterne.EnAttenteConseil);
    dbContext.DemandesPublication.Add(demande);
    await dbContext.SaveChangesAsync();

    // Cible 1 : journal EXISTANT -> ne doit pas bouger.
    var cibleExistante = new JournalCibleDemande
    {
        DemandePublicationId = demande.Id,
        JournalId = journalOfficiel.Id,
        EstNouveauJournal = false,
        OrdrePreference = 1,
        StatutModeration = StatutModeration.NonRequise
    };

    // Cible 2 : journal PROPOSÉ -> doit passer EnAttente.
    var cibleNouvelle = new JournalCibleDemande
    {
        DemandePublicationId = demande.Id,
        JournalId = null,
        EstNouveauJournal = true,
        NomJournalSaisiManuellement = "IEEE Trans on AI",
        EditeurSaisiManuellement = "IEEE",
        IssnSaisiManuellement = "9999-0001",
        OrdrePreference = 2,
        StatutModeration = StatutModeration.Proposee
    };

    dbContext.JournauxCiblesDemande.AddRange(
        cibleExistante, cibleNouvelle);
    await dbContext.SaveChangesAsync();

    var service = new ValidationService(
        dbContext,
        new HorlogeFixe(new DateTimeOffset(
            2026, 8, 20, 10, 0, 0, TimeSpan.Zero)));

    ResultatValidation resultat =
        await service.ValiderAsync(
            demande.Id,
            "secretariat-1",
            NiveauValidation.Conseil,
            null);

    Assert.True(resultat.EstReussi);

    // La proposition part en modération...
    Assert.Equal(
        StatutModeration.EnAttente,
        cibleNouvelle.StatutModeration);

    // ...le journal existant n'est pas concerné...
    Assert.Equal(
        StatutModeration.NonRequise,
        cibleExistante.StatutModeration);

    // ...et SURTOUT : aucun journal créé automatiquement.
    Assert.Equal(1, await dbContext.Journaux.CountAsync());
    Assert.Null(cibleNouvelle.JournalId);
}

[Fact]
public async Task RejeterAsync_NiveauConseil_RejetteSansDateNiModeration()
{
    await using AppDbContext dbContext = CreerContexte();

    DemandePublication demande =
        CreerDemande(StatutValidationInterne.EnAttenteConseil);
    dbContext.DemandesPublication.Add(demande);
    await dbContext.SaveChangesAsync();

    var cibleNouvelle = new JournalCibleDemande
    {
        DemandePublicationId = demande.Id,
        EstNouveauJournal = true,
        NomJournalSaisiManuellement = "Journal Proposé",
        OrdrePreference = 1,
        StatutModeration = StatutModeration.Proposee
    };
    dbContext.JournauxCiblesDemande.Add(cibleNouvelle);
    await dbContext.SaveChangesAsync();

    var service = new ValidationService(
        dbContext,
        new HorlogeFixe(new DateTimeOffset(
            2026, 8, 20, 10, 0, 0, TimeSpan.Zero)));

    ResultatValidation resultat =
        await service.RejeterAsync(
            demande.Id,
            "secretariat-1",
            NiveauValidation.Conseil,
            "Dossier scientifiquement insuffisant.");

    Assert.True(resultat.EstReussi);

    Assert.Equal(
        StatutValidationInterne.Rejetee,
        demande.StatutValidationInterne);

    // Pas d'autorisation => pas de date, pas de modération.
    Assert.Null(demande.DateAutorisationConseilUtc);
    Assert.Equal(
        StatutModeration.Proposee,
        cibleNouvelle.StatutModeration);
}


[Fact]
public async Task CircuitComplet_QuatreValidations_TermineEnAutorisee()
{
    await using AppDbContext dbContext = CreerContexte();

    DemandePublication demande =
        CreerDemande(StatutValidationInterne.EnAttenteLabo);
    dbContext.DemandesPublication.Add(demande);
    await dbContext.SaveChangesAsync();

    var service = new ValidationService(
        dbContext,
        new HorlogeFixe(new DateTimeOffset(
            2026, 8, 20, 10, 0, 0, TimeSpan.Zero)));

    await service.ValiderAsync(
        demande.Id, "chef-labo-1", NiveauValidation.Labo, null);
    await service.ValiderAsync(
        demande.Id, "chef-uer-1", NiveauValidation.Uer, null);
    await service.ValiderAsync(
        demande.Id, "chef-bureau-1", NiveauValidation.Bureau, null);

    ResultatValidation resultatFinal =
        await service.ValiderAsync(
            demande.Id, "secretariat-1",
            NiveauValidation.Conseil, null);

    Assert.True(resultatFinal.EstReussi);
    Assert.Equal(
        StatutValidationInterne.Autorisee,
        demande.StatutValidationInterne);
    Assert.NotNull(demande.DateAutorisationConseilUtc);

    // 4 niveaux = 4 traces d'historique.
    Assert.Equal(4,
        await dbContext.EtapesValidation.CountAsync());
}



}