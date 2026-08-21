using GestionRecherche.Business;
using GestionRecherche.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionRecherche.Business.Tests;

public sealed class ValidationLaboJour5Tests
{
    private static AppDbContext CreerContexte()
{
    var options =
        new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(
                $"jour5-{Guid.NewGuid()}")
            .Options;

    return new AppDbContext(options);
}
private static ApplicationUser CreerUtilisateur(
    string id,
    string prenom,
    string nom,
    int laboratoireId)
{
    string email = $"{id}@test.local";

    return new ApplicationUser
    {
        Id = id,

        UserName = email,

        NormalizedUserName =
            email.ToUpperInvariant(),

        Email = email,

        NormalizedEmail =
            email.ToUpperInvariant(),

        EmailConfirmed = true,

        Prenom = prenom,

        Nom = nom,

        LaboratoireId = laboratoireId,

        UerId = 1,

        EstActif = true
    };
}

private static DemandePublication CreerDemande(
    string auteurId,
    string titre,
    DateTime dateCreationUtc)
{
    return new DemandePublication
    {
        AuteurId = auteurId,

        Titre = titre,

        Abstract =
            $"Résumé scientifique de {titre}.",

        StatutValidationInterne =
            StatutValidationInterne.EnAttenteLabo,

        NumeroCycleValidation = 1,

        DateCreationUtc = dateCreationUtc
    };
}
private sealed record JeuJour5(
    string ChefAId,
    string ChefBId,
    int DemandeAId,
    int DemandeBId);
    private static async Task<JeuJour5>
    InitialiserJour5Async(
        AppDbContext dbContext)
{
    const string chefAId = "chef-labo-a";
    const string chefBId = "chef-labo-b";
    const string auteurAId = "auteur-a";
    const string auteurBId = "auteur-b";

    ApplicationUser chefA =
        CreerUtilisateur(
            chefAId,
            "Chef",
            "A",
            laboratoireId: 1);

    ApplicationUser chefB =
        CreerUtilisateur(
            chefBId,
            "Chef",
            "B",
            laboratoireId: 2);

    ApplicationUser auteurA =
        CreerUtilisateur(
            auteurAId,
            "Auteur",
            "A",
            laboratoireId: 1);

    ApplicationUser auteurB =
        CreerUtilisateur(
            auteurBId,
            "Auteur",
            "B",
            laboratoireId: 2);

    var uer = new Uer
    {
        Id = 1,

        Code = "UER-TEST",

        Nom = "UER de test",

        ChefUerId = chefAId,

        EstActive = true
    };

    var laboratoireA = new Laboratoire
    {
        Id = 1,

        Code = "LAB-A",

        Nom = "Laboratoire A",

        UerId = uer.Id,

        ChefLaboId = chefAId,

        EstActif = true
    };

    var laboratoireB = new Laboratoire
    {
        Id = 2,

        Code = "LAB-B",

        Nom = "Laboratoire B",

        UerId = uer.Id,

        ChefLaboId = chefBId,

        EstActif = true
    };

    dbContext.Users.AddRange(
        chefA,
        chefB,
        auteurA,
        auteurB);

    dbContext.Uers.Add(uer);

    dbContext.Laboratoires.AddRange(
        laboratoireA,
        laboratoireB);

    DemandePublication demandeA =
        CreerDemande(
            auteurAId,
            "Article du laboratoire A",
            new DateTime(
                2026,
                8,
                18,
                8,
                0,
                0,
                DateTimeKind.Utc));

    DemandePublication demandeB =
        CreerDemande(
            auteurBId,
            "Article du laboratoire B",
            new DateTime(
                2026,
                8,
                18,
                9,
                0,
                0,
                DateTimeKind.Utc));

    dbContext.DemandesPublication.AddRange(
        demandeA,
        demandeB);

    await dbContext.SaveChangesAsync();

    return new JeuJour5(
        ChefAId: chefAId,
        ChefBId: chefBId,
        DemandeAId: demandeA.Id,
        DemandeBId: demandeB.Id);
}

