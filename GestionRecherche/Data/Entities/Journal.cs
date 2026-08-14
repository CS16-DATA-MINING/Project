using System;

namespace GestionRecherche.Data;

public class Journal
{
 public int Id { get; set; }

    public string Nom { get; set; } = string.Empty;

    public string NomNormalise { get; set; } = string.Empty;

    public string Editeur { get; set; } = string.Empty;

    public string Issn { get; set; } = string.Empty;

    public string IssnNormalise { get; set; } = string.Empty;

    public bool EstActif { get; set; } = true;

    public DateTime DateCreationUtc { get; set; } = DateTime.UtcNow;

    public ICollection<JournalCibleDemande> DemandesCibles { get; set; }
        = new List<JournalCibleDemande>();
}
