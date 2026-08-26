using System;
using System.Threading.Tasks;

using GestionRecherche.Business;
using GestionRecherche.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace GestionRecherche.Business.Tests;

public sealed class AlerteServiceD2Tests
{
    /*
     * Le robot est exécuté à une date fixe
     * pour que les tests donnent toujours
     * exactement le même résultat.
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
     * Ce test vérifie cinq situations :
     *
     * 1. article récent : aucune anomalie ;
     * 2. limite exacte D2 : aucune anomalie ;
     * 3. D2 dépassé : création d'une anomalie ;
     * 4. article publié : aucune anomalie ;
     * 5. article rejeté : aucune anomalie.
     */
    [Theory]
    [InlineData(
        StatutEditeur.UnderReview,
        2026,
        7,
        1,
        false)]
    [InlineData(
        StatutEditeur.MinorRevision,
        2026,
        2,
        24,
        false)]
    [InlineData(
        StatutEditeur.MajorRevision,
        2026,
        1,
        1,
        true)]
    [InlineData(
        StatutEditeur.Published,
        2026,
        1,
        1,
        false)]
    [InlineData(
        StatutEditeur.Rejected,
        2026,
        1,
        1,
        false)]
    public async Task ExecuterPassageAsync_CreeD2SeulementPourArticleStagnant(
        StatutEditeur statut,
        int anneeDerniereMiseAJour,
        int moisDerniereMiseAJour,
        int jourDerniereMiseAJour,
        bool anomalieAttendue)
    {
        /*
         * ARRANGE
         * Préparer une base de données vide.
         */
        await using AppDbContext dbContext =
            CreerContexte();

        /*
         * D1 = 30 jours
         * D2 = 6 mois
         */
        AjouterParametres(
            dbContext,
            valeurD2: 6,
            uniteD2: UniteParametre.Mois);

        /*
         * Ajouter une demande soumise avec
         * le statut et la date reçus par le test.
         */
        dbContext.DemandesPublication.Add(
            CreerDemandeSoumise(
                statut,
                Utc(
                    anneeDerniereMiseAJour,
                    moisDerniereMiseAJour,
                    jourDerniereMiseAJour,
                    10)));

        await dbContext.SaveChangesAsync();

        /*
         * Créer le service avec une horloge fixe.
         */
        var service =
            new AlerteService(
                dbContext,
                new HorlogeFixe(Maintenant),
                NullLogger<AlerteService>.Instance);

        /*
         * ACT
         * Exécuter le robot.
         */
        ResultatPassageAlerte resultat =
            await service.ExecuterPassageAsync();

        /*
         * ASSERT
         * Vérifier que le passage s'est terminé.
         */
        Assert.True(resultat.EstReussi);

        int nombreAnomaliesAttendu =
            anomalieAttendue ? 1 : 0;

        int nombreAnomaliesObtenu =
            await dbContext.Anomalies.CountAsync();

        Assert.Equal(
            nombreAnomaliesAttendu,
            nombreAnomaliesObtenu);

        /*
         * Si une anomalie est attendue,
         * vérifier son contenu.
         */
        if (anomalieAttendue)
        {
            Anomalie anomalie =
                await dbContext.Anomalies
                    .SingleAsync();

            Assert.Equal(
                TypeAnomalie.Stagnation,
                anomalie.Type);

            Assert.Equal(
                StatutAnomalie.Ouverte,
                anomalie.Statut);

            Assert.True(
                anomalie.DureeJours > 0);

            Assert.Contains(
                statut.ToString(),
                anomalie.Message);

            Assert.Contains(
                "6 mois",
                anomalie.Message);
        }
    }

