using GestionRecherche.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionRecherche.Business.Tests;

public sealed class RoutingServiceTests
{
    [Fact]
    public async Task
        DeterminerRoutageAsync_DeuxLaboratoires_RetourneDeuxChefsDifferents()
    {
        await using AppDbContext dbContext =
            CreerContexte();

        ApplicationUser chefLaboA =
            CreerUtilisateur("chef-labo-a");

        ApplicationUser chefLaboB =
            CreerUtilisateur("chef-labo-b");

        ApplicationUser chefUerA =
            CreerUtilisateur("chef-uer-a");

        ApplicationUser chefUerB =
            CreerUtilisateur("chef-uer-b");

        ApplicationUser auteurA =
            CreerUtilisateur(
                "auteur-a",
                laboratoireId: 1,
                uerId: 1);

        ApplicationUser auteurB =
            CreerUtilisateur(
                "auteur-b",
                laboratoireId: 2,
                uerId: 2);

        dbContext.Users.AddRange(
            chefLaboA,
            chefLaboB,
            chefUerA,
            chefUerB,
            auteurA,
            auteurB);

        dbContext.Uers.AddRange(
            CreerUer(1, "chef-uer-a"),
            CreerUer(2, "chef-uer-b"));

        dbContext.Laboratoires.AddRange(
            CreerLaboratoire(
                1,
                1,
                "chef-labo-a"),

            CreerLaboratoire(
                2,
                2,
                "chef-labo-b"));

        DemandePublication demandeA =
            CreerDemande(
                "auteur-a",
                StatutValidationInterne.EnAttenteLabo,
                "Article A");

        DemandePublication demandeB =
            CreerDemande(
                "auteur-b",
                StatutValidationInterne.EnAttenteLabo,
                "Article B");

        dbContext.DemandesPublication.AddRange(
            demandeA,
            demandeB);

        await dbContext.SaveChangesAsync();

        var service = new RoutingService(dbContext);

        ResultatRoutage resultatA =
            await service.DeterminerRoutageAsync(
                demandeA.Id);

        ResultatRoutage resultatB =
            await service.DeterminerRoutageAsync(
                demandeB.Id);

        Assert.True(resultatA.EstReussi);
        Assert.True(resultatB.EstReussi);

        Assert.Equal(
            NiveauValidation.Labo,
            resultatA.Niveau!.Value);

        Assert.Equal(
            NiveauValidation.Labo,
            resultatB.Niveau!.Value);

        Assert.Equal(
            "chef-labo-a",
            resultatA.ValidateurId);

        Assert.Equal(
            "chef-labo-b",
            resultatB.ValidateurId);

        Assert.NotEqual(
            resultatA.ValidateurId,
            resultatB.ValidateurId);
    }

    [Fact]
    public async Task
        DeterminerRoutageAsync_NiveauUer_RetourneChefUerAuteur()
    {
        await using AppDbContext dbContext =
            CreerContexte();

        ApplicationUser chefUer =
            CreerUtilisateur("chef-uer-1");

        ApplicationUser auteur =
            CreerUtilisateur(
                "auteur-1",
                uerId: 1);

        dbContext.Users.AddRange(
            chefUer,
            auteur);

        dbContext.Uers.Add(
            CreerUer(1, "chef-uer-1"));

        DemandePublication demande =
            CreerDemande(
                auteur.Id,
                StatutValidationInterne.EnAttenteUer);

        dbContext.DemandesPublication.Add(demande);

        await dbContext.SaveChangesAsync();

        var service = new RoutingService(dbContext);

        ResultatRoutage resultat =
            await service.DeterminerRoutageAsync(
                demande.Id);

        Assert.True(resultat.EstReussi);

        Assert.Equal(
            NiveauValidation.Uer,
            resultat.Niveau!.Value);

        Assert.Equal(
            "chef-uer-1",
            resultat.ValidateurId);
    }

    [Fact]
    public async Task
        DeterminerRoutageAsync_NiveauBureau_RetourneChefBureau()
    {
        await using AppDbContext dbContext =
            CreerContexte();

        ApplicationUser auteur =
            CreerUtilisateur("auteur-1");

        ApplicationUser chefBureau =
            CreerUtilisateur("chef-bureau-1");

        dbContext.Users.AddRange(
            auteur,
            chefBureau);

        AjouterRole(
            dbContext,
            "ChefBureau",
            chefBureau.Id);

        DemandePublication demande =
            CreerDemande(
                auteur.Id,
                StatutValidationInterne.EnAttenteBureau);

        dbContext.DemandesPublication.Add(demande);

        await dbContext.SaveChangesAsync();

        var service = new RoutingService(dbContext);

        ResultatRoutage resultat =
            await service.DeterminerRoutageAsync(
                demande.Id);

        Assert.True(resultat.EstReussi);

        Assert.Equal(
            NiveauValidation.Bureau,
            resultat.Niveau!.Value);

        Assert.Equal(
            chefBureau.Id,
            resultat.ValidateurId);
    }

