using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using GestionRecherche.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GestionRecherche.Business;

public class AlerteService : IAlerteService
{
    /*
     * Les clés doivent être exactement les mêmes
     * que celles enregistrées dans ParametresSysteme.
     */
    private const string CleD1 = "D1";
    private const string CleD2 = "D2";

    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AlerteService> _logger;

    public AlerteService(
        AppDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<AlerteService> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<ResultatPassageAlerte>
        ExecuterPassageAsync(
            CancellationToken cancellationToken = default)
    {
        /*
         * Enregistrer l’heure de début du robot.
         *
         * TimeProvider est utilisé à la place de
         * DateTime.UtcNow pour faciliter les tests.
         */
        DateTime debutUtc =
            _timeProvider.GetUtcNow().UtcDateTime;

        int? valeurD1 = null;
        int? valeurD2 = null;

        int demandesExaminees = 0;

        _logger.LogInformation(
            "Début du passage du robot à {DateDebut:o}.",
            debutUtc);

        try
        {
            /*
             * ========================================
             * ÉTAPE 1 : LIRE D1 ET D2 DEPUIS LA BASE
             * ========================================
             *
             * Les paramètres sont relus à chaque passage.
             * Une modification faite par l’administrateur
             * sera donc prise en compte sans redémarrage.
             */
            List<ParametreSysteme> parametres =
                await _dbContext.ParametresSysteme
                    .AsNoTracking()
                    .Where(parametre =>
                        parametre.Cle == CleD1 ||
                        parametre.Cle == CleD2)
                    .ToListAsync(cancellationToken);

            ParametreSysteme? parametreD1 =
                parametres.SingleOrDefault(
                    parametre =>
                        parametre.Cle == CleD1);

            ParametreSysteme? parametreD2 =
                parametres.SingleOrDefault(
                    parametre =>
                        parametre.Cle == CleD2);

            /*
             * D1 et D2 doivent obligatoirement exister.
             */
            if (parametreD1 is null ||
                parametreD2 is null)
            {
                _logger.LogWarning(
                    "Passage arrêté : D1 ou D2 est absent.");

                return CreerResultatEchec(
                    code: "PARAMETRE_MANQUANT",
                    message:
                        "Le paramètre D1 ou D2 est absent.",
                    debutUtc: debutUtc,
                    valeurD1: null,
                    valeurD2: null,
                    demandesExaminees: 0);
            }

            /*
             * Les valeurs doivent être positives.
             */
            if (parametreD1.Valeur <= 0 ||
                parametreD2.Valeur <= 0)
            {
                _logger.LogWarning(
                    "Valeur invalide. D1={D1}, D2={D2}.",
                    parametreD1.Valeur,
                    parametreD2.Valeur);

                return CreerResultatEchec(
                    code: "VALEUR_INVALIDE",
                    message:
                        "Les valeurs D1 et D2 doivent être " +
                        "strictement positives.",
                    debutUtc: debutUtc,
                    valeurD1: null,
                    valeurD2: null,
                    demandesExaminees: 0);
            }

            /*
             * D1 et D2 peuvent être exprimés :
             *
             * - en jours ;
             * - ou en mois.
             *
             * L’unité Nombre n’est pas une durée.
             */
            if (!EstUniteDeDuree(parametreD1.Unite) ||
                !EstUniteDeDuree(parametreD2.Unite))
            {
                _logger.LogWarning(
                    "Unité invalide. D1={UniteD1}, D2={UniteD2}.",
                    parametreD1.Unite,
                    parametreD2.Unite);

                return CreerResultatEchec(
                    code: "UNITE_INVALIDE",
                    message:
                        "D1 et D2 doivent être exprimés " +
                        "en jours ou en mois.",
                    debutUtc: debutUtc,
                    valeurD1: null,
                    valeurD2: null,
                    demandesExaminees: 0);
            }

            valeurD1 = parametreD1.Valeur;
            valeurD2 = parametreD2.Valeur;

            DateTime maintenantUtc =
                _timeProvider.GetUtcNow().UtcDateTime;

            _logger.LogInformation(
                "Paramètres utilisés : " +
                "D1={ValeurD1} {UniteD1}, " +
                "D2={ValeurD2} {UniteD2}.",
                parametreD1.Valeur,
                parametreD1.Unite,
                parametreD2.Valeur,
                parametreD2.Unite);

            /*
             * ========================================
             * ÉTAPE 2 : DÉTECTER D1
             * ========================================
             */
            ResultatDetection resultatD1 =
                await DetecterNonSoumissionsAsync(
                    maintenantUtc,
                    parametreD1.Valeur,
                    parametreD1.Unite,
                    cancellationToken);

            /*
             * ========================================
             * ÉTAPE 3 : DÉTECTER D2
             * ========================================
             */
            ResultatDetection resultatD2 =
                await DetecterStagnationsAsync(
                    maintenantUtc,
                    parametreD2.Valeur,
                    parametreD2.Unite,
                    cancellationToken);

            demandesExaminees =
                resultatD1.DemandesExaminees +
                resultatD2.DemandesExaminees;

            int anomaliesCreees =
                resultatD1.AnomaliesCreees +
                resultatD2.AnomaliesCreees;

            /*
             * Une seule sauvegarde pour toutes
             * les anomalies D1 et D2.
             *
             * Cela évite d’appeler SaveChangesAsync
             * dans chaque boucle.
             */
            if (anomaliesCreees > 0)
            {
                await _dbContext.SaveChangesAsync(
                    cancellationToken);
            }

            DateTime finUtc =
                _timeProvider.GetUtcNow().UtcDateTime;

            _logger.LogInformation(
                "Fin du robot. " +
                "Demandes examinées={Demandes}. " +
                "D1 créées={NombreD1}. " +
                "D2 créées={NombreD2}.",
                demandesExaminees,
                resultatD1.AnomaliesCreees,
                resultatD2.AnomaliesCreees);

            /*
             * Les propriétés DelaiD1Jours et DelaiD2Mois
             * existent déjà dans ton modèle de résultat.
             *
             * On les conserve pour ne pas casser
             * le contrôleur et la vue existants.
             *
             * Les calculs, eux, respectent bien
             * l’unité réelle de la base.
             */
            return new ResultatPassageAlerte(
                EstReussi: true,
                Code: "SUCCES",
                Message:
                    $"Passage terminé : " +
                    $"{resultatD1.AnomaliesCreees} anomalie(s) D1 " +
                    $"et {resultatD2.AnomaliesCreees} anomalie(s) D2 " +
                    $"créée(s).",
                DateDebutUtc: debutUtc,
                DateFinUtc: finUtc,
                DelaiD1Jours: valeurD1,
                DelaiD2Mois: valeurD2,
                DemandesExaminees: demandesExaminees,
                AnomaliesCreees: anomaliesCreees);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            /*
             * Cela arrive normalement lorsque
             * l’application est arrêtée.
             */
            _logger.LogInformation(
                "Le passage du robot a été annulé.");

            throw;
        }
        catch (Exception exception)
        {
            DateTime finUtc =
                _timeProvider.GetUtcNow().UtcDateTime;

            _logger.LogError(
                exception,
                "Erreur technique pendant le passage du robot.");

            return new ResultatPassageAlerte(
                EstReussi: false,
                Code: "ERREUR_TECHNIQUE",
                Message:
                    "Une erreur technique a empêché " +
                    "le passage du robot.",
                DateDebutUtc: debutUtc,
                DateFinUtc: finUtc,
                DelaiD1Jours: valeurD1,
                DelaiD2Mois: valeurD2,
                DemandesExaminees: demandesExaminees,
                AnomaliesCreees: 0);
        }
    }

    /*
     * ==========================================
     * DÉTECTION D1 : NON-SOUMISSION
     * ==========================================
     *
     * D1 concerne les demandes :
     *
     * - autorisées par le Conseil ;
     * - avec une date d’autorisation ;
     * - sans soumission réelle.
     */
    private async Task<ResultatDetection>
        DetecterNonSoumissionsAsync(
            DateTime maintenantUtc,
            int valeurD1,
            UniteParametre uniteD1,
            CancellationToken cancellationToken)
    {
        List<DemandePublication> demandesASurveiller =
            await _dbContext.DemandesPublication
                .AsNoTracking()
                .Where(demande =>
                    demande.StatutValidationInterne ==
                        StatutValidationInterne.Autorisee

                    &&

                    demande.DateAutorisationConseilUtc != null

                    &&

                    demande.DateSoumissionReelleUtc == null)
                .ToListAsync(cancellationToken);

        /*
         * Charger les demandes qui ont déjà
         * une anomalie D1 ouverte.
         */
        HashSet<int> demandesAvecD1Ouverte =
            (await _dbContext.Anomalies
                .AsNoTracking()
                .Where(anomalie =>
                    anomalie.Type ==
                        TypeAnomalie.NonSoumission

                    &&

                    anomalie.Statut ==
                        StatutAnomalie.Ouverte)
                .Select(anomalie =>
                    anomalie.DemandePublicationId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        int anomaliesCreees = 0;

        foreach (DemandePublication demande
            in demandesASurveiller)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            DateTime dateAutorisationUtc =
                demande.DateAutorisationConseilUtc!.Value;

            /*
             * Cette méthode utilise AddDays ou AddMonths
             * selon l’unité enregistrée dans la base.
             */
            DateTime dateLimiteD1Utc =
                AjouterDelai(
                    dateAutorisationUtc,
                    valeurD1,
                    uniteD1);

            /*
             * À la limite exacte, le délai
             * n’est pas encore dépassé.
             */
            if (maintenantUtc <= dateLimiteD1Utc)
            {
                continue;
            }

            /*
             * Empêcher une deuxième D1 ouverte
             * pour la même demande.
             */
            if (demandesAvecD1Ouverte.Contains(
                demande.Id))
            {
                continue;
            }

            int joursDeRetard =
                Math.Max(
                    1,
                    (int)Math.Ceiling(
                        (maintenantUtc -
                         dateLimiteD1Utc)
                        .TotalDays));

            var anomalie =
                new Anomalie
                {
                    DemandePublicationId =
                        demande.Id,

                    Type =
                        TypeAnomalie.NonSoumission,

                    Statut =
                        StatutAnomalie.Ouverte,

                    DateDetectionUtc =
                        maintenantUtc,

                    /*
                     * DureeJours contient toujours
                     * un nombre de jours observés.
                     */
                    DureeJours =
                        joursDeRetard,

                    Message =
                        $"La demande #{demande.Id} a été " +
                        $"autorisée par le Conseil le " +
                        $"{dateAutorisationUtc:dd/MM/yyyy HH:mm} UTC, " +

                        $"mais aucune soumission réelle " +
                        $"n’a été déclarée après le délai D1 de " +

                        $"{valeurD1} " +
                        $"{ObtenirLibelleUnite(uniteD1)}. " +

                        $"Retard : {joursDeRetard} jour(s)."
                };

            _dbContext.Anomalies.Add(anomalie);

            demandesAvecD1Ouverte.Add(
                demande.Id);

            anomaliesCreees++;

            _logger.LogWarning(
                "Anomalie D1 créée. " +
                "Demande={DemandeId}, Retard={Retard}.",
                demande.Id,
                joursDeRetard);
        }

        return new ResultatDetection(
            DemandesExaminees:
                demandesASurveiller.Count,

            AnomaliesCreees:
                anomaliesCreees);
    }

    /*
     * ==========================================
     * JOUR 13 : DÉTECTION D2 — STAGNATION
     * ==========================================
     *
     * D2 concerne les demandes :
     *
     * - autorisées par le Conseil ;
     * - réellement soumises ;
     * - possédant un statut éditeur ;
     * - possédant une dernière mise à jour ;
     * - différentes de Published et Rejected.
     */
    private async Task<ResultatDetection>
        DetecterStagnationsAsync(
            DateTime maintenantUtc,
            int valeurD2,
            UniteParametre uniteD2,
            CancellationToken cancellationToken)
    {
        /*
         * ÉTAPE 1
         * -------
         * Récupérer les articles qui peuvent
         * être surveillés par D2.
         */
        List<DemandePublication> demandesASurveiller =
            await _dbContext.DemandesPublication
                .AsNoTracking()
                .Where(demande =>
                    /*
                     * La demande doit être autorisée.
                     */
                    demande.StatutValidationInterne ==
                        StatutValidationInterne.Autorisee

                    &&

                    /*
                     * Une soumission réelle doit exister.
                     * Sinon, la demande appartient à D1.
                     */
                    demande.DateSoumissionReelleUtc != null

                    &&

                    /*
                     * Une date est nécessaire
                     * pour calculer D2.
                     */
                    demande.DateDerniereMiseAJourUtc != null

                    &&

                    /*
                     * Un statut éditeur doit exister.
                     */
                    demande.StatutEditeurCourant.HasValue

                    &&

                    /*
                     * Published termine le cycle.
                     */
                    demande.StatutEditeurCourant !=
                        StatutEditeur.Published

                    &&

                    /*
                     * Rejected termine aussi le cycle.
                     */
                    demande.StatutEditeurCourant !=
                        StatutEditeur.Rejected)
                .ToListAsync(cancellationToken);

        /*
         * ÉTAPE 2
         * -------
         * Charger toutes les demandes qui possèdent
         * déjà une anomalie D2 ouverte.
         *
         * HashSet permet une recherche rapide
         * par identifiant.
         */
        HashSet<int> demandesAvecD2Ouverte =
            (await _dbContext.Anomalies
                .AsNoTracking()
                .Where(anomalie =>
                    anomalie.Type ==
                        TypeAnomalie.Stagnation

                    &&

                    anomalie.Statut ==
                        StatutAnomalie.Ouverte)
                .Select(anomalie =>
                    anomalie.DemandePublicationId)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        int anomaliesCreees = 0;

        /*
         * ÉTAPE 3
         * -------
         * Examiner chaque demande.
         */
        foreach (DemandePublication demande
            in demandesASurveiller)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            DateTime derniereMiseAJourUtc =
                demande.DateDerniereMiseAJourUtc!.Value;

            /*
             * Respecter l’unité configurée.
             *
             * Exemple 1 :
             * valeurD2 = 6
             * uniteD2 = Mois
             * => AddMonths(6)
             *
             * Exemple 2 :
             * valeurD2 = 180
             * uniteD2 = Jours
             * => AddDays(180)
             */
            DateTime dateLimiteD2Utc =
                AjouterDelai(
                    derniereMiseAJourUtc,
                    valeurD2,
                    uniteD2);

            /*
             * Si maintenant est avant ou exactement
             * à la limite, D2 n’est pas dépassé.
             */
            if (maintenantUtc <= dateLimiteD2Utc)
            {
                continue;
            }

            /*
             * Si une D2 ouverte existe déjà,
             * ne pas créer de doublon.
             */
            if (demandesAvecD2Ouverte.Contains(
                demande.Id))
            {
                continue;
            }

            StatutEditeur statutActuel =
                demande.StatutEditeurCourant!.Value;

            /*
             * Nombre total de jours écoulés
             * depuis la dernière mise à jour.
             */
            int joursSansMiseAJour =
                Math.Max(
                    1,
                    (int)Math.Floor(
                        (maintenantUtc -
                         derniereMiseAJourUtc)
                        .TotalDays));

            /*
             * ÉTAPE 4
             * -------
             * Créer l’anomalie Stagnation.
             */
            var anomalie =
                new Anomalie
                {
                    DemandePublicationId =
                        demande.Id,

                    Type =
                        TypeAnomalie.Stagnation,

                    Statut =
                        StatutAnomalie.Ouverte,

                    DateDetectionUtc =
                        maintenantUtc,

                    DureeJours =
                        joursSansMiseAJour,

                    Message =
                        $"La demande #{demande.Id} est " +
                        $"sans évolution depuis " +
                        $"{joursSansMiseAJour} jour(s). " +

                        $"Dernière mise à jour : " +
                        $"{derniereMiseAJourUtc:dd/MM/yyyy HH:mm} UTC. " +

                        $"Le délai D2 de " +
                        $"{valeurD2} " +
                        $"{ObtenirLibelleUnite(uniteD2)} " +
                        $"est dépassé. " +

                        $"Statut éditeur actuel : " +
                        $"{statutActuel}."
                };

            /*
             * Préparer l’ajout.
             *
             * SaveChangesAsync sera appelé
             * dans ExecuterPassageAsync.
             */
            _dbContext.Anomalies.Add(anomalie);

            /*
             * Protéger également le passage courant.
             */
            demandesAvecD2Ouverte.Add(
                demande.Id);

            anomaliesCreees++;

            _logger.LogWarning(
                "Anomalie D2 créée. " +
                "Demande={DemandeId}, " +
                "Statut={Statut}, " +
                "JoursSansMiseAJour={Jours}.",
                demande.Id,
                statutActuel,
                joursSansMiseAJour);
        }

        return new ResultatDetection(
            DemandesExaminees:
                demandesASurveiller.Count,

            AnomaliesCreees:
                anomaliesCreees);
    }

    /*
     * Vérifier que l’unité représente
     * réellement une durée.
     */
    private static bool EstUniteDeDuree(
        UniteParametre unite)
    {
        return unite == UniteParametre.Jours ||
               unite == UniteParametre.Mois;
    }

    /*
     * Ajouter un délai en respectant son unité.
     *
     * Il ne faut surtout pas convertir :
     * 6 mois = 180 jours.
     *
     * Les mois n’ont pas tous la même durée.
     */
    private static DateTime AjouterDelai(
        DateTime dateDebutUtc,
        int valeur,
        UniteParametre unite)
    {
        if (valeur <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(valeur),
                "La valeur du délai doit être positive.");
        }

        return unite switch
        {
            UniteParametre.Jours =>
                dateDebutUtc.AddDays(valeur),

            UniteParametre.Mois =>
                dateDebutUtc.AddMonths(valeur),

            _ => throw new InvalidOperationException(
                $"L’unité {unite} ne peut pas être " +
                "utilisée pour calculer une durée.")
        };
    }

    /*
     * Produire le texte correct dans les messages.
     */
    private static string ObtenirLibelleUnite(
        UniteParametre unite)
    {
        return unite switch
        {
            UniteParametre.Jours =>
                "jour(s)",

            UniteParametre.Mois =>
                "mois",

            _ => throw new InvalidOperationException(
                $"L’unité {unite} n’est pas " +
                "une unité de durée.")
        };
    }

    /*
     * Construire un résultat d’échec uniforme.
     */
    private ResultatPassageAlerte CreerResultatEchec(
        string code,
        string message,
        DateTime debutUtc,
        int? valeurD1,
        int? valeurD2,
        int demandesExaminees)
    {
        return new ResultatPassageAlerte(
            EstReussi: false,
            Code: code,
            Message: message,
            DateDebutUtc: debutUtc,
            DateFinUtc:
                _timeProvider.GetUtcNow().UtcDateTime,
            DelaiD1Jours: valeurD1,
            DelaiD2Mois: valeurD2,
            DemandesExaminees: demandesExaminees,
            AnomaliesCreees: 0);
    }

    /*
     * Petit objet interne permettant à chaque
     * détecteur de retourner deux informations.
     */
    private sealed record ResultatDetection(
        int DemandesExaminees,
        int AnomaliesCreees);
}