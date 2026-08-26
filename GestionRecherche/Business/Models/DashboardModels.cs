using GestionRecherche.Data;

namespace GestionRecherche.Business;

public sealed record CompteurStatutInterneDto(
    StatutValidationInterne Statut,
    int Nombre);

public sealed record CompteurStatutEditeurDto(
    StatutEditeur? Statut,
    int Nombre);

public sealed record StatistiqueLaboratoireDto(
    int? Id,
    string Code,
    string Nom,
    bool EstActif,
    int NombreDemandes,
    int NombreAnomaliesOuvertes,
    int NombreAnomaliesTraitees);

public sealed record DashboardDto(
    DateTime DateGenerationUtc,
    int NombreTotalDemandes,
    int NombreDemandesAutorisees,
    int NombreArticlesPublies,
    int NombreAnomaliesOuvertes,
    IReadOnlyList<CompteurStatutInterneDto> StatutsInternes,
    IReadOnlyList<CompteurStatutEditeurDto> StatutsEditeur,
    IReadOnlyList<StatistiqueLaboratoireDto> Laboratoires);