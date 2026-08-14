using System;

namespace GestionRecherche.Data;

public class DemandePublication
{
    public int Id { get; set; }

    public string Titre { get; set; } = string.Empty;

    public string Abstract { get; set; } = string.Empty;

    public string AuteurId { get; set; } = string.Empty;

    public ApplicationUser Auteur { get; set; } = null!;

    public StatutValidationInterne StatutValidationInterne { get; set; }
        = StatutValidationInterne.EnAttenteLabo;

    public int NumeroCycleValidation { get; set; } = 1;

    public DateTime DateCreationUtc { get; set; } = DateTime.UtcNow;

    public DateTime? DateAutorisationConseilUtc { get; set; }

    public DateTime? DateSoumissionReelleUtc { get; set; }

    public string? NumeroSuivi { get; set; }

    public string? NomOriginalPreuve { get; set; }

    public string? CheminStockagePreuve { get; set; }

    public string? TypeContenuPreuve { get; set; }

    public StatutEditeur? StatutEditeurCourant { get; set; }

    public DateTime? DateDerniereMiseAJourUtc { get; set; }

    public ICollection<JournalCibleDemande> JournauxCibles { get; set; }
        = new List<JournalCibleDemande>();

    public ICollection<EtapeValidation> EtapesValidation { get; set; }
        = new List<EtapeValidation>();

    public ICollection<Anomalie> Anomalies { get; set; }
        = new List<Anomalie>();

    public ICollection<HistoriqueStatutEditeur> HistoriqueStatutsEditeur { get; set; }
        = new List<HistoriqueStatutEditeur>();

}
