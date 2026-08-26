using GestionRecherche.Business;
using GestionRecherche.Data;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace GestionRecherche.Business.Tests;

public sealed class AnomalieServiceTests
{
    /*
     * Tous les tests utilisent une date fixe.
     *
     * Cela évite que les résultats changent selon
     * la date réelle d'exécution des tests.
     */
    private static readonly DateTimeOffset Maintenant =
        new DateTimeOffset(
            2026,
            8,
            24,
            10,
            0,
            0,
            TimeSpan.Zero);

    /*
     * Vérifie que la liste des anomalies ouvertes
     * applique correctement :
     *
     * 1. le filtre du type d'anomalie ;
     * 2. le filtre du laboratoire.
     */
    [Fact]
    public async Task ListerOuvertesAsync_FiltreTypeEtLaboratoire()
    {
        await using AppDbContext dbContext =
            CreerContexte();

        Scenario scenario =
            await AjouterScenarioAsync(dbContext);

        /*
         * On ajoute une deuxième anomalie :
         *
         * - d'un autre type ;
         * - appartenant à un autre laboratoire.
         *
         * Elle ne doit pas apparaître dans le résultat.
         */
        await AjouterAutreAnomalieAsync(dbContext);

        AnomalieService service =
            CreerService(dbContext);

        IReadOnlyList<AnomalieDto> anomalies =
            await service.ListerOuvertesAsync(
                new FiltreAnomalies(
                    TypeAnomalie.Stagnation,
                    scenario.LaboratoireId));

        AnomalieDto anomalie =
            Assert.Single(anomalies);

        Assert.Equal(
            scenario.AnomalieId,
            anomalie.Id);

        Assert.Equal(
            TypeAnomalie.Stagnation,
            anomalie.Type);

        Assert.Equal(
            StatutAnomalie.Ouverte,
            anomalie.Statut);

        Assert.Equal(
            "Ada Lovelace",
            anomalie.AuteurNomComplet);

        Assert.Equal(
            "Laboratoire Intelligence Artificielle",
            anomalie.LaboratoireNom);

        Assert.Equal(
            StatutEditeur.UnderReview,
            anomalie.StatutEditeurCourant);
    }

    /*
     * Vérifie le scénario normal :
     *
     * un Chef de Bureau actif traite une anomalie
     * avec un commentaire valide.
     */
    [Fact]
    public async Task MarquerTraiteeAsync_CommentaireValide_TraceTraitement()
    {
        await using AppDbContext dbContext =
            CreerContexte();

        Scenario scenario =
            await AjouterScenarioAsync(dbContext);

        AnomalieService service =
            CreerService(dbContext);

        ResultatTraitementAnomalie resultat =
            await service.MarquerTraiteeAsync(
                scenario.AnomalieId,
                scenario.ChefBureauId,
                "  Auteur contacté et mise à jour demandée.  ");

        Assert.True(resultat.EstReussi);

        Assert.Equal(
            CodesTraitementAnomalie.Succes,
            resultat.Code);

        Anomalie anomalie =
            await dbContext.Anomalies
                .SingleAsync(
                    element =>
                        element.Id ==
                        scenario.AnomalieId);

        /*
         * L'anomalie doit maintenant être traitée.
         */
        Assert.Equal(
            StatutAnomalie.Traitee,
            anomalie.Statut);

        /*
         * La date doit être celle de l'horloge fixe.
         */
        Assert.NotNull(
            anomalie.DateTraitementUtc);

        Assert.Equal(
            Maintenant.UtcDateTime,
            anomalie.DateTraitementUtc.Value);

        /*
         * L'identifiant du Chef de Bureau doit
         * être enregistré.
         */
        Assert.Equal(
            scenario.ChefBureauId,
            anomalie.TraiteeParId);

        /*
         * Le service doit supprimer les espaces
         * inutiles au début et à la fin.
         */
        Assert.Equal(
            "Auteur contacté et mise à jour demandée.",
            anomalie.CommentaireTraitement);

        /*
         * L'anomalie ne doit plus apparaître
         * dans la liste des anomalies ouvertes.
         */
        IReadOnlyList<AnomalieDto> anomaliesOuvertes =
            await service.ListerOuvertesAsync(
                new FiltreAnomalies());

        Assert.Empty(anomaliesOuvertes);

        /*
         * Elle doit rester visible dans l'historique.
         */
        IReadOnlyList<AnomalieDto> anomaliesTraitees =
            await service.ListerTraiteesAsync(
                new FiltreAnomalies());

        AnomalieDto anomalieHistorique =
            Assert.Single(anomaliesTraitees);

        Assert.Equal(
            scenario.AnomalieId,
            anomalieHistorique.Id);

        Assert.Equal(
            StatutAnomalie.Traitee,
            anomalieHistorique.Statut);

        Assert.Equal(
            "Marie Bureau",
            anomalieHistorique.TraiteeParNomComplet);

        Assert.Equal(
            "Auteur contacté et mise à jour demandée.",
            anomalieHistorique.CommentaireTraitement);
    }

