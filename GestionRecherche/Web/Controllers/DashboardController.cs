using GestionRecherche.Business;
using GestionRecherche.Data;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionRecherche.Web;

[Authorize(Roles = "ChefBureau")]
public sealed class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(
        IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        DashboardDto donnees =
            await _dashboardService
                .ObtenirTableauDeBordAsync(
                    cancellationToken);

        var model = new DashboardViewModel
        {
            DateGenerationUtc =
                donnees.DateGenerationUtc,

            NombreTotalDemandes =
                donnees.NombreTotalDemandes,

            NombreDemandesAutorisees =
                donnees.NombreDemandesAutorisees,

            NombreArticlesPublies =
                donnees.NombreArticlesPublies,

            NombreAnomaliesOuvertes =
                donnees.NombreAnomaliesOuvertes,

            StatutsInternes =
                donnees.StatutsInternes
                    .Select(element =>
                        new CompteurDashboardViewModel
                        {
                            Libelle =
                                ObtenirLibelleStatutInterne(
                                    element.Statut),

                            Nombre = element.Nombre,

                            BadgeCss =
                                ObtenirCouleurStatutInterne(
                                    element.Statut)
                        })
                    .ToList(),

            StatutsEditeur =
                donnees.StatutsEditeur
                    .Select(element =>
                        new CompteurDashboardViewModel
                        {
                            Libelle =
                                ObtenirLibelleStatutEditeur(
                                    element.Statut),

                            Nombre = element.Nombre,

                            BadgeCss =
                                ObtenirCouleurStatutEditeur(
                                    element.Statut)
                        })
                    .ToList(),

            Laboratoires =
                donnees.Laboratoires
                    .Select(element =>
                        new StatistiqueLaboratoireViewModel
                        {
                            Id = element.Id,
                            Code = element.Code,
                            Nom = element.Nom,

                            EtatLibelle =
                                element.Id is null
                                    ? "Non affecté"
                                    : element.EstActif
                                        ? "Actif"
                                        : "Archivé",

                            EtatBadgeCss =
                                element.Id is null
                                    ? "bg-warning text-dark"
                                    : element.EstActif
                                        ? "bg-success"
                                        : "bg-secondary",

                            NombreDemandes =
                                element.NombreDemandes,

                            NombreAnomaliesOuvertes =
                                element.NombreAnomaliesOuvertes,

                            NombreAnomaliesTraitees =
                                element.NombreAnomaliesTraitees
                        })
                    .ToList()
        };

        return View(model);
    }

    private static string ObtenirLibelleStatutInterne(
        StatutValidationInterne statut)
    {
        return statut switch
        {
            StatutValidationInterne.EnAttenteLabo =>
                "En attente du Chef de laboratoire",

            StatutValidationInterne.EnAttenteUer =>
                "En attente du Chef UER",

            StatutValidationInterne.EnAttenteBureau =>
                "En attente du Chef de Bureau",

            StatutValidationInterne.EnAttenteConseil =>
                "En attente du Conseil",

            StatutValidationInterne.Autorisee =>
                "Autorisée",

            StatutValidationInterne.Rejetee =>
                "Rejetée",

            _ => statut.ToString()
        };
    }

    private static string ObtenirCouleurStatutInterne(
        StatutValidationInterne statut)
    {
        return statut switch
        {
            StatutValidationInterne.Autorisee =>
                "bg-success",

            StatutValidationInterne.Rejetee =>
                "bg-danger",

            _ => "bg-warning text-dark"
        };
    }

    private static string ObtenirLibelleStatutEditeur(
        StatutEditeur? statut)
    {
        return statut switch
        {
            null =>
                "Aucun statut éditeur",

            StatutEditeur.Soumis =>
                "Soumis",

            StatutEditeur.UnderReview =>
                "En cours d'évaluation",

            StatutEditeur.MinorRevision =>
                "Révisions mineures",

            StatutEditeur.MajorRevision =>
                "Révisions majeures",

            StatutEditeur.Accepted =>
                "Accepté",

            StatutEditeur.Published =>
                "Publié",

            StatutEditeur.Rejected =>
                "Rejeté",

            _ => statut.ToString()!
        };
    }

    private static string ObtenirCouleurStatutEditeur(
        StatutEditeur? statut)
    {
        return statut switch
        {
            null =>
                "bg-secondary",

            StatutEditeur.Soumis =>
                "bg-primary",

            StatutEditeur.UnderReview =>
                "bg-info text-dark",

            StatutEditeur.MinorRevision =>
                "bg-warning text-dark",

            StatutEditeur.MajorRevision =>
                "bg-warning text-dark",

            StatutEditeur.Accepted =>
                "bg-success",

            StatutEditeur.Published =>
                "bg-success",

            StatutEditeur.Rejected =>
                "bg-danger",

            _ => "bg-secondary"
        };
    }
}