    /*
     * Vérifier spécialement D2 exprimé en jours.
     */
    [Fact]
    public async Task ExecuterPassageAsync_D2EnJours_CreeAnomalieApres180Jours()
    {
        /*
         * ARRANGE
         */
        await using AppDbContext dbContext =
            CreerContexte();

        /*
         * D2 est volontairement exprimé
         * en jours et non en mois.
         */
        AjouterParametres(
            dbContext,
            valeurD2: 180,
            uniteD2: UniteParametre.Jours);

        dbContext.DemandesPublication.Add(
            CreerDemandeSoumise(
                StatutEditeur.UnderReview,
                Utc(2026, 1, 1, 10)));

        await dbContext.SaveChangesAsync();

        var service =
            new AlerteService(
                dbContext,
                new HorlogeFixe(Maintenant),
                NullLogger<AlerteService>.Instance);

        /*
         * ACT
         */
        ResultatPassageAlerte resultat =
            await service.ExecuterPassageAsync();

        /*
         * ASSERT
         */
        Assert.True(resultat.EstReussi);

        Assert.Equal(
            1,
            resultat.AnomaliesCreees);

        Anomalie anomalie =
            await dbContext.Anomalies
                .SingleAsync();

        Assert.Equal(
            TypeAnomalie.Stagnation,
            anomalie.Type);

        Assert.Equal(
            StatutAnomalie.Ouverte,
            anomalie.Statut);

        Assert.Contains(
            "180 jour(s)",
            anomalie.Message);
    }

    /*
     * Vérifier que deux passages du robot
     * ne créent pas deux anomalies D2.
     */
    [Fact]
    public async Task ExecuterPassageAsync_DeuxPassages_NeCreeQuUneSeuleD2()
    {
        /*
         * ARRANGE
         */
        await using AppDbContext dbContext =
            CreerContexte();

        AjouterParametres(
            dbContext,
            valeurD2: 6,
            uniteD2: UniteParametre.Mois);

        dbContext.DemandesPublication.Add(
            CreerDemandeSoumise(
                StatutEditeur.Accepted,
                Utc(2026, 1, 1, 10)));

        await dbContext.SaveChangesAsync();

        var service =
            new AlerteService(
                dbContext,
                new HorlogeFixe(Maintenant),
                NullLogger<AlerteService>.Instance);

        /*
         * ACT
         */
        ResultatPassageAlerte premierPassage =
            await service.ExecuterPassageAsync();

        ResultatPassageAlerte deuxiemePassage =
            await service.ExecuterPassageAsync();

        /*
         * ASSERT
         */
        Assert.True(
            premierPassage.EstReussi);

        Assert.True(
            deuxiemePassage.EstReussi);

        Assert.Equal(
            1,
            premierPassage.AnomaliesCreees);

        Assert.Equal(
            0,
            deuxiemePassage.AnomaliesCreees);

        Assert.Equal(
            1,
            await dbContext.Anomalies.CountAsync());
    }

    /*
     * Créer une demande qui a déjà été
     * réellement soumise à un éditeur.
     */
    private static DemandePublication CreerDemandeSoumise(
        StatutEditeur statut,
        DateTime derniereMiseAJourUtc)
    {
        return new DemandePublication
        {
            Titre =
                "Article de test D2",

            Abstract =
                "Résumé scientifique utilisé pour le test D2.",

            AuteurId =
                "auteur-test-d2",

            StatutValidationInterne =
                StatutValidationInterne.Autorisee,

            NumeroCycleValidation =
                1,

            DateAutorisationConseilUtc =
                Utc(2025, 12, 1, 10),

            DateSoumissionReelleUtc =
                Utc(2025, 12, 15, 10),

            StatutEditeurCourant =
                statut,

            DateDerniereMiseAJourUtc =
                derniereMiseAJourUtc
        };
    }

    /*
     * Chaque test reçoit une base différente.
     *
     * Guid.NewGuid() empêche les tests
     * de partager les mêmes données.
     */
    private static AppDbContext CreerContexte()
    {
        DbContextOptions<AppDbContext> options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(
                    $"alertes-d2-{Guid.NewGuid()}")
                .Options;

        return new AppDbContext(options);
    }

    /*
     * Ajouter les paramètres nécessaires
     * au fonctionnement du robot.
     */
    private static void AjouterParametres(
        AppDbContext dbContext,
        int valeurD2,
        UniteParametre uniteD2)
    {
        dbContext.ParametresSysteme.AddRange(
            new ParametreSysteme
            {
                Cle = "D1",
                Valeur = 30,
                Unite = UniteParametre.Jours,
                Description =
                    "Délai avant non-soumission."
            },
            new ParametreSysteme
            {
                Cle = "D2",
                Valeur = valeurD2,
                Unite = uniteD2,
                Description =
                    "Délai avant stagnation."
            });
    }

    /*
     * Construire facilement une date UTC.
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
     * Horloge fixe utilisée uniquement
     * pendant les tests.
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