    /*
     * Vérifie qu'un commentaire vide ou composé
     * uniquement d'espaces est refusé.
     */
    [Fact]
    public async Task MarquerTraiteeAsync_CommentaireVide_Echoue()
    {
        await using AppDbContext dbContext =
            CreerContexte();

        Scenario scenario =
            await AjouterScenarioAsync(dbContext);

        AnomalieService service =
            CreerService(dbContext);

        ResultatTraitementAnomalie resultat =
            await service.MarquerTraiteeAsync(
                scenario.AnomalieId,
                scenario.ChefBureauId,
                "   ");

        Assert.False(resultat.EstReussi);

        Assert.Equal(
            CodesTraitementAnomalie.CommentaireObligatoire,
            resultat.Code);

        /*
         * Vérifier que la base n'a pas été modifiée.
         */
        Anomalie anomalie =
            await dbContext.Anomalies
                .SingleAsync(
                    element =>
                        element.Id ==
                        scenario.AnomalieId);

        Assert.Equal(
            StatutAnomalie.Ouverte,
            anomalie.Statut);

        Assert.Null(
            anomalie.DateTraitementUtc);

        Assert.Null(
            anomalie.TraiteeParId);

        Assert.Null(
            anomalie.CommentaireTraitement);
    }

    /*
     * Vérifie la défense en profondeur du service.
     *
     * Même si un Chercheur appelle directement
     * la méthode, le service doit refuser.
     */
    [Fact]
    public async Task MarquerTraiteeAsync_Chercheur_EchoueAccesInterdit()
    {
        await using AppDbContext dbContext =
            CreerContexte();

        Scenario scenario =
            await AjouterScenarioAsync(dbContext);

        AnomalieService service =
            CreerService(dbContext);

        ResultatTraitementAnomalie resultat =
            await service.MarquerTraiteeAsync(
                scenario.AnomalieId,
                scenario.ChercheurId,
                "Tentative non autorisée.");

        Assert.False(resultat.EstReussi);

        Assert.Equal(
            CodesTraitementAnomalie.AccesInterdit,
            resultat.Code);

        /*
         * L'anomalie doit rester ouverte.
         */
        Anomalie anomalie =
            await dbContext.Anomalies
                .SingleAsync(
                    element =>
                        element.Id ==
                        scenario.AnomalieId);

        Assert.Equal(
            StatutAnomalie.Ouverte,
            anomalie.Statut);

        Assert.Null(
            anomalie.DateTraitementUtc);

        Assert.Null(
            anomalie.TraiteeParId);

        Assert.Null(
            anomalie.CommentaireTraitement);
    }

