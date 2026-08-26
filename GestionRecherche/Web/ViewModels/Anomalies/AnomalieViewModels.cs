using System.ComponentModel.DataAnnotations;

using GestionRecherche.Data;

namespace GestionRecherche.Web;

public sealed class AnomaliesIndexViewModel
{
    public bool AfficherHistorique { get; init; }

    public TypeAnomalie? Type { get; init; }

    public int? LaboratoireId { get; init; }

    public List<LaboratoireFiltreViewModel> Laboratoires
        { get; init; } = new();

    public List<AnomalieListeItemViewModel> Anomalies
        { get; init; } = new();
}

public sealed class LaboratoireFiltreViewModel
{
    public int Id { get; init; }

    public string Nom { get; init; } = string.Empty;
}

public sealed class AnomalieListeItemViewModel
{
    public int Id { get; init; }

    public int DemandePublicationId { get; init; }

    public string TitreDemande { get; init; }
        = string.Empty;

    public string AuteurNomComplet { get; init; }
        = string.Empty;

    public string LaboratoireNom { get; init; }
        = string.Empty;

    public string TypeLibelle { get; init; }
        = string.Empty;

    public string TypeBadgeCss { get; init; }
        = string.Empty;

    public string StatutEditeurLibelle { get; init; }
        = string.Empty;

    public DateTime DateDetectionUtc { get; init; }

    public int DureeJours { get; init; }

    public string Message { get; init; } = string.Empty;

    public DateTime? DateTraitementUtc { get; init; }

    public string? TraiteeParNomComplet { get; init; }
}

public sealed class AnomalieDetailsViewModel
{
    public int Id { get; init; }

    public int DemandePublicationId { get; init; }

    public string TitreDemande { get; init; }
        = string.Empty;

    public string AuteurNomComplet { get; init; }
        = string.Empty;

    public string LaboratoireNom { get; init; }
        = string.Empty;

    public string TypeLibelle { get; init; }
        = string.Empty;

    public string TypeBadgeCss { get; init; }
        = string.Empty;

    public string StatutEditeurLibelle { get; init; }
        = string.Empty;

    public bool EstOuverte { get; init; }

    public DateTime DateDetectionUtc { get; init; }

    public int DureeJours { get; init; }

    public string Message { get; init; } = string.Empty;

    public DateTime? DateTraitementUtc { get; init; }

    public string? TraiteeParNomComplet { get; init; }

    public string? CommentaireTraitement { get; init; }

    public TraitementAnomalieViewModel Traitement
        { get; set; } = new();
}

public sealed class TraitementAnomalieViewModel
{
    [Range(
        1,
        int.MaxValue,
        ErrorMessage = "L'anomalie est invalide.")]
    public int AnomalieId { get; set; }

    [Display(Name = "Commentaire de traitement")]
    [Required(
        AllowEmptyStrings = false,
        ErrorMessage =
            "Le commentaire de traitement est obligatoire.")]
    [StringLength(
        1000,
        MinimumLength = 3,
        ErrorMessage =
            "Le commentaire doit contenir entre " +
            "3 et 1000 caractères.")]
    public string Commentaire { get; set; }
        = string.Empty;
}