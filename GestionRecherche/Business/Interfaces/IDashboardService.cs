using System;

namespace GestionRecherche.Business;

public interface IDashboardService
{
  Task<DashboardDto> ObtenirTableauDeBordAsync(
        CancellationToken cancellationToken = default);
Task<int> CompterAnomaliesOuvertesAsync(
        CancellationToken cancellationToken = default);

}
