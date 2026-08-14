using System;
using GestionRecherche.Data;
using Microsoft.AspNetCore.Identity;

namespace GestionRecherche.Web;

public class IdentitySeeder
{

private static readonly string[] Roles =
    {
        "Chercheur",
        "ChefLabo",
        "ChefUER",
        "ChefBureau",
        "SecretariatConseil",
        "Admin"
    };

public static async Task SeedAsync( IServiceProvider services,
        IConfiguration configuration,
        bool seedDevelopmentUser)
    {
        using var scope = services.CreateScope();

        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var roleManager =  scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();



         foreach (var roleName in Roles)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var roleResult =
                await roleManager.CreateAsync(new IdentityRole(roleName));

            EnsureSuccess(
                roleResult,
                $"Création du rôle {roleName}");
        }


if (!seedDevelopmentUser)
        {
            return;
        }

        var email = configuration["SeedAdmin:Email"];
        var password = configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Les secrets SeedAdmin:Email et SeedAdmin:Password sont obligatoires en développement.");
        }

        var admin = await userManager.FindByEmailAsync(email);

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Nom = configuration["SeedAdmin:Nom"] ?? "Administrateur",
                Prenom = configuration["SeedAdmin:Prenom"] ?? "Local"
            };

            var creationResult =
                await userManager.CreateAsync(admin, password);

            EnsureSuccess(
                creationResult,
                "Création du compte administrateur");
        }

        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            var roleResult =
                await userManager.AddToRoleAsync(admin, "Admin");

            EnsureSuccess(
                roleResult,
                "Affectation du rôle Admin");
        }
    }

private static void EnsureSuccess(
        IdentityResult result,
        string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(
            " | ",
            result.Errors.Select(error =>
                $"{error.Code}: {error.Description}"));

        throw new InvalidOperationException(
            $"{operation} a échoué : {errors}");




    }
}  