    /*
     * Vérifie qu'une anomalie déjà fermée
     * ne peut pas être traitée une deuxième fois.
     */
    [Fact]
    public async Task MarquerTraiteeAsync_DejaTraitee_Echoue()
    {
        await using AppDbContext dbContext =
            CreerContexte();

        Scenario scenario =
            await AjouterScenarioAsync(dbContext);

        Anomalie anomalie =
            await dbContext.Anomalies
                .SingleAsync(
                    element =>
                        element.Id ==
                        scenario.AnomalieId);

        /*
         * On simule un premier traitement.
         */
        anomalie.Statut =
            StatutAnomalie.Traitee;

        anomalie.DateTraitementUtc =
            Maintenant.UtcDateTime;

        anomalie.TraiteeParId =
            scenario.ChefBureauId;

        anomalie.CommentaireTraitement =
            "Premier traitement.";

        await dbContext.SaveChangesAsync();

        AnomalieService service =
            CreerService(dbContext);

        /*
         * On tente un deuxième traitement.
         */
        ResultatTraitementAnomalie resultat =
            await service.MarquerTraiteeAsync(
                scenario.AnomalieId,
                scenario.ChefBureauId,
                "Deuxième traitement interdit.");

        Assert.False(resultat.EstReussi);

        Assert.Equal(
            CodesTraitementAnomalie.DejaTraitee,
            resultat.Code);

        /*
         * Le premier commentaire doit être conservé.
         */
        Assert.Equal(
            "Premier traitement.",
            anomalie.CommentaireTraitement);

        Assert.Equal(
            StatutAnomalie.Traitee,
            anomalie.Statut);
    }

    /*
     * Vérifie le cas d'un identifiant inexistant.
     */
    [Fact]
    public async Task MarquerTraiteeAsync_AnomalieInexistante_Echoue()
    {
        await using AppDbContext dbContext =
            CreerContexte();

        Scenario scenario =
            await AjouterScenarioAsync(dbContext);

        AnomalieService service =
            CreerService(dbContext);

        ResultatTraitementAnomalie resultat =
            await service.MarquerTraiteeAsync(
                anomalieId: 999999,
                chefBureauId: scenario.ChefBureauId,
                commentaire:
                    "Tentative sur une anomalie inexistante.");

        Assert.False(resultat.EstReussi);

        Assert.Equal(
            CodesTraitementAnomalie.AnomalieIntrouvable,
            resultat.Code);
    }

    /*
     * Vérifie la récupération d'une anomalie
     * précise pour la page Details.
     */
    [Fact]
    public async Task ObtenirAsync_AnomalieExistante_RetourneDetails()
    {
        await using AppDbContext dbContext =
            CreerContexte();

        Scenario scenario =
            await AjouterScenarioAsync(dbContext);

        AnomalieService service =
            CreerService(dbContext);

        AnomalieDto? anomalie =
            await service.ObtenirAsync(
                scenario.AnomalieId);

        Assert.NotNull(anomalie);

        Assert.Equal(
            scenario.AnomalieId,
            anomalie.Id);

        Assert.Equal(
            "Article stagnant",
            anomalie.TitreDemande);

        Assert.Equal(
            "Ada Lovelace",
            anomalie.AuteurNomComplet);

        Assert.Equal(
            TypeAnomalie.Stagnation,
            anomalie.Type);

        Assert.Equal(
            StatutAnomalie.Ouverte,
            anomalie.Statut);

        Assert.Contains(
            "D2 de 6 mois",
            anomalie.Message);
    }

    /*
     * Vérifie que ObtenirAsync retourne null
     * pour un identifiant inexistant.
     */
    [Fact]
    public async Task ObtenirAsync_AnomalieInexistante_RetourneNull()
    {
        await using AppDbContext dbContext =
            CreerContexte();

        AnomalieService service =
            CreerService(dbContext);

        AnomalieDto? anomalie =
            await service.ObtenirAsync(999999);

        Assert.Null(anomalie);
    }

    /*
     * Crée le service avec :
     *
     * - le contexte de test ;
     * - une horloge fixe ;
     * - un logger qui n'écrit rien.
     */
    private static AnomalieService CreerService(
        AppDbContext dbContext)
    {
        return new AnomalieService(
            dbContext,
            new HorlogeFixe(Maintenant),
            NullLogger<AnomalieService>.Instance);
    }