    [Fact]
    public async Task
        DeterminerRoutageAsync_NiveauConseil_RetourneSecretariatConseil()
    {
        await using AppDbContext dbContext =
            CreerContexte();

        ApplicationUser auteur =
            CreerUtilisateur("auteur-1");

        ApplicationUser secretaire =
            CreerUtilisateur("secretariat-1");

        dbContext.Users.AddRange(
            auteur,
            secretaire);

        AjouterRole(
            dbContext,
            "SecretariatConseil",
            secretaire.Id);

        DemandePublication demande =
            CreerDemande(
                auteur.Id,
                StatutValidationInterne.EnAttenteConseil);

        dbContext.DemandesPublication.Add(demande);

        await dbContext.SaveChangesAsync();

        var service = new RoutingService(dbContext);

        ResultatRoutage resultat =
            await service.DeterminerRoutageAsync(
                demande.Id);

        Assert.True(resultat.EstReussi);

        Assert.Equal(
            NiveauValidation.Conseil,
            resultat.Niveau!.Value);

        Assert.Equal(
            secretaire.Id,
            resultat.ValidateurId);
    }

    [Fact]
    public async Task
        DeterminerRoutageAsync_AuteurSansLaboratoire_RetourneEchec()
    {
        await using AppDbContext dbContext =
            CreerContexte();

        ApplicationUser auteur =
            CreerUtilisateur("auteur-1");

        dbContext.Users.Add(auteur);

        DemandePublication demande =
            CreerDemande(
                auteur.Id,
                StatutValidationInterne.EnAttenteLabo);

        dbContext.DemandesPublication.Add(demande);

        await dbContext.SaveChangesAsync();

        var service = new RoutingService(dbContext);

        ResultatRoutage resultat =
            await service.DeterminerRoutageAsync(
                demande.Id);

        Assert.False(resultat.EstReussi);

        Assert.Equal(
            CodesRoutage.AuteurSansLaboratoire,
            resultat.Code);
    }

    [Fact]
    public async Task
        DeterminerRoutageAsync_LaboratoireSansChef_RetourneEchec()
    {
        await using AppDbContext dbContext =
            CreerContexte();

        ApplicationUser chefUer =
            CreerUtilisateur("chef-uer-1");

        ApplicationUser auteur =
            CreerUtilisateur(
                "auteur-1",
                laboratoireId: 1,
                uerId: 1);

        dbContext.Users.AddRange(
            chefUer,
            auteur);

        dbContext.Uers.Add(
            CreerUer(1, chefUer.Id));

        dbContext.Laboratoires.Add(
            CreerLaboratoire(
                1,
                1,
                chefLaboId: null));

        DemandePublication demande =
            CreerDemande(
                auteur.Id,
                StatutValidationInterne.EnAttenteLabo);

        dbContext.DemandesPublication.Add(demande);

        await dbContext.SaveChangesAsync();

        var service = new RoutingService(dbContext);

        ResultatRoutage resultat =
            await service.DeterminerRoutageAsync(
                demande.Id);

        Assert.False(resultat.EstReussi);

        Assert.Equal(
            CodesRoutage.LaboratoireSansChef,
            resultat.Code);
    }

    [Fact]
    public async Task
        DeterminerRoutageAsync_AuteurSansUer_RetourneEchec()
    {
        await using AppDbContext dbContext =
            CreerContexte();

        ApplicationUser auteur =
            CreerUtilisateur("auteur-1");

        dbContext.Users.Add(auteur);

        DemandePublication demande =
            CreerDemande(
                auteur.Id,
                StatutValidationInterne.EnAttenteUer);

        dbContext.DemandesPublication.Add(demande);

        await dbContext.SaveChangesAsync();

        var service = new RoutingService(dbContext);

        ResultatRoutage resultat =
            await service.DeterminerRoutageAsync(
                demande.Id);

        Assert.False(resultat.EstReussi);

        Assert.Equal(
            CodesRoutage.AuteurSansUer,
            resultat.Code);
    }

