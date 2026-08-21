using System;
using System.Security.Claims;
using GestionRecherche.Business;
using GestionRecherche.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GestionRecherche.Web;

[Authorize(Roles = "ChefLabo,ChefUER,ChefBureau,SecretariatConseil")]
public sealed class ValidationController : Controller
{
    private readonly IValidationService _validationService;

    public ValidationController(IValidationService validationService)
    {
        _validationService = validationService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        string? utilisateurId = ObtenirUtilisateurIdCourant();

        if (utilisateurId is null)
        {
            return Challenge();
        }

        NiveauValidation? niveau = ObtenirNiveauCourant();

        if (niveau is null)
        {
            return Forbid();
        }

        IReadOnlyList<DemandeValidationListeDto> demandes =
            await _validationService.ListerDemandesEnAttenteAsync(
                utilisateurId,
                niveau.Value,
                cancellationToken);

        var viewModel = new ValidationIndexViewModel
        {
            NiveauLibelle = ObtenirLibelleNiveau(niveau.Value),

            StructureLibelle = ObtenirLibelleStructure(niveau.Value),

            Demandes = demandes
                .Select(demande => new DemandeValidationItemViewModel
                {
                    Id = demande.Id,
                    Titre = demande.Titre,
                    AuteurNomComplet = demande.AuteurNomComplet,
                    StructureNom = demande.StructureNom,
                    DateCreationUtc = demande.DateCreationUtc,
                    NumeroCycleValidation = demande.NumeroCycleValidation
                })
                .ToList()
        };

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Details(
        int id,
        CancellationToken cancellationToken)
    {
        string? utilisateurId = ObtenirUtilisateurIdCourant();

        if (utilisateurId is null)
        {
            return Challenge();
        }

        NiveauValidation? niveau = ObtenirNiveauCourant();

        if (niveau is null)
        {
            return Forbid();
        }

        return await AfficherDetailsAsync(
            id, utilisateurId, niveau.Value, cancellationToken);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]

    public async Task<IActionResult> Valider(
        ValidationDecisionViewModel model,
        CancellationToken cancellationToken)
    {
        string? utilisateurId = ObtenirUtilisateurIdCourant();

        if (utilisateurId is null)
        {
            return Challenge();
        }

        NiveauValidation? niveau = ObtenirNiveauCourant();

        if (niveau is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return await AfficherDetailsAsync(
                model.DemandeId, utilisateurId,
                niveau.Value, cancellationToken);
        }

        ResultatValidation resultat =
            await _validationService.ValiderAuNiveauAsync(
                model.DemandeId,
                utilisateurId,
                niveau.Value,
                model.Commentaire,
                cancellationToken);

        if (!resultat.EstReussi)
        {
            return await TraiterEchecAsync(
                resultat, model.DemandeId, utilisateurId,
                niveau.Value, cancellationToken);
        }

        TempData["Succes"] =
            ObtenirMessageSuccesValidation(niveau.Value);

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rejeter(
        ValidationRejetViewModel model,
        CancellationToken cancellationToken)
    {
        string? utilisateurId = ObtenirUtilisateurIdCourant();

        if (utilisateurId is null)
        {
            return Challenge();
        }

        NiveauValidation? niveau = ObtenirNiveauCourant();

        if (niveau is null)
        {
            return Forbid();
        }

        if (!ModelState.IsValid)
        {
            return await AfficherDetailsAsync(
                model.DemandeId, utilisateurId,
                niveau.Value, cancellationToken);
        }

        ResultatValidation resultat =
            await _validationService.RejeterAuNiveauAsync(
                model.DemandeId,
                utilisateurId,
                niveau.Value,
                model.Commentaire,
                cancellationToken);

        if (!resultat.EstReussi)
        {
            return await TraiterEchecAsync(
                resultat, model.DemandeId, utilisateurId,
                niveau.Value, cancellationToken);
        }

        TempData["Succes"] =
            "La demande a été rejetée et la décision a été historisée.";

        return RedirectToAction(nameof(Index));
    }

    // =====================================================
    // Méthodes privées
    // =====================================================

    private string? ObtenirUtilisateurIdCourant()
    {


        return User
            .FindFirst(ClaimTypes.NameIdentifier)
            ?.Value;
    }

    /*
     * Le rôle Identity détermine la file affichée.
     * Ordre de priorité explicite si un compte cumule
     * plusieurs rôles (cas anormal, mais prévisible).
     */
    private NiveauValidation? ObtenirNiveauCourant()
    {
        if (User.IsInRole("ChefLabo"))
        {
            return NiveauValidation.Labo;
        }

        if (User.IsInRole("ChefUER"))
        {
            return NiveauValidation.Uer;
        }

        if (User.IsInRole("ChefBureau"))
        {
            return NiveauValidation.Bureau;
        }
        if (User.IsInRole("SecretariatConseil"))
{
    return NiveauValidation.Conseil;
}

        return null;
    }

    private static string ObtenirLibelleNiveau(
        NiveauValidation niveau) => niveau switch
    {
        NiveauValidation.Labo => "Laboratoire",
        NiveauValidation.Uer => "UER",
        NiveauValidation.Bureau => "Bureau de la Recherche",
        NiveauValidation.Conseil => "Conseil Scientifique",
        _ => niveau.ToString()
    };

    private static string ObtenirLibelleStructure(
        NiveauValidation niveau) => niveau switch
    {
        NiveauValidation.Labo => "Laboratoire",
        NiveauValidation.Uer => "UER",
        NiveauValidation.Bureau => "Laboratoire d’origine",
        
        // Dans ObtenirLibelleStructure :
        NiveauValidation.Conseil => "Laboratoire d’origine",

        _ => "Structure"
    };

    private static string ObtenirLibelleBoutonValidation(
        NiveauValidation niveau) => niveau switch
    {
        NiveauValidation.Labo =>
            "Valider et transmettre au Chef UER",
        NiveauValidation.Uer =>
            "Valider et transmettre au Bureau de la Recherche",
        NiveauValidation.Bureau =>
            "Valider et transmettre au Conseil Scientifique",

        NiveauValidation.Conseil =>
             "Autoriser la publication (décision finale)",

        _ => "Valider"
    };

    private static string ObtenirMessageSuccesValidation(
        NiveauValidation niveau) => niveau switch
    {
        NiveauValidation.Labo =>
            "La demande a été validée et transmise au Chef UER.",
        NiveauValidation.Uer =>
            "La demande a été validée et transmise au Chef "
            + "du Bureau de la Recherche.",
        NiveauValidation.Bureau =>
            "La demande a été validée et transmise au "
            + "Conseil Scientifique.",
        NiveauValidation.Conseil =>
    "La demande a été AUTORISÉE par le Conseil Scientifique. "
    + "Le délai de soumission réelle (D1) commence maintenant.",
    

        _ => "La demande a été validée."
    };

   private async Task<IActionResult> AfficherDetailsAsync(
    int demandeId,
    string utilisateurId,
    NiveauValidation niveau,
    CancellationToken cancellationToken)
{
    DemandeValidationDetailsDto? demande =
        await _validationService.ObtenirDetailsAsync(
            demandeId, utilisateurId, niveau, cancellationToken);

    if (demande is null)
    {
        return NotFound();
    }

    // ➊ NOUVEAU (Jour 8) : appelé seulement si l'accès
    //    est déjà validé par ObtenirDetailsAsync.
    IReadOnlyList<EtapeHistoriqueDto> historique =
        await _validationService.ObtenirHistoriqueAsync(
            demandeId, cancellationToken);

    var viewModel = new ValidationDetailsViewModel
    {
        Id = demande.Id,
        Titre = demande.Titre,
        Abstract = demande.Abstract,
        AuteurNomComplet = demande.AuteurNomComplet,
        StructureNom = demande.StructureNom,
        StructureLibelle = ObtenirLibelleStructure(niveau),
        LibelleBoutonValidation =
            ObtenirLibelleBoutonValidation(niveau),
        DateCreationUtc = demande.DateCreationUtc,
        NumeroCycleValidation = demande.NumeroCycleValidation,

        Journaux = demande.Journaux
            .Select(journal => new JournalCibleValidationViewModel
            {
                OrdrePreference = journal.OrdrePreference,
                Nom = journal.Nom,
                Editeur = journal.Editeur,
                Issn = journal.Issn,
                EstNouveauJournal = journal.EstNouveauJournal
            })
            .ToList(),

        // ➋ NOUVEAU (Jour 8) : mapping DTO -> ViewModel.
        Historique = historique
            .Select(etape => new EtapeHistoriqueViewModel
            {
                NiveauLibelle =
                    ObtenirLibelleNiveau(etape.Niveau),

                EstRejet = etape.Decision ==
                    DecisionValidation.Rejetee,

                DecisionLibelle = etape.Decision ==
                    DecisionValidation.Rejetee
                        ? "Rejetée"
                        : "Validée",

                ValidateurNomComplet =
                    etape.ValidateurNomComplet,

                Commentaire = etape.Commentaire,
                DateDecisionUtc = etape.DateDecisionUtc,
                NumeroCycleValidation =
                    etape.NumeroCycleValidation
            })
            .ToList()
    };

    return View("Details", viewModel);
}

    private async Task<IActionResult> TraiterEchecAsync(
        ResultatValidation resultat,
        int demandeId,
        string utilisateurId,
        NiveauValidation niveau,
        CancellationToken cancellationToken)
    {
        if (resultat.Code == CodesValidation.AccesInterdit)
        {
            return Forbid();
        }

        if (resultat.Code == CodesValidation.DemandeIntrouvable)
        {
            return NotFound();
        }

        if (resultat.Code is
            CodesValidation.CommentaireRejetObligatoire
            or CodesValidation.DonneesInvalides)
        {
            ModelState.AddModelError(string.Empty, resultat.Message);

            return await AfficherDetailsAsync(
                demandeId, utilisateurId, niveau, cancellationToken);
        }

        TempData["Erreur"] = resultat.Message;

        return RedirectToAction(nameof(Index));
    }
}