    /*
     * Chaque test possède sa propre base InMemory.
     *
     * Guid.NewGuid() garantit qu'aucun test ne
     * partage les données d'un autre test.
     */
    private static AppDbContext CreerContexte()
    {
        DbContextOptions<AppDbContext> options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(
                    $"anomalies-jour14-{Guid.NewGuid()}")
                .Options;

        return new AppDbContext(options);
    }

    /*
     * Prépare les données principales :
     *
     * - un rôle ChefBureau ;
     * - un Chef de Bureau actif ;
     * - un Chercheur sans rôle ChefBureau ;
     * - un auteur ;
     * - un laboratoire ;
     * - une demande ;
     * - une anomalie D2 ouverte.
     */
    private static async Task<Scenario> AjouterScenarioAsync(
        AppDbContext dbContext)
    {
        const string chefBureauId =
            "chef-bureau-1";

        const string chercheurId =
            "chercheur-1";

        const string auteurId =
            "auteur-1";

        const string roleId =
            "role-chef-bureau";

        const int laboratoireId = 10;
        const int demandeId = 100;
        const int anomalieId = 500;

        /*
         * Ajouter le rôle Identity.
         */
        dbContext.Roles.Add(
            new IdentityRole
            {
                Id = roleId,
                Name = "ChefBureau",
                NormalizedName = "CHEFBUREAU"
            });

        /*
         * Ajouter les utilisateurs.
         */
        dbContext.Users.AddRange(
            new ApplicationUser
            {
                Id = chefBureauId,

                UserName =
                    "chef.bureau@test.local",

                NormalizedUserName =
                    "CHEF.BUREAU@TEST.LOCAL",

                Email =
                    "chef.bureau@test.local",

                NormalizedEmail =
                    "CHEF.BUREAU@TEST.LOCAL",

                Prenom = "Marie",
                Nom = "Bureau",

                EstActif = true
            },
            new ApplicationUser
            {
                Id = chercheurId,

                UserName =
                    "chercheur@test.local",

                NormalizedUserName =
                    "CHERCHEUR@TEST.LOCAL",

                Email =
                    "chercheur@test.local",

                NormalizedEmail =
                    "CHERCHEUR@TEST.LOCAL",

                Prenom = "Alan",
                Nom = "Chercheur",

                EstActif = true
            },
            new ApplicationUser
            {
                Id = auteurId,

                UserName =
                    "auteur@test.local",

                NormalizedUserName =
                    "AUTEUR@TEST.LOCAL",

                Email =
                    "auteur@test.local",

                NormalizedEmail =
                    "AUTEUR@TEST.LOCAL",

                Prenom = "Ada",
                Nom = "Lovelace",

                LaboratoireId =
                    laboratoireId,

                EstActif = true
            });

        /*
         * Affecter uniquement le premier utilisateur
         * au rôle ChefBureau.
         */
        dbContext.UserRoles.Add(
            new IdentityUserRole<string>
            {
                UserId = chefBureauId,
                RoleId = roleId
            });

        /*
         * Ajouter le laboratoire.
         *
         * Le fournisseur InMemory ne vérifie pas
         * les contraintes SQL de clé étrangère UER.
         */
        dbContext.Laboratoires.Add(
            new Laboratoire
            {
                Id = laboratoireId,
                Code = "LIA",

                Nom =
                    "Laboratoire Intelligence Artificielle",

                UerId = 1,
                EstActif = true
            });

        /*
         * Ajouter une demande autorisée et soumise.
         */
        dbContext.DemandesPublication.Add(
            new DemandePublication
            {
                Id = demandeId,

                Titre =
                    "Article stagnant",

                Abstract =
                    "Résumé scientifique.",

                AuteurId = auteurId,

                StatutValidationInterne =
                    StatutValidationInterne.Autorisee,

                NumeroCycleValidation = 1,

                DateAutorisationConseilUtc =
                    Utc(2025, 12, 1, 10),

                DateSoumissionReelleUtc =
                    Utc(2025, 12, 15, 10),

                StatutEditeurCourant =
                    StatutEditeur.UnderReview,

                DateDerniereMiseAJourUtc =
                    Utc(2026, 1, 1, 10)
            });

        /*
         * Ajouter l'anomalie D2 ouverte.
         */
        dbContext.Anomalies.Add(
            new Anomalie
            {
                Id = anomalieId,

                DemandePublicationId =
                    demandeId,

                Type =
                    TypeAnomalie.Stagnation,

                Statut =
                    StatutAnomalie.Ouverte,

                DateDetectionUtc =
                    Utc(2026, 8, 24, 9),

                DureeJours = 234,

                Message =
                    "La demande est sans évolution. " +
                    "Le délai D2 de 6 mois est dépassé."
            });

        await dbContext.SaveChangesAsync();

        return new Scenario(
            ChefBureauId: chefBureauId,
            ChercheurId: chercheurId,
            LaboratoireId: laboratoireId,
            AnomalieId: anomalieId);
    }

