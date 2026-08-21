using GestionRecherche.Business;
using GestionRecherche.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GestionRecherche.Web;

public static class Jour5TestDataSeeder
{
    private const string MotDePasseTest =
        "Test1234!";

    public static async Task SeedAsync(
        IServiceProvider services)
    {
        using IServiceScope scope =
            services.CreateScope();

        AppDbContext dbContext =
            scope.ServiceProvider
                .GetRequiredService<AppDbContext>();

        UserManager<ApplicationUser> userManager =
            scope.ServiceProvider
                .GetRequiredService<
                    UserManager<ApplicationUser>>();

        RoleManager<IdentityRole> roleManager =
            scope.ServiceProvider
                .GetRequiredService<
                    RoleManager<IdentityRole>>();

        /*
         * Ton IdentitySeeder crée déjà normalement
         * un administrateur.
         *
         * Nous utilisons temporairement ce compte
         * pour créer l'ـUER et les laboratoires.
         */
        ApplicationUser utilisateurTechnique =
            await userManager.Users
                .OrderBy(utilisateur =>
                    utilisateur.Id)
                .FirstOrDefaultAsync()
            ?? throw new InvalidOperationException(
                "Aucun utilisateur n'existe. "
                + "Exécute d'abord IdentitySeeder.");

        // 1. Créer le rôle ChefLabo.
        await AssurerRoleAsync(
            roleManager,
            "ChefLabo");

        /*
         * Si le rôle Chercheur existe déjà,
         * nous le donnerons aux auteurs.
         *
         * S'il n'existe pas, les auteurs seront
         * créés sans rôle. Cela ne bloque pas
         * les tests du Chef de Labo.
         */
    
        string? roleAuteur =
            await roleManager.RoleExistsAsync(
                "Chercheur")
                ? "Chercheur"
                : null;

        // 2. Créer ou récupérer l'ـUER.
        Uer? uer =
            await dbContext.Uers
                .SingleOrDefaultAsync(
                    element =>
                        element.Code ==
                        "UER-TEST");

        if (uer is null)
        {
            uer = new Uer
            {
                Code = "UER-TEST",
                Nom = "UER de test",

                ChefUerId =
                    utilisateurTechnique.Id,

                EstActive = true
            };

            dbContext.Uers.Add(uer);

            await dbContext.SaveChangesAsync();

            Console.WriteLine(
                "UER-TEST créée.");
        }
        else
        {
            uer.Nom = "UER de test";
            uer.EstActive = true;

            await dbContext.SaveChangesAsync();
        }

        /*
         * 3. Créer les laboratoires.
         *
         * Au début, le Chef est temporairement
         * l'utilisateur technique.
         *
         * Après la création de Chef A et Chef B,
         * nous remplacerons ces identifiants.
         */

        Laboratoire? laboratoireA =
            await dbContext.Laboratoires
                .SingleOrDefaultAsync(
                    element =>
                        element.Code == "LAB-A");

        if (laboratoireA is null)
        {
            laboratoireA = new Laboratoire
            {
                Code = "LAB-A",
                Nom = "Laboratoire A",
                UerId = uer.Id,

                ChefLaboId =
                    utilisateurTechnique.Id,

                EstActif = true
            };

            dbContext.Laboratoires.Add(
                laboratoireA);
        }
        else
        {
            laboratoireA.Nom =
                "Laboratoire A";

            laboratoireA.UerId =
                uer.Id;

            laboratoireA.EstActif = true;
        }

        Laboratoire? laboratoireB =
            await dbContext.Laboratoires
                .SingleOrDefaultAsync(
                    element =>
                        element.Code == "LAB-B");

        if (laboratoireB is null)
        {
            laboratoireB = new Laboratoire
            {
                Code = "LAB-B",
                Nom = "Laboratoire B",
                UerId = uer.Id,

                ChefLaboId =
                    utilisateurTechnique.Id,

                EstActif = true
            };

            dbContext.Laboratoires.Add(
                laboratoireB);
        }
        else
        {
            laboratoireB.Nom =
                "Laboratoire B";

            laboratoireB.UerId =
                uer.Id;

            laboratoireB.EstActif = true;
        }

        await dbContext.SaveChangesAsync();

        /*
         * À ce moment, les laboratoires ont
         * reçu leurs véritables identifiants.
         */

        // 4. Créer Chef A.
        ApplicationUser chefA =
            await CreerOuMettreAJourUtilisateurAsync(
                userManager,
                email: "chef.a@test.local",
                motDePasse: MotDePasseTest,
                prenom: "Chef",
                nom: "A",
                laboratoireId:
                    laboratoireA.Id,
                uerId: uer.Id,
                role: "ChefLabo");

        // 5. Créer Chef B.
        ApplicationUser chefB =
            await CreerOuMettreAJourUtilisateurAsync(
                userManager,
                email: "chef.b@test.local",
                motDePasse: MotDePasseTest,
                prenom: "Chef",
                nom: "B",
                laboratoireId:
                    laboratoireB.Id,
                uerId: uer.Id,
                role: "ChefLabo");

        // 6. Créer Auteur A.
        ApplicationUser auteurA =
            await CreerOuMettreAJourUtilisateurAsync(
                userManager,
                email: "auteur.a@test.local",
                motDePasse: MotDePasseTest,
                prenom: "Auteur",
                nom: "A",
                laboratoireId:
                    laboratoireA.Id,
                uerId: uer.Id,
                role: roleAuteur);

        // 7. Créer Auteur B.
        ApplicationUser auteurB =
            await CreerOuMettreAJourUtilisateurAsync(
                userManager,
                email: "auteur.b@test.local",
                motDePasse: MotDePasseTest,
                prenom: "Auteur",
                nom: "B",
                laboratoireId:
                    laboratoireB.Id,
                uerId: uer.Id,
                role: roleAuteur);

        // 8. Affecter les vrais Chefs.
        laboratoireA.ChefLaboId =
            chefA.Id;

        laboratoireB.ChefLaboId =
            chefB.Id;

        await dbContext.SaveChangesAsync();

        /*
         * 9. Créer la demande A.
         *
         * Si elle existe déjà, nous ne la
         * recréons pas.
         */
        DemandePublication? demandeA =
            await dbContext.DemandesPublication
                .FirstOrDefaultAsync(
                    demande =>
                        demande.Titre ==
                        "[TEST JOUR 5] Article Labo A");

        if (demandeA is null)
        {
            demandeA =
                new DemandePublication
                {
                    AuteurId = auteurA.Id,

                    Titre =
                        "[TEST JOUR 5] "
                        + "Article Labo A",

                    Abstract =
                        "Résumé scientifique "
                        + "de la demande A.",

                    StatutValidationInterne =
                        StatutValidationInterne
                            .EnAttenteLabo,

                    NumeroCycleValidation = 1,

                    DateCreationUtc =
                        DateTime.UtcNow
                            .AddMinutes(-10)
                };

            dbContext.DemandesPublication.Add(
                demandeA);
        }

        // 10. Créer la demande B.
        DemandePublication? demandeB =
            await dbContext.DemandesPublication
                .FirstOrDefaultAsync(
                    demande =>
                        demande.Titre ==
                        "[TEST JOUR 5] Article Labo B");

        if (demandeB is null)
        {
            demandeB =
                new DemandePublication
                {
                    AuteurId = auteurB.Id,

                    Titre =
                        "[TEST JOUR 5] "
                        + "Article Labo B",

                    Abstract =
                        "Résumé scientifique "
                        + "de la demande B.",

                    StatutValidationInterne =
                        StatutValidationInterne
                            .EnAttenteLabo,

                    NumeroCycleValidation = 1,

                    DateCreationUtc =
                        DateTime.UtcNow
                };

            dbContext.DemandesPublication.Add(
                demandeB);
        }

        await dbContext.SaveChangesAsync();

        Console.WriteLine(
            "--------------------------------");

        Console.WriteLine(
            "Données du Jour 5 créées.");

        Console.WriteLine(
            $"Demande A : ID = {demandeA.Id}");

        Console.WriteLine(
            $"Demande B : ID = {demandeB.Id}");

        Console.WriteLine(
            "Chef A : chef.a@test.local");

        Console.WriteLine(
            "Chef B : chef.b@test.local");

        Console.WriteLine(
            $"Mot de passe : {MotDePasseTest}");

        Console.WriteLine(
            "--------------------------------");
    }

