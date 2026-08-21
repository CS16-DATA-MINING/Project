using GestionRecherche.Data;
using Microsoft.EntityFrameworkCore;

namespace GestionRecherche.Business;

public sealed class RoutingService : IRoutingService
{
    private const string RoleChefBureau = "ChefBureau";

    private const string RoleSecretariatConseil =
        "SecretariatConseil";

    private readonly AppDbContext _dbContext;

    public RoutingService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ResultatRoutage> DeterminerRoutageAsync(
        int demandeId,
        CancellationToken cancellationToken = default)
    {
        if (demandeId <= 0)
        {
            return ResultatRoutage.Echec(
                CodesRoutage.DonneesInvalides,
                "L’identifiant de la demande est invalide.");
        }

        var demande =
            await _dbContext.DemandesPublication
                .AsNoTracking()
                .Where(demande => demande.Id == demandeId)
                .Select(demande => new
                {
                    demande.AuteurId,
                    demande.StatutValidationInterne
                })
                .SingleOrDefaultAsync(cancellationToken);

        if (demande is null)
        {
            return ResultatRoutage.Echec(
                CodesRoutage.DemandeIntrouvable,
                "La demande est introuvable.");
        }

        if (demande.StatutValidationInterne is
            StatutValidationInterne.Autorisee or
            StatutValidationInterne.Rejetee)
        {
            return ResultatRoutage.Echec(
                CodesRoutage.DemandeTerminee,
                "Cette demande est déjà terminée.");
        }

        NiveauValidation? niveauAttendu =
            ObtenirNiveauAttendu(
                demande.StatutValidationInterne);

        if (niveauAttendu is null)
        {
            return ResultatRoutage.Echec(
                CodesRoutage.StatutInvalide,
                "Le statut actuel de la demande ne correspond " +
                "à aucun niveau de validation.");
        }

        var auteur =
            await _dbContext.Users
                .AsNoTracking()
                .Where(user => user.Id == demande.AuteurId)
                .Select(user => new
                {
                    user.Id,
                    user.LaboratoireId,
                    user.UerId
                })
                .SingleOrDefaultAsync(cancellationToken);

        if (auteur is null)
        {
            return ResultatRoutage.Echec(
                CodesRoutage.AuteurIntrouvable,
                "L’auteur associé à la demande est introuvable.",
                niveauAttendu);
        }

        switch (niveauAttendu.Value)
        {
            case NiveauValidation.Labo:
                return await DeterminerChefLaboratoireAsync(
                    auteur.LaboratoireId,
                    cancellationToken);

            case NiveauValidation.Uer:
                return await DeterminerChefUerAsync(
                    auteur.UerId,
                    cancellationToken);

            case NiveauValidation.Bureau:
                return await DeterminerValidateurGlobalAsync(
                    NiveauValidation.Bureau,
                    RoleChefBureau,
                    CodesRoutage.RoleChefBureauManquant,
                    cancellationToken);

            case NiveauValidation.Conseil:
                return await DeterminerValidateurGlobalAsync(
                    NiveauValidation.Conseil,
                    RoleSecretariatConseil,
                    CodesRoutage.RoleSecretariatConseilManquant,
                    cancellationToken);

            default:
                return ResultatRoutage.Echec(
                    CodesRoutage.StatutInvalide,
                    "Le niveau de validation calculé est invalide.");
        }
    }

    private async Task<ResultatRoutage>
        DeterminerChefLaboratoireAsync(
            int? laboratoireId,
            CancellationToken cancellationToken)
    {
        if (laboratoireId is null)
        {
            return ResultatRoutage.Echec(
                CodesRoutage.AuteurSansLaboratoire,
                "L’auteur n’est affecté à aucun laboratoire.",
                NiveauValidation.Labo);
        }

        var laboratoire =
            await _dbContext.Laboratoires
                .AsNoTracking()
                .Where(laboratoire =>
                    laboratoire.Id == laboratoireId.Value)
                .Select(laboratoire => new
                {
                    laboratoire.Id,
                    laboratoire.Nom,
                    laboratoire.ChefLaboId,
                    laboratoire.EstActif
                })
                .SingleOrDefaultAsync(cancellationToken);

        if (laboratoire is null || !laboratoire.EstActif)
        {
            return ResultatRoutage.Echec(
                CodesRoutage.LaboratoireIntrouvable,
                "Le laboratoire de l’auteur est introuvable " +
                "ou inactif.",
                NiveauValidation.Labo);
        }

        if (string.IsNullOrWhiteSpace(
                laboratoire.ChefLaboId))
        {
            return ResultatRoutage.Echec(
                CodesRoutage.LaboratoireSansChef,
                $"Le laboratoire « {laboratoire.Nom} » " +
                "ne possède aucun Chef de Laboratoire.",
                NiveauValidation.Labo);
        }

        ValidateurRoutage? validateur =
            await ObtenirUtilisateurActifParIdAsync(
                laboratoire.ChefLaboId,
                cancellationToken);

        if (validateur is null)
        {
            return ResultatRoutage.Echec(
                CodesRoutage.LaboratoireSansChef,
                $"Le Chef du laboratoire " +
                $"« {laboratoire.Nom} » est introuvable " +
                "ou inactif.",
                NiveauValidation.Labo);
        }

        return ResultatRoutage.Reussite(
            NiveauValidation.Labo,
            validateur.Id,
            validateur.NomComplet,
            $"La demande doit être traitée par " +
            $"{validateur.NomComplet}, Chef du laboratoire " +
            $"« {laboratoire.Nom} ».");
    }

