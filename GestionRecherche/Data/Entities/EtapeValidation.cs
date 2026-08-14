using System;

namespace GestionRecherche.Data;

public class EtapeValidation
{ public int Id { get; set; }

    public int DemandePublicationId { get; set; }

    public DemandePublication DemandePublication { get; set; } = null!;

    public string ValidateurId { get; set; } = string.Empty;

    public ApplicationUser Validateur { get; set; } = null!;

    public NiveauValidation Niveau { get; set; }

    public DecisionValidation Decision { get; set; }

    public string? Commentaire { get; set; }

    public DateTime DateDecisionUtc { get; set; } = DateTime.UtcNow;

    public int NumeroCycleValidation { get; set; } = 1;

}
