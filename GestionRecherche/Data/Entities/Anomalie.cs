using System;

namespace GestionRecherche.Data;

public class Anomalie
{
  public int Id { get; set; }

    public int DemandePublicationId { get; set; }

    public DemandePublication DemandePublication { get; set; } = null!;

    public TypeAnomalie Type { get; set; }

    public StatutAnomalie Statut { get; set; }
        = StatutAnomalie.Ouverte;

    public DateTime DateDetectionUtc { get; set; } = DateTime.UtcNow;

    public int DureeJours { get; set; }

    public string Message { get; set; } = string.Empty;

    public DateTime? DateTraitementUtc { get; set; }

    public string? TraiteeParId { get; set; }

    public ApplicationUser? TraiteePar { get; set; }

    public string? CommentaireTraitement { get; set; }
}