    [Fact]
    public async Task
        DeterminerRoutageAsync_UerSansChef_RetourneEchec()
    {
        await using AppDbContext dbContext =
            CreerContexte();

        ApplicationUser auteur =
            CreerUtilisateur(
                "auteur-1",
                uerId: 1);

        dbContext.Users.Add(auteur);

        dbContext.Uers.Add(
            CreerUer(
                1,
                chefUerId: string.Empty));

        DemandePublication demande =
            CreerDemande(
                auteur.Id,
                StatutValidationInterne.EnAttenteUer);

        dbContext.DemandesPublication.Add(demande);

        await dbContext.SaveChangesAsync();

        var service = new RoutingService(dbContext);

        ResultatRoutage resultat =
            await service.DeterminerRoutageAsync(
                demande.Id);

        Assert.False(resultat.EstReussi);

        Assert.Equal(
            CodesRoutage.UerSansChef,
            resultat.Code);
    }

    [Theory]
    [InlineData(
        StatutValidationInterne.EnAttenteBureau,
        CodesRoutage.RoleChefBureauManquant)]
    [InlineData(
        StatutValidationInterne.EnAttenteConseil,
        CodesRoutage.RoleSecretariatConseilManquant)]
    public async Task
        DeterminerRoutageAsync_RoleGlobalManquant_RetourneEchec(
            StatutValidationInterne statut,
            string codeAttendu)
    {
        await using AppDbContext dbContext =
            CreerContexte();

        ApplicationUser auteur =
            CreerUtilisateur("auteur-1");

        dbContext.Users.Add(auteur);

        DemandePublication demande =
            CreerDemande(
                auteur.Id,
                statut);

        dbContext.DemandesPublication.Add(demande);

        await dbContext.SaveChangesAsync();

        var service = new RoutingService(dbContext);

        ResultatRoutage resultat =
            await service.DeterminerRoutageAsync(
                demande.Id);

        Assert.False(resultat.EstReussi);
        Assert.Equal(codeAttendu, resultat.Code);
        Assert.Null(resultat.ValidateurId);
    }

    [Theory]
    [InlineData(StatutValidationInterne.Autorisee)]
    [InlineData(StatutValidationInterne.Rejetee)]
    public async Task
        DeterminerRoutageAsync_DemandeTerminee_RetourneEchec(
            StatutValidationInterne statut)
    {
        await using AppDbContext dbContext =
            CreerContexte();

        ApplicationUser auteur =
            CreerUtilisateur("auteur-1");

        dbContext.Users.Add(auteur);

        DemandePublication demande =
            CreerDemande(
                auteur.Id,
                statut);

        dbContext.DemandesPublication.Add(demande);

        await dbContext.SaveChangesAsync();

        var service = new RoutingService(dbContext);

        ResultatRoutage resultat =
            await service.DeterminerRoutageAsync(
                demande.Id);

        Assert.False(resultat.EstReussi);

        Assert.Equal(
            CodesRoutage.DemandeTerminee,
            resultat.Code);
    }

    private static AppDbContext CreerContexte()
    {
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(
                    $"routing-{Guid.NewGuid()}")
                .Options;

        return new AppDbContext(options);
    }

    private static ApplicationUser CreerUtilisateur(
        string id,
        int? laboratoireId = null,
        int? uerId = null)
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
            Nom = id,
            Prenom = "Test",
            EstActif = true,
            LaboratoireId = laboratoireId,
            UerId = uerId
        };
    }

    private static Uer CreerUer(
        int id,
        string chefUerId)
    {
        return new Uer
        {
            Id = id,
            Code = $"UER-{id}",
            Nom = $"UER {id}",
            ChefUerId = chefUerId,
            EstActive = true
        };
    }

    private static Laboratoire CreerLaboratoire(
        int id,
        int uerId,
        string? chefLaboId)
    {
        return new Laboratoire
        {
            Id = id,
            Code = $"LAB-{id}",
            Nom = $"Laboratoire {id}",
            UerId = uerId,
            ChefLaboId = chefLaboId,
            EstActif = true
        };
    }

    private static DemandePublication CreerDemande(
        string auteurId,
        StatutValidationInterne statut,
        string titre = "Article scientifique de test")
    {
        return new DemandePublication
        {
            Titre = titre,
            Abstract =
                "Résumé scientifique utilisé pour le test.",
            AuteurId = auteurId,
            StatutValidationInterne = statut,
            NumeroCycleValidation = 1
        };
    }

    private static void AjouterRole(
        AppDbContext dbContext,
        string nomRole,
        string utilisateurId)
    {
        string roleId =
            $"role-{nomRole.ToLowerInvariant()}";

        dbContext.Roles.Add(
            new IdentityRole
            {
                Id = roleId,
                Name = nomRole,
                NormalizedName =
                    nomRole.ToUpperInvariant()
            });

        dbContext.UserRoles.Add(
            new IdentityUserRole<string>
            {
                RoleId = roleId,
                UserId = utilisateurId
            });
    }
}