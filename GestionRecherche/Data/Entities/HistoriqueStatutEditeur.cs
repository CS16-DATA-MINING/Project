using System;

namespace GestionRecherche.Data;

public class HistoriqueStatutEditeur
{
public int Id { get; set; }

    public int DemandePublicationId { get; set; }

    public DemandePublication DemandePublication { get; set; } = null!;

    public StatutEditeur? AncienStatut { get; set; }

    public StatutEditeur NouveauStatut { get; set; }

    public DateTime DateChangementUtc { get; set; } = DateTime.UtcNow;

    public string ModifieParId { get; set; } = string.Empty;

    public ApplicationUser ModifiePar { get; set; } = null!;

    public string? Commentaire { get; set; }

}
