namespace GestionRecherche.Business;

public interface IAnomalieService
{
    Task<IReadOnlyList<AnomalieDto>> ListerOuvertesAsync(
        FiltreAnomalies filtre,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AnomalieDto>> ListerTraiteesAsync(
        FiltreAnomalies filtre,
        CancellationToken cancellationToken = default);

    Task<AnomalieDto?> ObtenirAsync(
        int anomalieId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LaboratoireFiltreDto>>
        ListerLaboratoiresAsync(
            CancellationToken cancellationToken = default);

    Task<ResultatTraitementAnomalie> MarquerTraiteeAsync(
        int anomalieId,
        string chefBureauId,
        string? commentaire,
        CancellationToken cancellationToken = default);
}