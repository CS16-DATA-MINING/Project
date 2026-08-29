using System;

namespace GestionRecherche.Web;

public sealed class DeclareSubmissionViewModel
{
    public int DemandeId { get; set; }

    public DateTime DateSoumission { get; set; } = DateTime.UtcNow;

    public string? NumeroSuivi { get; set; }

    public string? CheminPreuve { get; set; }
}

public sealed class HistoriqueViewModel
{
    public int DemandeId { get; set; }
    public List<HistoriqueItemViewModel> Items { get; set; } = new();
}

public sealed class HistoriqueItemViewModel
{
    public DateTime DateUtc { get; set; }
    public string Statut { get; set; } = string.Empty;
    public string? Details { get; set; }
}
