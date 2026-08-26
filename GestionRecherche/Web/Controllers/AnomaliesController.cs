using System.Security.Claims;

using GestionRecherche.Business;
using GestionRecherche.Data;
using GestionRecherche.Web.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionRecherche.Web;


[Authorize(
    Policy = PolitiquesAutorisation.ChefBureauActif)]
public sealed class AnomaliesController : Controller
{
    private readonly IAnomalieService _anomalieService;

    public AnomaliesController(
        IAnomalieService anomalieService)
    {
        _anomalieService = anomalieService;
    }



    [HttpGet]
    public async Task<IActionResult> Index(
        TypeAnomalie? type,
        int? laboratoireId,
        CancellationToken cancellationToken)
    {
        AnomaliesIndexViewModel model =
            await ConstruireListeAsync(
                false,
                type,
                laboratoireId,
                cancellationToken);

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Historique(
        TypeAnomalie? type,
        int? laboratoireId,
        CancellationToken cancellationToken)
    {
        AnomaliesIndexViewModel model =
            await ConstruireListeAsync(
                true,
                type,
                laboratoireId,
                cancellationToken);

        return View("Index", model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        int id,
        CancellationToken cancellationToken)
    {
        return await AfficherDetailsAsync(
            id,
            null,
            cancellationToken);
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Traiter(
        TraitementAnomalieViewModel saisie,
        CancellationToken cancellationToken)
    {
        string? utilisateurId =
            User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

        if (utilisateurId is null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return await AfficherDetailsAsync(
                saisie.AnomalieId,
                saisie,
                cancellationToken);
        }

        ResultatTraitementAnomalie resultat =
            await _anomalieService.MarquerTraiteeAsync(
                saisie.AnomalieId,
                utilisateurId,
                saisie.Commentaire,
                cancellationToken);

        if (resultat.EstReussi)
        {
            TempData["Succes"] = resultat.Message;

            return RedirectToAction(nameof(Index));
        }

        if (resultat.Code ==
            CodesTraitementAnomalie.AccesInterdit)
        {
            return Forbid();
        }

        if (resultat.Code ==
            CodesTraitementAnomalie.AnomalieIntrouvable)
        {
            return NotFound();
        }

        if (resultat.Code ==
            CodesTraitementAnomalie.DejaTraitee)
        {
            TempData["Information"] = resultat.Message;

            return RedirectToAction(nameof(Historique));
        }

        ModelState.AddModelError(
            nameof(TraitementAnomalieViewModel.Commentaire),
            resultat.Message);

        return await AfficherDetailsAsync(
            saisie.AnomalieId,
            saisie,
            cancellationToken);
    }

    private async Task<AnomaliesIndexViewModel>
        ConstruireListeAsync(
            bool afficherHistorique,
            TypeAnomalie? type,
            int? laboratoireId,
            CancellationToken cancellationToken)
    {
        var filtre = new FiltreAnomalies(
            type,
            laboratoireId);

        IReadOnlyList<AnomalieDto> anomalies =
            afficherHistorique
                ? await _anomalieService
                    .ListerTraiteesAsync(
                        filtre,
                        cancellationToken)
                : await _anomalieService
                    .ListerOuvertesAsync(
                        filtre,
                        cancellationToken);

        IReadOnlyList<LaboratoireFiltreDto> laboratoires =
            await _anomalieService
                .ListerLaboratoiresAsync(
                    cancellationToken);

        return new AnomaliesIndexViewModel
        {
            AfficherHistorique = afficherHistorique,
            Type = type,
            LaboratoireId = laboratoireId,

            Laboratoires = laboratoires
                .Select(laboratoire =>
                    new LaboratoireFiltreViewModel
                    {
                        Id = laboratoire.Id,
                        Nom = laboratoire.Nom
                    })
                .ToList(),

            Anomalies = anomalies
                .Select(ConstruireItem)
                .ToList()
        };
    }

    private async Task<IActionResult> AfficherDetailsAsync(
        int anomalieId,
        TraitementAnomalieViewModel? saisie,
        CancellationToken cancellationToken)
    {
        AnomalieDto? anomalie =
            await _anomalieService.ObtenirAsync(
                anomalieId,
                cancellationToken);

        if (anomalie is null)
        {
            return NotFound();
        }

        AnomalieDetailsViewModel model =
            ConstruireDetails(anomalie);

        model.Traitement = saisie ??
            new TraitementAnomalieViewModel
            {
                AnomalieId = anomalie.Id
            };

        return View("Details", model);
    }

    private static AnomalieListeItemViewModel ConstruireItem(
        AnomalieDto anomalie)
    {
        return new AnomalieListeItemViewModel
        {
            Id = anomalie.Id,
            DemandePublicationId =
                anomalie.DemandePublicationId,
            TitreDemande = anomalie.TitreDemande,
            AuteurNomComplet =
                anomalie.AuteurNomComplet,
            LaboratoireNom =
                anomalie.LaboratoireNom,
            TypeLibelle =
                ObtenirLibelleType(anomalie.Type),
            TypeBadgeCss =
                ObtenirClasseType(anomalie.Type),
            StatutEditeurLibelle =
                ObtenirLibelleStatutEditeur(
                    anomalie.StatutEditeurCourant),
            DateDetectionUtc =
                anomalie.DateDetectionUtc,
            DureeJours = anomalie.DureeJours,
            Message = anomalie.Message,
            DateTraitementUtc =
                anomalie.DateTraitementUtc,
            TraiteeParNomComplet =
                anomalie.TraiteeParNomComplet
        };
    }

    private static AnomalieDetailsViewModel ConstruireDetails(
        AnomalieDto anomalie)
    {
        return new AnomalieDetailsViewModel
        {
            Id = anomalie.Id,
            DemandePublicationId =
                anomalie.DemandePublicationId,
            TitreDemande = anomalie.TitreDemande,
            AuteurNomComplet =
                anomalie.AuteurNomComplet,
            LaboratoireNom =
                anomalie.LaboratoireNom,
            TypeLibelle =
                ObtenirLibelleType(anomalie.Type),
            TypeBadgeCss =
                ObtenirClasseType(anomalie.Type),
            StatutEditeurLibelle =
                ObtenirLibelleStatutEditeur(
                    anomalie.StatutEditeurCourant),
            EstOuverte =
                anomalie.Statut ==
                StatutAnomalie.Ouverte,
            DateDetectionUtc =
                anomalie.DateDetectionUtc,
            DureeJours = anomalie.DureeJours,
            Message = anomalie.Message,
            DateTraitementUtc =
                anomalie.DateTraitementUtc,
            TraiteeParNomComplet =
                anomalie.TraiteeParNomComplet,
            CommentaireTraitement =
                anomalie.CommentaireTraitement
        };
    }

    private static string ObtenirLibelleType(
        TypeAnomalie type) => type switch
    {
        TypeAnomalie.NonSoumission =>
            "Non-soumission (D1)",

        TypeAnomalie.Stagnation =>
            "Stagnation (D2)",

        _ => type.ToString()
    };

    private static string ObtenirClasseType(
        TypeAnomalie type) => type switch
    {
        TypeAnomalie.NonSoumission =>
            "bg-danger",

        TypeAnomalie.Stagnation =>
            "bg-warning text-dark",

        _ => "bg-secondary"
    };

    private static string ObtenirLibelleStatutEditeur(
        StatutEditeur? statut) => statut switch
    {
        null => "Non soumis",
        StatutEditeur.Soumis => "Soumis",
        StatutEditeur.UnderReview => "Under Review",
        StatutEditeur.MinorRevision => "Minor Revision",
        StatutEditeur.MajorRevision => "Major Revision",
        StatutEditeur.Accepted => "Accepted",
        StatutEditeur.Published => "Published",
        StatutEditeur.Rejected => "Rejected",
        _ => statut.ToString() ?? "Non renseigné"
    };
}