 [Fact]
public async Task ListerDemandes_DeuxChefs_RetourneSeulementSonLaboratoire()
{
    await using AppDbContext dbContext =
        CreerContexte();

    JeuJour5 jeu =
        await InitialiserJour5Async(
            dbContext);

    var service = new ValidationService(
        dbContext,
        TimeProvider.System);

    IReadOnlyList<DemandeValidationListeDto>
        resultats =
            await service
                .ListerDemandesEnAttenteLaboAsync(
                    jeu.ChefAId);

    DemandeValidationListeDto resultat =
        Assert.Single(resultats);

    Assert.Equal(
        jeu.DemandeAId,
        resultat.Id);

    Assert.Equal(
        "Laboratoire A",
        resultat.StructureNom);

    Assert.DoesNotContain(
        resultats,
        demande =>
            demande.Id == jeu.DemandeBId);
}

[Fact]
public async Task
    Details_DemandeAutreLaboratoire_RetourneNull()
{
    await using AppDbContext dbContext =
        CreerContexte();

    JeuJour5 jeu =
        await InitialiserJour5Async(
            dbContext);

    var service = new ValidationService(
        dbContext,
        TimeProvider.System);

    DemandeValidationDetailsDto? resultat =
        await service.ObtenirDetailsLaboAsync(
            demandeId: jeu.DemandeBId,
            chefLaboId: jeu.ChefAId);

    Assert.Null(resultat);
}


[Fact]
public async Task
    ValiderLabo_DemandeAutreLaboratoire_RefuseLaDecision()
{
    await using AppDbContext dbContext =
        CreerContexte();

    JeuJour5 jeu =
        await InitialiserJour5Async(
            dbContext);

    var service = new ValidationService(
        dbContext,
        TimeProvider.System);

    ResultatValidation resultat =
        await service.ValiderLaboAsync(
            demandeId: jeu.DemandeBId,
            chefLaboId: jeu.ChefAId,
            commentaire:
                "Tentative de validation interdite.");

    Assert.False(resultat.EstReussi);

    Assert.Equal(
        CodesValidation.AccesInterdit,
        resultat.Code);

    dbContext.ChangeTracker.Clear();

    DemandePublication demande =
        await dbContext.DemandesPublication
            .SingleAsync(element =>
                element.Id == jeu.DemandeBId);

    Assert.Equal(
        StatutValidationInterne.EnAttenteLabo,
        demande.StatutValidationInterne);

    int nombreHistoriques =
        await dbContext.EtapesValidation
            .CountAsync();

    Assert.Equal(
        0,
        nombreHistoriques);
}

[Fact]
public async Task
    ValiderLabo_BonChef_TransmetALuerEtHistorise()
{
    await using AppDbContext dbContext =
        CreerContexte();

    JeuJour5 jeu =
        await InitialiserJour5Async(
            dbContext);

    var service = new ValidationService(
        dbContext,
        TimeProvider.System);

    ResultatValidation resultat =
        await service.ValiderLaboAsync(
            demandeId: jeu.DemandeAId,
            chefLaboId: jeu.ChefAId,
            commentaire:
                "Dossier conforme.");

    Assert.True(resultat.EstReussi);

    Assert.Equal(
        CodesValidation.Succes,
        resultat.Code);

    dbContext.ChangeTracker.Clear();

    DemandePublication demande =
        await dbContext.DemandesPublication
            .SingleAsync(element =>
                element.Id == jeu.DemandeAId);

    Assert.Equal(
        StatutValidationInterne.EnAttenteUer,
        demande.StatutValidationInterne);

    EtapeValidation historique =
        await dbContext.EtapesValidation
            .SingleAsync();

    Assert.Equal(
        jeu.DemandeAId,
        historique.DemandePublicationId);

    Assert.Equal(
        jeu.ChefAId,
        historique.ValidateurId);

    Assert.Equal(
        NiveauValidation.Labo,
        historique.Niveau);

    Assert.Equal(
        DecisionValidation.Validee,
        historique.Decision);

    Assert.Equal(
        "Dossier conforme.",
        historique.Commentaire);
}



[Fact]
public async Task
    RejeterLabo_SansCommentaire_RefuseLaDecision()
{
    await using AppDbContext dbContext =
        CreerContexte();

    JeuJour5 jeu =
        await InitialiserJour5Async(
            dbContext);

    var service = new ValidationService(
        dbContext,
        TimeProvider.System);

    ResultatValidation resultat =
        await service.RejeterLaboAsync(
            demandeId: jeu.DemandeAId,
            chefLaboId: jeu.ChefAId,
            commentaire: "   ");

    Assert.False(resultat.EstReussi);

    Assert.Equal(
        CodesValidation
            .CommentaireRejetObligatoire,
        resultat.Code);

    Assert.Equal(
        0,
        await dbContext.EtapesValidation
            .CountAsync());
}




}
