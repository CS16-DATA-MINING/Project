using GestionRecherche.Data;

namespace GestionRecherche.Business;

public sealed record FiltreAnomalies(
    TypeAnomalie? Type = null,
    int? LaboratoireId = null);

public sealed record LaboratoireFiltreDto(
    int Id,
    string Nom);

public sealed record AnomalieDto(
    int Id,
    int DemandePublicationId,
    string TitreDemande,
    string AuteurNomComplet,
    int? LaboratoireId,
    string LaboratoireNom,
    TypeAnomalie Type,
    StatutAnomalie Statut,
    StatutEditeur? StatutEditeurCourant,
    DateTime DateDetectionUtc,
    int DureeJours,
    string Message,
    DateTime? DateTraitementUtc,
    string? TraiteeParNomComplet,
    string? CommentaireTraitement);

public static class CodesTraitementAnomalie
{
    public const string Succes = "SUCCES";

    public const string DonneesInvalides =
        "DONNEES_INVALIDES";

    public const string AnomalieIntrouvable =
        "ANOMALIE_INTROUVABLE";

    public const string CommentaireObligatoire =
        "COMMENTAIRE_TRAITEMENT_OBLIGATOIRE";

    public const string CommentaireTropLong =
        "COMMENTAIRE_TRAITEMENT_TROP_LONG";

    public const string DejaTraitee =
        "ANOMALIE_DEJA_TRAITEE";

    public const string AccesInterdit =
        "ACCES_INTERDIT";

    public const string Conflit =
        "CONFLIT_TRAITEMENT";
}

public sealed record ResultatTraitementAnomalie(
    bool EstReussi,
    string Code,
    string Message)
{
    public static ResultatTraitementAnomalie Reussite(
        string message)
    {
        return new ResultatTraitementAnomalie(
            true,
            CodesTraitementAnomalie.Succes,
            message);
    }

    public static ResultatTraitementAnomalie Echec(
        string code,
        string message)
    {
        return new ResultatTraitementAnomalie(
            false,
            code,
            message);
    }
}