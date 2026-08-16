using System;

namespace GestionRecherche.Business;

public interface IRoutingService
{
    Task<ResultatRoutage> DeterminerRoutageAsync(int demandeId,CancellationToken cancellationToken = default);

}
