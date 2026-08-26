namespace GestionRecherche.Web;

public sealed class DashboardViewModel
{
    public DateTime DateGenerationUtc { get; init; }

    public int NombreTotalDemandes { get; init; }

    public int NombreDemandesAutorisees { get; init; }

    public int NombreArticlesPublies { get; init; }

    public int NombreAnomaliesOuvertes { get; init; }

    public List<CompteurDashboardViewModel> StatutsInternes
        { get; init; } = new();

    public List<CompteurDashboardViewModel> StatutsEditeur
        { get; init; } = new();

    public List<StatistiqueLaboratoireViewModel> Laboratoires
        { get; init; } = new();
}

public sealed class CompteurDashboardViewModel
{
    public string Libelle { get; init; } = string.Empty;

    public int Nombre { get; init; }

    public string BadgeCss { get; init; } = "bg-secondary";
}

public sealed class StatistiqueLaboratoireViewModel
{
    public int? Id { get; init; }

    public string Code { get; init; } = string.Empty;

    public string Nom { get; init; } = string.Empty;

    public string EtatLibelle { get; init; } = string.Empty;

    public string EtatBadgeCss { get; init; } = "bg-secondary";

    public int NombreDemandes { get; init; }

    public int NombreAnomaliesOuvertes { get; init; }

    public int NombreAnomaliesTraitees { get; init; }

    public int NombreTotalAnomalies =>
        NombreAnomaliesOuvertes +
        NombreAnomaliesTraitees;
}