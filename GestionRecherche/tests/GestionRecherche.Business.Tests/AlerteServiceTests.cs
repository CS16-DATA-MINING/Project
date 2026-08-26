using GestionRecherche.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using Xunit;

namespace GestionRecherche.Business.Tests;

public sealed class AlerteServiceTests
{
    [Fact]

public async Task ExecuterPassageAsync_DemandeEncoreDansLeDelai_NeCreePasAnomalie()
{
    await using AppDbContext dbContext =
        CreerContexte();

    AjouterParametresD1EtD2(dbContext);

    /*
     * D1 = 30 jours.
     *
     * Autorisation : 01/08/2026
     * Date limite : 31/08/2026
     *
     * Aujourd'hui : 22/08/2026
     *
     * La demande n'est donc PAS encore en retard.
     */
    dbContext.DemandesPublication.Add(
        new DemandePublication
        {
            Titre = "Demande encore dans le délai",

            Abstract =
                "Résumé de test.",

            AuteurId = "auteur-1",

            StatutValidationInterne =
                StatutValidationInterne.Autorisee,

            DateAutorisationConseilUtc =
                new DateTime(
                    2026,
                    8,
                    1,
                    10,
                    0,
                    0,
                    DateTimeKind.Utc),

            DateSoumissionReelleUtc = null,

            NumeroCycleValidation = 1
        });

    await dbContext.SaveChangesAsync();

    var instant =
        new DateTimeOffset(
            2026,
            8,
            22,
            10,
            0,
            0,
            TimeSpan.Zero);

    var service =
        new AlerteService(
            dbContext,
            new HorlogeFixe(instant),
            NullLogger<AlerteService>.Instance);

    ResultatPassageAlerte resultat =
        await service.ExecuterPassageAsync();

    Assert.True(resultat.EstReussi);

    Assert.Equal(
        1,
        resultat.DemandesExaminees);

    Assert.Equal(
        0,
        resultat.AnomaliesCreees);

    Assert.Equal(
        0,
        await dbContext.Anomalies.CountAsync());
}

   [Fact]
public async Task ExecuterPassageAsync_D1Depasse_CreeUneAnomalie()
{
    await using AppDbContext dbContext =
        CreerContexte();

    AjouterParametresD1EtD2(dbContext);

    /*
     * D1 = 30 jours.
     *
     * Autorisation : 01/07/2026
     * Date limite : 31/07/2026
     *
     * Aujourd'hui : 22/08/2026
     *
     * La demande est donc en retard.
     */
    dbContext.DemandesPublication.Add(
        new DemandePublication
        {
            Titre = "Demande en retard",

            Abstract =
                "Résumé scientifique de test.",

            AuteurId = "auteur-1",

            StatutValidationInterne =
                StatutValidationInterne.Autorisee,

            DateAutorisationConseilUtc =
                new DateTime(
                    2026,
                    7,
                    1,
                    10,
                    0,
                    0,
                    DateTimeKind.Utc),

            DateSoumissionReelleUtc = null,

            NumeroCycleValidation = 1
        });

    await dbContext.SaveChangesAsync();

    var instant =
        new DateTimeOffset(
            2026,
            8,
            22,
            10,
            0,
            0,
            TimeSpan.Zero);

    var service =
        new AlerteService(
            dbContext,
            new HorlogeFixe(instant),
            NullLogger<AlerteService>.Instance);

    ResultatPassageAlerte resultat =
        await service.ExecuterPassageAsync();

    Assert.True(resultat.EstReussi);

    Assert.Equal(
        1,
        resultat.DemandesExaminees);

    /*
     * Une anomalie doit avoir été créée.
     */
    Assert.Equal(
        1,
        resultat.AnomaliesCreees);

    /*
     * Vérifier réellement la base de données.
     */
    Anomalie anomalie =
        await dbContext.Anomalies.SingleAsync();

    Assert.Equal(
        TypeAnomalie.NonSoumission,
        anomalie.Type);

    Assert.Equal(
        StatutAnomalie.Ouverte,
        anomalie.Statut);

    Assert.Equal(
        1,
        anomalie.DemandePublicationId);

    /*
     * Du 31/07 au 22/08 :
     * 22 jours de retard.
     */
    Assert.Equal(
        22,
        anomalie.DureeJours);

    Assert.Contains(
        "D1 de 30 jour(s)",
        anomalie.Message);
}

