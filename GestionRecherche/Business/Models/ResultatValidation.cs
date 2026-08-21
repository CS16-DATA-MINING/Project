using System;

namespace GestionRecherche.Business;

public static class CodesValidation
{
    public const string Succes = "SUCCES";
    public const string DonneesInvalides = "DONNEES_INVALIDES";
    public const string DemandeIntrouvable = "DEMANDE_INTROUVABLE";
    public const string CommentaireRejetObligatoire =
        "COMMENTAIRE_REJET_OBLIGATOIRE";
    public const string NiveauIncorrect = "NIVEAU_INCORRECT";
    public const string DemandeTerminee = "DEMANDE_TERMINEE";
    public const string AccesInterdit = "ACCES_INTERDIT";

    public const string ResoumissionImpossible = "RESOUMISSION_IMPOSSIBLE";
}

public sealed record ResultatValidation(
    bool EstReussi,
    string Code,
    string Message)
{
    public static ResultatValidation Reussite(string message)
    {
        return new ResultatValidation(
            true,
            CodesValidation.Succes,
            message);
    }

    public static ResultatValidation Echec(
        string code,
        string message)
    {
        return new ResultatValidation(
            false,
            code,
            message);
    }
}