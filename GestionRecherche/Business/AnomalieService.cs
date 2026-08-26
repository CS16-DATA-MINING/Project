using System.Linq.Expressions;

using GestionRecherche.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GestionRecherche.Business;

public sealed class AnomalieService : IAnomalieService
{
    private const string RoleChefBureauNormalise =
        "CHEFBUREAU";

    private const int LongueurMaxCommentaire = 1000;

    private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<AnomalieService> _logger;

    public AnomalieService(
        AppDbContext dbContext,
        TimeProvider timeProvider,
        ILogger<AnomalieService> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _logger = logger;
    }


// recuperer les anomaliees a traiter
    public async Task<IReadOnlyList<AnomalieDto>>
        ListerOuvertesAsync(
            FiltreAnomalies filtre,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtre);

        return await ConstruireRequete(
                StatutAnomalie.Ouverte,
                filtre)
            .OrderByDescending(anomalie =>
                anomalie.DateDetectionUtc)
            .Select(Projection)
            .ToListAsync(cancellationToken);
    }

    // recuperer les ancien anomalie 

    public async Task<IReadOnlyList<AnomalieDto>>
        ListerTraiteesAsync(
            FiltreAnomalies filtre,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtre);

        return await ConstruireRequete(
                StatutAnomalie.Traitee,
                filtre)
            .OrderByDescending(anomalie =>
                anomalie.DateTraitementUtc)
            .ThenByDescending(anomalie =>
                anomalie.DateDetectionUtc)
            .Select(Projection)
            .ToListAsync(cancellationToken);
    }

    //obtenir les detaille d une anomalie

    public async Task<AnomalieDto?> ObtenirAsync(
        int anomalieId,
        CancellationToken cancellationToken = default)
    {
        if (anomalieId <= 0)
        {
            return null;
        }

        return await _dbContext.Anomalies
            .AsNoTracking()
            .Where(anomalie => anomalie.Id == anomalieId)
            .Select(Projection)
            .SingleOrDefaultAsync(cancellationToken);
    }


