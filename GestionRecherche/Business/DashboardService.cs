using GestionRecherche.Data;

using Microsoft.EntityFrameworkCore;

namespace GestionRecherche.Business;

public sealed class DashboardService : IDashboardService
{
    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;

    public DashboardService(
        AppDbContext dbContext,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public async Task<DashboardDto> ObtenirTableauDeBordAsync(
        CancellationToken cancellationToken = default)
    {
        /*
         * 1. Compter les demandes par statut interne.
         *
         * Exemple :
         * EnAttenteLabo      = 4
         * EnAttenteUer       = 2
         * Autorisee          = 7
         * Rejetee            = 1
         */
        var valeursStatutsInternes =
            await _dbContext.DemandesPublication
                .AsNoTracking()
                .GroupBy(demande =>
                    demande.StatutValidationInterne)
                .Select(groupe => new
                {
                    Statut = groupe.Key,
                    Nombre = groupe.Count()
                })
                .ToListAsync(cancellationToken);

        /*
         * On ajoute aussi les statuts absents avec la valeur zéro.
         *
         * Sans cette étape, si aucune demande n'est en attente UER,
         * EnAttenteUer n'apparaîtrait pas dans le résultat.
         */
        List<CompteurStatutInterneDto> statutsInternes =
            Enum.GetValues<StatutValidationInterne>()
                .Select(statut =>
                    new CompteurStatutInterneDto(
                        statut,
                        valeursStatutsInternes
                            .FirstOrDefault(element =>
                                element.Statut == statut)
                            ?.Nombre ?? 0))
                .ToList();

        /*
         * 2. Compter les demandes par statut éditeur.
         *
         * StatutEditeurCourant est nullable.
         * Une demande qui n'a pas encore été soumise possède donc
         * généralement la valeur null.
         */
    
        var valeursStatutsEditeur =
            await _dbContext.DemandesPublication
                .AsNoTracking()
                .GroupBy(demande =>
                    demande.StatutEditeurCourant)
                .Select(groupe => new
                {
                    Statut = groupe.Key,
                    Nombre = groupe.Count()
                })
                .ToListAsync(cancellationToken);
                

        int nombreSansStatutEditeur =
            valeursStatutsEditeur
                .FirstOrDefault(element =>
                    !element.Statut.HasValue)
                ?.Nombre ?? 0;

        var statutsEditeur =
            new List<CompteurStatutEditeurDto>
            {
                new(
                    Statut: null,
                    Nombre: nombreSansStatutEditeur)
            };

        statutsEditeur.AddRange(
            Enum.GetValues<StatutEditeur>()
                .Select(statut =>
                    new CompteurStatutEditeurDto(
                        statut,
                        valeursStatutsEditeur
                            .FirstOrDefault(element =>
                                element.Statut == statut)
                            ?.Nombre ?? 0)));

        /*
         * 3. Regrouper les informations par laboratoire.
         *
         * Pour chaque laboratoire, les sous-requêtes comptent :
         * - les demandes des membres ;
         * - les anomalies ouvertes ;
         * - les anomalies traitées.
         */
        var valeursLaboratoires =
            await _dbContext.Laboratoires
                .AsNoTracking()
                .OrderBy(laboratoire => laboratoire.Nom)
                .Select(laboratoire => new
                {
                    laboratoire.Id,
                    laboratoire.Code,
                    laboratoire.Nom,
                    laboratoire.EstActif,

                    NombreDemandes =
                        _dbContext.DemandesPublication.Count(
                            demande =>
                                demande.Auteur.LaboratoireId ==
                                laboratoire.Id),

                    NombreAnomaliesOuvertes =
                        _dbContext.Anomalies.Count(
                            anomalie =>
                                anomalie.DemandePublication
                                    .Auteur.LaboratoireId ==
                                laboratoire.Id &&
                                anomalie.Statut ==
                                StatutAnomalie.Ouverte),

                    NombreAnomaliesTraitees =
                        _dbContext.Anomalies.Count(
                            anomalie =>
                                anomalie.DemandePublication
                                    .Auteur.LaboratoireId ==
                                laboratoire.Id &&
                                anomalie.Statut ==
                                StatutAnomalie.Traitee)
                })
                .ToListAsync(cancellationToken);

        List<StatistiqueLaboratoireDto> laboratoires =
            valeursLaboratoires
                .Select(laboratoire =>
                    new StatistiqueLaboratoireDto(
                        laboratoire.Id,
                        laboratoire.Code,
                        laboratoire.Nom,
                        laboratoire.EstActif,
                        laboratoire.NombreDemandes,
                        laboratoire.NombreAnomaliesOuvertes,
                        laboratoire.NombreAnomaliesTraitees))
                .ToList();

        /*
         * LaboratoireId est nullable dans ApplicationUser.
         * On évite donc de perdre les demandes appartenant à un
         * utilisateur qui n'a pas encore de laboratoire.
         */
        int nombreDemandesSansLaboratoire =
            await _dbContext.DemandesPublication
                .AsNoTracking()
                .CountAsync(
                    demande =>
                        demande.Auteur.LaboratoireId == null,
                    cancellationToken);

        if (nombreDemandesSansLaboratoire > 0)
        {
            int anomaliesOuvertesSansLaboratoire =
                await _dbContext.Anomalies
                    .AsNoTracking()
                    .CountAsync(
                        anomalie =>
                            anomalie.DemandePublication
                                .Auteur.LaboratoireId == null &&
                            anomalie.Statut ==
                            StatutAnomalie.Ouverte,
                        cancellationToken);

            int anomaliesTraiteesSansLaboratoire =
                await _dbContext.Anomalies
                    .AsNoTracking()
                    .CountAsync(
                        anomalie =>
                            anomalie.DemandePublication
                                .Auteur.LaboratoireId == null &&
                            anomalie.Statut ==
                            StatutAnomalie.Traitee,
                        cancellationToken);

            laboratoires.Add(
                new StatistiqueLaboratoireDto(
                    Id: null,
                    Code: "-",
                    Nom: "Sans laboratoire",
                    EstActif: true,
                    NombreDemandes:
                        nombreDemandesSansLaboratoire,
                    NombreAnomaliesOuvertes:
                        anomaliesOuvertesSansLaboratoire,
                    NombreAnomaliesTraitees:
                        anomaliesTraiteesSansLaboratoire));
        }

        /*
         * 4. Construire les quatre indicateurs principaux.
         */
        int nombreTotalDemandes =
            statutsInternes.Sum(element => element.Nombre);

        int nombreDemandesAutorisees =
            statutsInternes
                .Single(element =>
                    element.Statut ==
                    StatutValidationInterne.Autorisee)
                .Nombre;

        int nombreArticlesPublies =
            statutsEditeur
                .Single(element =>
                    element.Statut ==
                    StatutEditeur.Published)
                .Nombre;

        int nombreAnomaliesOuvertes =
            await CompterAnomaliesOuvertesAsync(
                cancellationToken);

        return new DashboardDto(
            DateGenerationUtc:
                _timeProvider.GetUtcNow().UtcDateTime,
            NombreTotalDemandes:
                nombreTotalDemandes,
            NombreDemandesAutorisees:
                nombreDemandesAutorisees,
            NombreArticlesPublies:
                nombreArticlesPublies,
            NombreAnomaliesOuvertes:
                nombreAnomaliesOuvertes,
            StatutsInternes:
                statutsInternes,
            StatutsEditeur:
                statutsEditeur,
            Laboratoires:
                laboratoires);
    }

    public Task<int> CompterAnomaliesOuvertesAsync(
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Anomalies
            .AsNoTracking()
            .CountAsync(
                anomalie =>
                    anomalie.Statut ==
                    StatutAnomalie.Ouverte,
                cancellationToken);
    }
}