using System.Security.Claims;

using GestionRecherche.Data;

using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace GestionRecherche.Web.Security;

public sealed class UtilisateurActifRequirement
    : IAuthorizationRequirement
{
}

public sealed class UtilisateurActifHandler
    : AuthorizationHandler<UtilisateurActifRequirement>
{
    private readonly AppDbContext _dbContext;

    public UtilisateurActifHandler(
        AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        UtilisateurActifRequirement requirement)
    {
        string? utilisateurId = context.User
            .FindFirst(ClaimTypes.NameIdentifier)
            ?.Value;

        if (string.IsNullOrWhiteSpace(utilisateurId))
        {
            return;
        }

        bool estActif = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(utilisateur =>
                utilisateur.Id == utilisateurId &&
                utilisateur.EstActif);

        if (estActif)
        {
            context.Succeed(requirement);
        }
    }
}