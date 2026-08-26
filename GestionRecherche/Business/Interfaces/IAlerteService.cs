using System;

namespace GestionRecherche.Business;

public interface IAlerteService
{
Task<ResultatPassageAlerte> ExecuterPassageAsync(
        CancellationToken cancellationToken = default);

}