    /*
     * Ajoute une anomalie D1 appartenant à
     * un autre laboratoire.
     *
     * Elle permet de vérifier les filtres.
     */
    private static async Task AjouterAutreAnomalieAsync(
        AppDbContext dbContext)
    {
        const int laboratoireId = 20;
        const string auteurId = "auteur-2";
        const int demandeId = 200;

        dbContext.Laboratoires.Add(
            new Laboratoire
            {
                Id = laboratoireId,
                Code = "LCH",

                Nom =
                    "Laboratoire Chimie",

                UerId = 2,
                EstActif = true
            });

        dbContext.Users.Add(
            new ApplicationUser
            {
                Id = auteurId,

                UserName =
                    "auteur2@test.local",

                NormalizedUserName =
                    "AUTEUR2@TEST.LOCAL",

                Email =
                    "auteur2@test.local",

                NormalizedEmail =
                    "AUTEUR2@TEST.LOCAL",

                Prenom = "Grace",
                Nom = "Hopper",

                LaboratoireId =
                    laboratoireId,

                EstActif = true
            });

        dbContext.DemandesPublication.Add(
            new DemandePublication
            {
                Id = demandeId,

                Titre =
                    "Article non soumis",

                Abstract =
                    "Autre résumé scientifique.",

                AuteurId = auteurId,

                StatutValidationInterne =
                    StatutValidationInterne.Autorisee,

                NumeroCycleValidation = 1,

                DateAutorisationConseilUtc =
                    Utc(2026, 6, 1, 10)
            });

        dbContext.Anomalies.Add(
            new Anomalie
            {
                Id = 600,

                DemandePublicationId =
                    demandeId,

                Type =
                    TypeAnomalie.NonSoumission,

                Statut =
                    StatutAnomalie.Ouverte,

                DateDetectionUtc =
                    Utc(2026, 8, 23, 10),

                DureeJours = 54,

                Message =
                    "Le délai D1 est dépassé."
            });

        await dbContext.SaveChangesAsync();
    }

    /*
     * Construit une date UTC de manière explicite.
     */
    private static DateTime Utc(
        int annee,
        int mois,
        int jour,
        int heure)
    {
        return new DateTime(
            annee,
            mois,
            jour,
            heure,
            0,
            0,
            DateTimeKind.Utc);
    }

    /*
     * Regroupe les identifiants utilisés
     * par chaque test.
     */
    private sealed record Scenario(
        string ChefBureauId,
        string ChercheurId,
        int LaboratoireId,
        int AnomalieId);

    /*
     * Horloge utilisée pour obtenir toujours
     * la même date dans les tests.
     */
    private sealed class HorlogeFixe : TimeProvider
    {
        private readonly DateTimeOffset _instant;

        public HorlogeFixe(
            DateTimeOffset instant)
        {
            _instant = instant;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _instant;
        }
    }
}