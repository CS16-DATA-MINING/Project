using System;

namespace GestionRecherche.Web;

using System.ComponentModel.DataAnnotations;


public sealed class EtapeHistoriqueViewModel
{
    public string NiveauLibelle { get; init; } = string.Empty;

    public bool EstRejet { get; init; }

    public string DecisionLibelle { get; init; } = string.Empty;

    public string ValidateurNomComplet { get; init; } = string.Empty;

    public string? Commentaire { get; init; }

    public DateTime DateDecisionUtc { get; init; }

    public int NumeroCycleValidation { get; init; }
}


public sealed class ValidationIndexViewModel
{
    /*
     * Jour 6 : titre de la page selon la file affichée
     * (« Laboratoire », « UER », « Bureau de la Recherche »).
     */
    public string NiveauLibelle { get; init; } = string.Empty;

    /*
     * Jour 6 : entête de la colonne structure
     * (« Laboratoire », « UER », « Laboratoire d’origine »).
     */
    public string StructureLibelle { get; init; } = string.Empty;

    public List<DemandeValidationItemViewModel> Demandes
        { get; init; } = new();
}

public sealed class DemandeValidationItemViewModel
{
    public int Id { get; init; }

    public string Titre { get; init; } = string.Empty;

    public string AuteurNomComplet { get; init; }
        = string.Empty;

    /*
     * Jour 6 : « LaboratoireNom » est renommé « StructureNom »
     * car cette colonne affiche le labo OU l'UER selon le niveau.
     */
    public string StructureNom { get; init; }
        = string.Empty;

    public DateTime DateCreationUtc { get; init; }

    public int NumeroCycleValidation { get; init; }
}

public sealed class JournalCibleValidationViewModel
{
    public int OrdrePreference { get; init; }

    public string Nom { get; init; } = string.Empty;

    public string? Editeur { get; init; }

    public string? Issn { get; init; }

    public bool EstNouveauJournal { get; init; }
}

public sealed class ValidationDetailsViewModel
{
    public int Id { get; init; }

    public string Titre { get; init; } = string.Empty;

    public string Abstract { get; init; } = string.Empty;

    public string AuteurNomComplet { get; init; }
        = string.Empty;

    // Jour 6 : renommé (ex-LaboratoireNom).
    public string StructureNom { get; init; }
        = string.Empty;

    // Jour 6 : libellé affiché à gauche de StructureNom.
    public string StructureLibelle { get; init; }
        = string.Empty;

    // Jour 6 : texte du bouton vert selon le niveau.
    public string LibelleBoutonValidation { get; init; }
        = string.Empty;

    public DateTime DateCreationUtc { get; init; }

    public int NumeroCycleValidation { get; init; }

    public List<JournalCibleValidationViewModel> Journaux
        { get; init; } = new();

    public List<EtapeHistoriqueViewModel> Historique { get; init; } = new();    
    
}

public sealed class ValidationDecisionViewModel
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "La demande est invalide.")]
    public int DemandeId { get; set; }

    [StringLength(
        1000,
        ErrorMessage =
            "Le commentaire ne doit pas dépasser "
            + "1000 caractères.")]
    public string? Commentaire { get; set; }
}

public sealed class ValidationRejetViewModel
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "La demande est invalide.")]
    public int DemandeId { get; set; }

    [Required(
        AllowEmptyStrings = false,
        ErrorMessage =
            "Le motif du rejet est obligatoire.")]
    [StringLength(
        1000,
        MinimumLength = 3,
        ErrorMessage =
            "Le motif doit contenir entre "
            + "3 et 1000 caractères.")]
    public string Commentaire { get; set; }
        = string.Empty;
}