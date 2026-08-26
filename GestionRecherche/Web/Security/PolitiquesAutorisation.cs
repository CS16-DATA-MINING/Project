namespace GestionRecherche.Web.Security;

public static class RolesMetier
{
    public const string Chercheur = "Chercheur";
    public const string ChefLabo = "ChefLabo";
    public const string ChefUer = "ChefUER";
    public const string ChefBureau = "ChefBureau";
    public const string SecretariatConseil = "SecretariatConseil";
    public const string Admin = "Admin";
}

public static class PolitiquesAutorisation
{
    public const string ValidationInterne =
        "ValidationInterne";

    public const string ChefBureauActif =
        "ChefBureauActif";

    public const string Administration =
        "Administration";
}