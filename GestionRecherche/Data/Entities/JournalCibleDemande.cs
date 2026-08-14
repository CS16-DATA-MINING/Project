using System;

namespace GestionRecherche.Data;

public class JournalCibleDemande
{

 public int Id { get; set; }

    public int DemandePublicationId { get; set; }

    public DemandePublication DemandePublication { get; set; } = null!;

    public int? JournalId { get; set; }

    public Journal? Journal { get; set; }

    public bool EstNouveauJournal { get; set; }

    public string? NomJournalSaisiManuellement { get; set; }

    public string? NomJournalNormalise { get; set; }

    public string? EditeurSaisiManuellement { get; set; }

    public string? IssnSaisiManuellement { get; set; }

    public string? IssnNormalise { get; set; }

    public int OrdrePreference { get; set; }

    public StatutModeration StatutModeration { get; set; }
        = StatutModeration.NonRequise;

    public DateTime DateCreationUtc { get; set; } = DateTime.UtcNow;



}