// remplir le filtre labo 
    public async Task<IReadOnlyList<LaboratoireFiltreDto>>
        ListerLaboratoiresAsync(
            CancellationToken cancellationToken = default)
    {
        return await _dbContext.Laboratoires
            .AsNoTracking()
            .Where(laboratoire => laboratoire.EstActif)
            .OrderBy(laboratoire => laboratoire.Nom)
            .Select(laboratoire =>
                new LaboratoireFiltreDto(
                    laboratoire.Id,
                    laboratoire.Nom))
            .ToListAsync(cancellationToken);
    }



    public async Task<ResultatTraitementAnomalie>
        MarquerTraiteeAsync(
            int anomalieId,
            string chefBureauId,
            string? commentaire,
            CancellationToken cancellationToken = default)
    {
        if (anomalieId <= 0 ||
            string.IsNullOrWhiteSpace(chefBureauId))
        {
            return ResultatTraitementAnomalie.Echec(
                CodesTraitementAnomalie.DonneesInvalides,
                "Les informations de traitement sont invalides.");
        }

        string commentaireNormalise =
            commentaire?.Trim() ?? string.Empty;

        if (commentaireNormalise.Length == 0)
        {
            return ResultatTraitementAnomalie.Echec(
                CodesTraitementAnomalie.CommentaireObligatoire,
                "Le commentaire de traitement est obligatoire.");
        }

        if (commentaireNormalise.Length >
            LongueurMaxCommentaire)
        {
            return ResultatTraitementAnomalie.Echec(
                CodesTraitementAnomalie.CommentaireTropLong,
                "Le commentaire ne doit pas dépasser " +
                $"{LongueurMaxCommentaire} caractères.");
        }

        string chefBureauIdNormalise =
            chefBureauId.Trim();

        bool estChefBureau =
            await EstChefBureauActifAsync(
                chefBureauIdNormalise,
                cancellationToken);

        if (!estChefBureau)
        {
            return ResultatTraitementAnomalie.Echec(
                CodesTraitementAnomalie.AccesInterdit,
                "Seul un Chef de Bureau actif peut " +
                "traiter une anomalie.");
        }

        Anomalie? anomalie =
            await _dbContext.Anomalies
                .SingleOrDefaultAsync(
                    element => element.Id == anomalieId,
                    cancellationToken);

        if (anomalie is null)
        {
            return ResultatTraitementAnomalie.Echec(
                CodesTraitementAnomalie.AnomalieIntrouvable,
                "L'anomalie demandée est introuvable.");
        }

        if (anomalie.Statut != StatutAnomalie.Ouverte)
        {
            return ResultatTraitementAnomalie.Echec(
                CodesTraitementAnomalie.DejaTraitee,
                "Cette anomalie a déjà été traitée.");
        }

        DateTime maintenantUtc =
            _timeProvider.GetUtcNow().UtcDateTime;

        anomalie.Statut = StatutAnomalie.Traitee;
        anomalie.DateTraitementUtc = maintenantUtc;
        anomalie.TraiteeParId = chefBureauIdNormalise;
        anomalie.CommentaireTraitement =
            commentaireNormalise;

        try
        {
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            _logger.LogWarning(
                exception,
                "Conflit pendant le traitement de " +
                "l'anomalie {AnomalieId}.",
                anomalieId);

            return ResultatTraitementAnomalie.Echec(
                CodesTraitementAnomalie.Conflit,
                "L'anomalie vient d'être modifiée. " +
                "Rechargez la page avant de réessayer.");
        }

        _logger.LogInformation(
            "Anomalie traitée. Anomalie={AnomalieId}, " +
            "Demande={DemandeId}, ChefBureau={ChefBureauId}.",
            anomalie.Id,
            anomalie.DemandePublicationId,
            chefBureauIdNormalise);

        return ResultatTraitementAnomalie.Reussite(
            "L'anomalie a été clôturée et le traitement " +
            "a été historisé.");
    }

    private IQueryable<Anomalie> ConstruireRequete(
        StatutAnomalie statut,
        FiltreAnomalies filtre)
    {
        IQueryable<Anomalie> requete =
            _dbContext.Anomalies
                .AsNoTracking()
                .Where(anomalie =>
                    anomalie.Statut == statut);

        if (filtre.Type.HasValue)
        {
            TypeAnomalie type = filtre.Type.Value;

            requete = requete.Where(anomalie =>
                anomalie.Type == type);
        }

        if (filtre.LaboratoireId.HasValue)
        {
            int laboratoireId =
                filtre.LaboratoireId.Value;

            requete = requete.Where(anomalie =>
                anomalie.DemandePublication.Auteur
                    .LaboratoireId == laboratoireId);
        }

        return requete;
    }


    private static readonly
        Expression<Func<Anomalie, AnomalieDto>>
        Projection = anomalie => new AnomalieDto(
            anomalie.Id,
            anomalie.DemandePublicationId,
            anomalie.DemandePublication.Titre,
            anomalie.DemandePublication.Auteur.Prenom +
                " " +
                anomalie.DemandePublication.Auteur.Nom,
            anomalie.DemandePublication.Auteur.LaboratoireId,
            anomalie.DemandePublication.Auteur.Laboratoire == null
                ? "Non affecté"
                : anomalie.DemandePublication.Auteur
                    .Laboratoire.Nom,
            anomalie.Type,
            anomalie.Statut,
            anomalie.DemandePublication.StatutEditeurCourant,
            anomalie.DateDetectionUtc,
            anomalie.DureeJours,
            anomalie.Message,
            anomalie.DateTraitementUtc,
            anomalie.TraiteePar == null
                ? null
                : anomalie.TraiteePar.Prenom +
                    " " + anomalie.TraiteePar.Nom,
            anomalie.CommentaireTraitement);

    private async Task<bool> EstChefBureauActifAsync(
        string utilisateurId,
        CancellationToken cancellationToken)
    {
        return await (
            from utilisateur in
                _dbContext.Users.AsNoTracking()

            join utilisateurRole in
                _dbContext.UserRoles.AsNoTracking()
                on utilisateur.Id equals
                utilisateurRole.UserId

            join role in
                _dbContext.Roles.AsNoTracking()
                on utilisateurRole.RoleId equals role.Id

            where
                utilisateur.Id == utilisateurId &&
                utilisateur.EstActif &&
                role.NormalizedName ==
                    RoleChefBureauNormalise

            select utilisateur.Id)
            .AnyAsync(cancellationToken);
    }
}