using GestionRecherche.Data;

namespace GestionRecherche.Business;

public static class CodesRoutage
{
    public const string Succes = "SUCCES";
    public const string DonneesInvalides = "DONNEES_INVALIDES";
    public const string DemandeIntrouvable = "DEMANDE_INTROUVABLE";
    public const string AuteurIntrouvable = "AUTEUR_INTROUVABLE";
    public const string DemandeTerminee = "DEMANDE_TERMINEE";
    public const string StatutInvalide = "STATUT_INVALIDE";

    public const string AuteurSansLaboratoire =
        "AUTEUR_SANS_LABORATOIRE";

    public const string LaboratoireIntrouvable =
        "LABORATOIRE_INTROUVABLE";

    public const string LaboratoireSansChef =
        "LABORATOIRE_SANS_CHEF";

    public const string AuteurSansUer =
        "AUTEUR_SANS_UER";

    public const string UerIntrouvable =
        "UER_INTROUVABLE";

    public const string UerSansChef =
        "UER_SANS_CHEF";

    public const string RoleChefBureauManquant =
        "ROLE_CHEF_BUREAU_MANQUANT";

    public const string RoleSecretariatConseilManquant =
        "ROLE_SECRETARIAT_CONSEIL_MANQUANT";
}

public sealed record ResultatRoutage(
    bool EstReussi,
    string Code,
    string Message,
    NiveauValidation? Niveau,
    string? ValidateurId,
    string? NomCompletValidateur)
{
    public static ResultatRoutage Reussite(
        NiveauValidation niveau,
        string validateurId,
        string nomCompletValidateur,
        string message)
    {
        return new ResultatRoutage(
            true,
            CodesRoutage.Succes,
            message,
            niveau,
            validateurId,
            nomCompletValidateur);
    }

    public static ResultatRoutage Echec(
        string code,
        string message,
        NiveauValidation? niveau = null)
    {
        return new ResultatRoutage(
            false,
            code,
            message,
            niveau,
            null,
            null);
    }
}