    private static async Task AssurerRoleAsync(
        RoleManager<IdentityRole> roleManager,
        string nomRole)
    {
        if (await roleManager.RoleExistsAsync(
                nomRole))
        {
            return;
        }

        IdentityResult resultat =
            await roleManager.CreateAsync(
                new IdentityRole(nomRole));

        VerifierIdentity(
            resultat,
            $"Création du rôle {nomRole}");
    }

    private static async Task<ApplicationUser>
        CreerOuMettreAJourUtilisateurAsync(
            UserManager<ApplicationUser> userManager,
            string email,
            string motDePasse,
            string prenom,
            string nom,
            int laboratoireId,
            int uerId,
            string? role)
    {
        ApplicationUser? utilisateur =
            await userManager.FindByEmailAsync(
                email);

        IdentityResult resultat;

        if (utilisateur is null)
        {
            utilisateur =
                new ApplicationUser
                {
                    UserName = email,
                    Email = email,

                    EmailConfirmed = true,

                    Prenom = prenom,
                    Nom = nom,

                    LaboratoireId =
                        laboratoireId,

                    UerId = uerId,

                    EstActif = true
                };

            resultat =
                await userManager.CreateAsync(
                    utilisateur,
                    motDePasse);

            VerifierIdentity(
                resultat,
                $"Création du compte {email}");
        }
        else
        {
            utilisateur.UserName = email;
            utilisateur.Email = email;
            utilisateur.EmailConfirmed = true;

            utilisateur.Prenom = prenom;
            utilisateur.Nom = nom;

            utilisateur.LaboratoireId =
                laboratoireId;

            utilisateur.UerId = uerId;

            utilisateur.EstActif = true;

            resultat =
                await userManager.UpdateAsync(
                    utilisateur);

            VerifierIdentity(
                resultat,
                $"Mise à jour du compte {email}");
        }

        if (!string.IsNullOrWhiteSpace(role) &&
            !await userManager.IsInRoleAsync(
                utilisateur,
                role))
        {
            resultat =
                await userManager.AddToRoleAsync(
                    utilisateur,
                    role);

            VerifierIdentity(
                resultat,
                $"Ajout du rôle {role} à {email}");
        }

        Console.WriteLine(
            $"Compte prêt : {email}");

        return utilisateur;
    }

    private static void VerifierIdentity(
        IdentityResult resultat,
        string operation)
    {
        if (resultat.Succeeded)
        {
            return;
        }

        string erreurs =
            string.Join(
                Environment.NewLine,
                resultat.Errors.Select(
                    erreur =>
                        erreur.Code
                        + " : "
                        + erreur.Description));

        throw new InvalidOperationException(
            operation
            + " impossible."
            + Environment.NewLine
            + erreurs);
    }
}