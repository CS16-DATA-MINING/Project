using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace GestionRecherche.Web;

public sealed class DepotViewModel
{
    [Required]
    [StringLength(300)]
    public string Titre { get; set; } = string.Empty;

    [Required]
    [StringLength(4000)]
    public string Abstract { get; set; } = string.Empty;

    // Maximum allowed by admin; UI will enforce client-side
    public List<JournalPropositionViewModel> Journaux { get; set; } = new();
}

public sealed class JournalPropositionViewModel
{
    public int OrdrePreference { get; set; }
    public int? JournalId { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Editeur { get; set; }
    public string? Issn { get; set; }
    public bool EstNouveauJournal { get; set; }
}

public sealed class MesDemandesViewModel
{
    public List<DemandeSummaryViewModel> MesDemandes { get; set; } = new();
}

public sealed class DemandeSummaryViewModel
{
    public int Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Statut { get; set; } = string.Empty;
    public DateTime DateCreationUtc { get; set; }
}

public sealed class DemandeDetailsViewModel
{
    public int Id { get; set; }
    public string Titre { get; set; } = string.Empty;
    public string Abstract { get; set; } = string.Empty;
    public string AuteurNomComplet { get; set; } = string.Empty;
    public List<JournalPropositionViewModel> Journaux { get; set; } = new();
    public string Statut { get; set; } = string.Empty;
}
