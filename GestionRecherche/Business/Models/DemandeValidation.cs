using System;
using GestionRecherche.Data;

namespace GestionRecherche.Business;

/*
 * Une ligne de l'historique des décisions (Jour 8).
 * Objet de transport pur : aucune logique.
 */
public sealed class EtapeHistoriqueDto
{
    public NiveauValidation Niveau { get; init; }

    public DecisionValidation Decision { get; init; }

    public string ValidateurNomComplet { get; init; } = string.Empty;

    public string? Commentaire { get; init; }

    public DateTime DateDecisionUtc { get; init; }

    public int NumeroCycleValidation { get; init; }
}


public sealed class DemandeValidationListeDto
{
    public int Id { get; init; }

    public string Titre { get; init; } = string.Empty;

    public string AuteurNomComplet { get; init; } = string.Empty;

    /*
     
     * - niveau Labo   -> nom du laboratoire
     * - niveau UER    -> nom de l'UER
     * - niveau Bureau -> laboratoire d'origine (utile au Bureau)
     structure  est pour  les troix niveaux 
     */
    public string StructureNom { get; init; } = string.Empty;

    public DateTime DateCreationUtc { get; init; }

    public int NumeroCycleValidation { get; init; }
}

public sealed class JournalCibleValidationDto
{
    public int OrdrePreference { get; init; }

    public string Nom { get; init; } = string.Empty;

    public string? Editeur { get; init; }

    public string? Issn { get; init; }

    public bool EstNouveauJournal { get; init; }
}

public sealed class DemandeValidationDetailsDto
{
    public int Id { get; init; }

    public string Titre { get; init; } = string.Empty;

    public string Abstract { get; init; } = string.Empty;

    public string AuteurNomComplet { get; init; } = string.Empty;

    public string StructureNom { get; init; } = string.Empty;

    public DateTime DateCreationUtc { get; init; }

    public int NumeroCycleValidation { get; init; }

    public List<JournalCibleValidationDto> Journaux { get; init; } = new();
}