    private async Task<ResultatRoutage>
        DeterminerChefUerAsync(
            int? uerId,
            CancellationToken cancellationToken)
    {
        if (uerId is null)
        {
            return ResultatRoutage.Echec(
                CodesRoutage.AuteurSansUer,
                "L’auteur n’est affecté à aucune UER.",
                NiveauValidation.Uer);
        }

        var uer =
            await _dbContext.Uers
                .AsNoTracking()
                .Where(uer => uer.Id == uerId.Value)
                .Select(uer => new
                {
                    uer.Id,
                    uer.Nom,
                    uer.ChefUerId,
                    uer.EstActive
                })
                .SingleOrDefaultAsync(cancellationToken);

        if (uer is null || !uer.EstActive)
        {
            return ResultatRoutage.Echec(
                CodesRoutage.UerIntrouvable,
                "L’UER de l’auteur est introuvable ou inactive.",
                NiveauValidation.Uer);
        }

        if (string.IsNullOrWhiteSpace(uer.ChefUerId))
        {
            return ResultatRoutage.Echec(
                CodesRoutage.UerSansChef,
                $"L’UER « {uer.Nom} » ne possède aucun Chef UER.",
                NiveauValidation.Uer);
        }

        ValidateurRoutage? validateur =
            await ObtenirUtilisateurActifParIdAsync(
                uer.ChefUerId,
                cancellationToken);

        if (validateur is null)
        {
            return ResultatRoutage.Echec(
                CodesRoutage.UerSansChef,
                $"Le Chef de l’UER « {uer.Nom} » " +
                "est introuvable ou inactif.",
                NiveauValidation.Uer);
        }

        return ResultatRoutage.Reussite(
            NiveauValidation.Uer,
            validateur.Id,
            validateur.NomComplet,
            $"La demande doit être traitée par " +
            $"{validateur.NomComplet}, Chef de l’UER " +
            $"« {uer.Nom} ».");
    }

    private async Task<ResultatRoutage>
        DeterminerValidateurGlobalAsync(
            NiveauValidation niveau,
            string role,
            string codeSiManquant,
            CancellationToken cancellationToken)
    {
        ValidateurRoutage? validateur =
            await ObtenirUtilisateurActifParRoleAsync(
                role,
                cancellationToken);

        if (validateur is null)
        {
            return ResultatRoutage.Echec(
                codeSiManquant,
                $"Aucun utilisateur actif ne possède " +
                $"le rôle « {role} ».",
                niveau);
        }

        return ResultatRoutage.Reussite(
            niveau,
            validateur.Id,
            validateur.NomComplet,
            $"La demande doit être traitée par " +
            $"{validateur.NomComplet}, rôle « {role} ».");
    }

    private async Task<ValidateurRoutage?>
        ObtenirUtilisateurActifParIdAsync(
            string utilisateurId,
            CancellationToken cancellationToken)
    {
        var utilisateur =
            await _dbContext.Users
                .AsNoTracking()
                .Where(user =>
                    user.Id == utilisateurId &&
                    user.EstActif)
                .Select(user => new
                {
                    user.Id,
                    user.Prenom,
                    user.Nom,
                    user.Email
                })
                .SingleOrDefaultAsync(cancellationToken);

        if (utilisateur is null)
        {
            return null;
        }

        return new ValidateurRoutage(
            utilisateur.Id,
            ConstruireNomComplet(
                utilisateur.Prenom,
                utilisateur.Nom,
                utilisateur.Email,
                utilisateur.Id));
    }

    private async Task<ValidateurRoutage?>
        ObtenirUtilisateurActifParRoleAsync(
            string role,
            CancellationToken cancellationToken)
    {
        string roleNormalise = role.ToUpperInvariant();

        var utilisateur =
            await (
                from user in
                    _dbContext.Users.AsNoTracking()

                join userRole in
                    _dbContext.UserRoles.AsNoTracking()
                    on user.Id equals userRole.UserId

                join identityRole in
                    _dbContext.Roles.AsNoTracking()
                    on userRole.RoleId equals identityRole.Id

                where
                    identityRole.NormalizedName ==
                        roleNormalise &&
                    user.EstActif

                orderby user.Id

                select new
                {
                    user.Id,
                    user.Prenom,
                    user.Nom,
                    user.Email
                })
                .FirstOrDefaultAsync(cancellationToken);

        if (utilisateur is null)
        {
            return null;
        }

        return new ValidateurRoutage(
            utilisateur.Id,
            ConstruireNomComplet(
                utilisateur.Prenom,
                utilisateur.Nom,
                utilisateur.Email,
                utilisateur.Id));
    }

    private static NiveauValidation? ObtenirNiveauAttendu(
        StatutValidationInterne statut)
    {
        return statut switch
        {
            StatutValidationInterne.EnAttenteLabo =>
                NiveauValidation.Labo,

            StatutValidationInterne.EnAttenteUer =>
                NiveauValidation.Uer,

            StatutValidationInterne.EnAttenteBureau =>
                NiveauValidation.Bureau,

            StatutValidationInterne.EnAttenteConseil =>
                NiveauValidation.Conseil,

            StatutValidationInterne.Autorisee => null,

            StatutValidationInterne.Rejetee => null,

            _ => null
        };
    }

    private static string ConstruireNomComplet(
        string prenom,
        string nom,
        string? email,
        string utilisateurId)
    {
        string nomComplet =
            $"{prenom.Trim()} {nom.Trim()}".Trim();

        if (!string.IsNullOrWhiteSpace(nomComplet))
        {
            return nomComplet;
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            return email;
        }

        return utilisateurId;
    }

    private sealed record ValidateurRoutage(
        string Id,
        string NomComplet);
}