    [Fact]
public async Task ExecuterPassageAsync_DemandeDejaSoumise_NeCreePasAnomalie()
{
    await using AppDbContext dbContext =
        CreerContexte();

    AjouterParametresD1EtD2(dbContext);

    /*
     * La demande est autorisée depuis longtemps.
     *
     * Mais l'auteur a déjà déclaré
     * une soumission réelle.
     *
     * Elle ne doit donc PAS produire D1.
     */
    dbContext.DemandesPublication.Add(
        new DemandePublication
        {
            Titre = "Article déjà soumis",

            Abstract =
                "Résumé de test.",

            AuteurId = "auteur-1",

            StatutValidationInterne =
                StatutValidationInterne.Autorisee,

            DateAutorisationConseilUtc =
                new DateTime(
                    2026,
                    7,
                    1,
                    10,
                    0,
                    0,
                    DateTimeKind.Utc),

            DateSoumissionReelleUtc =
                new DateTime(
                    2026,
                    7,
                    20,
                    10,
                    0,
                    0,
                    DateTimeKind.Utc),

            NumeroSuivi = "TRACK-001",

            NumeroCycleValidation = 1
        });

    await dbContext.SaveChangesAsync();

    var instant =
        new DateTimeOffset(
            2026,
            8,
            22,
            10,
            0,
            0,
            TimeSpan.Zero);

    var service =
        new AlerteService(
            dbContext,
            new HorlogeFixe(instant),
            NullLogger<AlerteService>.Instance);

    ResultatPassageAlerte resultat =
        await service.ExecuterPassageAsync();

    Assert.True(resultat.EstReussi);

    Assert.Equal(
        0,
        resultat.AnomaliesCreees);

    Assert.Equal(
        0,
        await dbContext.Anomalies.CountAsync());
}



    [Fact]
    public async Task ExecuterPassageAsync_D1Manquant_RetourneEchec()
    {
        await using AppDbContext dbContext =
            CreerContexte();

        /*
         * Ajouter seulement D2.
         * D1 est volontairement absent.
         */
        dbContext.ParametresSysteme.Add(
            new ParametreSysteme
            {
                Cle = "D2",
                Valeur = 6,
                Unite = UniteParametre.Mois,
                Description =
                    "Délai maximal de stagnation."
            });

        await dbContext.SaveChangesAsync();

        var instant = new DateTimeOffset(
            2026, 8, 22,
            10, 0, 0,
            TimeSpan.Zero);

        var service = new AlerteService(
            dbContext,
            new HorlogeFixe(instant),
            NullLogger<AlerteService>.Instance);

        ResultatPassageAlerte resultat =
            await service.ExecuterPassageAsync();

        // Le passage doit échouer proprement.
        Assert.False(resultat.EstReussi);

        Assert.Equal(
            "PARAMETRE_MANQUANT",
            resultat.Code);

        Assert.Equal(
            0,
            resultat.DemandesExaminees);

        Assert.Equal(
            0,
            resultat.AnomaliesCreees);

        Assert.Equal(
            0,
            await dbContext.Anomalies.CountAsync());
    }

  
     [Fact]
public async Task ExecuterPassageAsync_DeuxPassages_NeCreeQuUneSeuleAnomalie()
{
    await using AppDbContext dbContext =
        CreerContexte();

    AjouterParametresD1EtD2(dbContext);

    /*
     * Cette demande est en retard.
     */
    dbContext.DemandesPublication.Add(
        new DemandePublication
        {
            Titre = "Demande en retard",

            Abstract =
                "Article de test.",

            AuteurId = "auteur-1",

            StatutValidationInterne =
                StatutValidationInterne.Autorisee,

            DateAutorisationConseilUtc =
                new DateTime(
                    2026,
                    7,
                    1,
                    10,
                    0,
                    0,
                    DateTimeKind.Utc),

            DateSoumissionReelleUtc = null,

            NumeroCycleValidation = 1
        });

    await dbContext.SaveChangesAsync();

    var instant =
        new DateTimeOffset(
            2026,
            8,
            22,
            10,
            0,
            0,
            TimeSpan.Zero);

    var service =
        new AlerteService(
            dbContext,
            new HorlogeFixe(instant),
            NullLogger<AlerteService>.Instance);

    /*
     * PREMIER passage.
     *
     * L'anomalie n'existe pas encore.
     * Elle doit donc être créée.
     */
    ResultatPassageAlerte premierPassage =
        await service.ExecuterPassageAsync();

    /*
     * DEUXIÈME passage.
     *
     * Cette fois l'anomalie existe déjà.
     * Le robot ne doit donc PAS en créer une deuxième.
     */
    ResultatPassageAlerte deuxiemePassage =
        await service.ExecuterPassageAsync();

    Assert.True(
        premierPassage.EstReussi);

    Assert.True(
        deuxiemePassage.EstReussi);

    /*
     * Premier passage :
     * une anomalie créée.
     */
    Assert.Equal(
        1,
        premierPassage.AnomaliesCreees);

    /*
     * Deuxième passage :
     * aucune nouvelle anomalie.
     */
    Assert.Equal(
        0,
        deuxiemePassage.AnomaliesCreees);

    /*
     * Au final :
     * UNE SEULE anomalie dans la base.
     */
    Assert.Equal(
        1,
        await dbContext.Anomalies.CountAsync());
}

    private static AppDbContext CreerContexte()
    {
        /*
         * Chaque test reçoit une base différente.
         * Le Guid empêche les tests de partager les données.
         */
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(
                    $"alertes-{Guid.NewGuid()}")
                .Options;

        return new AppDbContext(options);
    }

    private static void AjouterParametresD1EtD2(
        AppDbContext dbContext)
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
                Valeur = 6,
                Unite = UniteParametre.Mois,
                Description =
                    "Délai avant stagnation."
            });
    }

    /*
     * Horloge fixe utilisée pendant les tests.
     *
     * Le test décide de l'heure au lieu d'utiliser
     * l'heure réelle de l'ordinateur.
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