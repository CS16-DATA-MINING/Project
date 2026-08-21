using GestionRecherche.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GestionRecherche.Business.Tests;

/*
 * JOUR 9 : filet de sécurité du circuit de validation.
 * Cas normaux, rejets, résoumission et accès interdits.
 */
public sealed class ValidationJour9Tests
{
    /* Une base EN MÉMOIRE, neuve et isolée pour CHAQUE test :
     * le Guid dans le nom empêche deux tests de se polluer. */
    private static AppDbContext CreerContexte()
    {
        var options =
            new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase($"jour9-{Guid.NewGuid()}")
                .Options;

        return new AppDbContext(options);
    }

    /* Horloge truquée : NOUS décidons de l'heure. Les dates
     * deviennent prévisibles, donc le tri est testable. */
    private sealed class HorlogeReglable : TimeProvider
    {
        private DateTimeOffset _instant;

        public HorlogeReglable(DateTimeOffset instant)
            => _instant = instant;

        public override DateTimeOffset GetUtcNow() => _instant;

        public void Avancer(TimeSpan duree)
            => _instant = _instant.Add(duree);
    }

    private static ApplicationUser CreerUtilisateur(
        string id,
        string prenom,
        string nom,
        int? laboratoireId = null,
        int? uerId = null)
    {
        string email = $"{id}@test.local";

        return new ApplicationUser
        {
            Id = id,
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            Prenom = prenom,
            Nom = nom,
            LaboratoireId = laboratoireId,
            UerId = uerId,
            EstActif = true
        };
    }

    private static DemandePublication CreerDemande(
        string auteurId,
        StatutValidationInterne statut)
    {
        return new DemandePublication
        {
            AuteurId = auteurId,
            Titre = "Article de test Jour 9",
            Abstract = "Résumé scientifique de test.",
            StatutValidationInterne = statut,
            NumeroCycleValidation = 1,
            DateCreationUtc = DateTime.UtcNow
        };
    }

    /* Identity range les rôles dans 2 tables : le rôle,
     * puis le lien utilisateur↔rôle. On simule les deux. */
    private static void DonnerRole(
        AppDbContext dbContext,
        string utilisateurId,
        string nomRole)
    {
        var role = new IdentityRole
        {
            Id = $"role-{nomRole}-{utilisateurId}",
            Name = nomRole,
            NormalizedName = nomRole.ToUpperInvariant()
        };

        dbContext.Roles.Add(role);

        dbContext.UserRoles.Add(new IdentityUserRole<string>
        {
            UserId = utilisateurId,
            RoleId = role.Id
        });
    }

    /* LE MONDE : 1 UER, 2 labos et leurs chefs, un bureau,
     * un conseil, un auteur au labo A — plus une demande. */
    private static async Task<DemandePublication>
        CreerMondeAvecDemandeAsync(
            AppDbContext dbContext,
            StatutValidationInterne statutDemande)
    {
        dbContext.Uers.Add(new Uer
        {
            Id = 1,
            Code = "UER-1",
            Nom = "UER de test",
            ChefUerId = "chef-uer",
            EstActive = true
        });

        dbContext.Laboratoires.AddRange(
            new Laboratoire
            {
                Id = 1, Code = "LAB-A", Nom = "Labo A",
                UerId = 1, ChefLaboId = "chef-a", EstActif = true
            },
            new Laboratoire
            {
                Id = 2, Code = "LAB-B", Nom = "Labo B",
                UerId = 1, ChefLaboId = "chef-b", EstActif = true
            });

        dbContext.Users.AddRange(
            CreerUtilisateur("chef-a", "Karim", "Benali", 1, 1),
            CreerUtilisateur("chef-b", "Lina", "Mansouri", 2, 1),
            CreerUtilisateur("chef-uer", "Salima", "Cherif", uerId: 1),
            CreerUtilisateur("chef-bureau", "Yacine", "Belkacem"),
            CreerUtilisateur("secretaire", "Nour", "Haddad"),
            CreerUtilisateur("auteur-a", "Amine", "Saidi", 1, 1));

        DonnerRole(dbContext, "chef-bureau", "ChefBureau");
        DonnerRole(dbContext, "secretaire", "SecretariatConseil");

        DemandePublication demande =
            CreerDemande("auteur-a", statutDemande);

        dbContext.DemandesPublication.Add(demande);

        await dbContext.SaveChangesAsync();

        return demande;
    }
        [Fact]
    public async Task CircuitComplet_HistoriqueContientQuatreLignesTriees()
    {
        // ---------- ARRANGE : le monde + une horloge truquée ----------
        await using AppDbContext dbContext = CreerContexte();

        DemandePublication demande =
            await CreerMondeAvecDemandeAsync(
                dbContext, StatutValidationInterne.EnAttenteLabo);

        var horloge = new HorlogeReglable(
            new DateTimeOffset(2026, 8, 22, 9, 0, 0, TimeSpan.Zero));

        var service = new ValidationService(dbContext, horloge);

        // ---------- ACT : les 4 validations, par les 4 BONNES personnes ----------
        var resultats = new List<ResultatValidation>();

        resultats.Add(await service.ValiderAuNiveauAsync(
            demande.Id, "chef-a", NiveauValidation.Labo, "OK labo"));
        horloge.Avancer(TimeSpan.FromMinutes(10));

        resultats.Add(await service.ValiderAuNiveauAsync(
            demande.Id, "chef-uer", NiveauValidation.Uer, "OK UER"));
        horloge.Avancer(TimeSpan.FromMinutes(10));

        resultats.Add(await service.ValiderAuNiveauAsync(
            demande.Id, "chef-bureau", NiveauValidation.Bureau, "OK bureau"));
        horloge.Avancer(TimeSpan.FromMinutes(10));

        resultats.Add(await service.ValiderAuNiveauAsync(
            demande.Id, "secretaire", NiveauValidation.Conseil, "OK conseil"));

        // ---------- ASSERT ----------
        // Chaque étape a réussi (sinon xUnit affiche le message d'échec).
        Assert.All(resultats,
            resultat => Assert.True(resultat.EstReussi, resultat.Message));

        Assert.Equal(StatutValidationInterne.Autorisee,
            demande.StatutValidationInterne);

        // L'historique du Jour 8 raconte toute l'histoire, dans l'ordre.
        IReadOnlyList<EtapeHistoriqueDto> historique =
            await service.ObtenirHistoriqueAsync(demande.Id);

        Assert.Equal(4, historique.Count);

        Assert.Equal(
            new[]
            {
                NiveauValidation.Labo, NiveauValidation.Uer,
                NiveauValidation.Bureau, NiveauValidation.Conseil
            },
            historique.Select(etape => etape.Niveau).ToArray());

        Assert.All(historique, etape =>
            Assert.Equal(DecisionValidation.Validee, etape.Decision));

        // Le JOIN vers l'utilisateur fonctionne : nom complet résolu.
        Assert.Equal("Karim Benali",
            historique[0].ValidateurNomComplet);
    }

        [Theory]
    [InlineData(StatutValidationInterne.EnAttenteLabo, NiveauValidation.Labo)]
    [InlineData(StatutValidationInterne.EnAttenteUer, NiveauValidation.Uer)]
    [InlineData(StatutValidationInterne.EnAttenteBureau, NiveauValidation.Bureau)]
    [InlineData(StatutValidationInterne.EnAttenteConseil, NiveauValidation.Conseil)]
    public async Task RejeterAsync_AChaqueNiveau_PasseLaDemandeEnRejetee(
        StatutValidationInterne statutDepart,
        NiveauValidation niveau)
    {
        await using AppDbContext dbContext = CreerContexte();

        DemandePublication demande =
            CreerDemande("auteur-a", statutDepart);

        dbContext.DemandesPublication.Add(demande);
        await dbContext.SaveChangesAsync();

        var service = new ValidationService(
            dbContext, TimeProvider.System);

        ResultatValidation resultat =
            await service.RejeterAsync(
                demande.Id,
                "validateur-1",
                niveau,
                $"Rejet de test au niveau {niveau}.");

        Assert.True(resultat.EstReussi, resultat.Message);

        // Un rejet à N'IMPORTE quel niveau => Rejetée.
        Assert.Equal(StatutValidationInterne.Rejetee,
            demande.StatutValidationInterne);

        // Et la trace est écrite avec le motif.
        EtapeValidation etape =
            await dbContext.EtapesValidation.SingleAsync();

        Assert.Equal(niveau, etape.Niveau);
        Assert.Equal(DecisionValidation.Rejetee, etape.Decision);
        Assert.False(string.IsNullOrWhiteSpace(etape.Commentaire));
    }
        [Fact]
    public async Task Resoumettre_ApresRejet_OuvreLeCycleDeux()
    {
        await using AppDbContext dbContext = CreerContexte();

        DemandePublication demande =
            CreerDemande("auteur-a", StatutValidationInterne.Rejetee);

        dbContext.DemandesPublication.Add(demande);
        await dbContext.SaveChangesAsync();

        var service = new ValidationService(
            dbContext, TimeProvider.System);

        ResultatValidation resultat =
            await service.ResoumettreApresRejetAsync(
                demande.Id, "auteur-a");

        Assert.True(resultat.EstReussi, resultat.Message);
        Assert.Equal(2, demande.NumeroCycleValidation);
        Assert.Equal(StatutValidationInterne.EnAttenteLabo,
            demande.StatutValidationInterne);
    }

    [Fact]
    public async Task Resoumettre_DemandeNonRejetee_EstRefuse()
    {
        await using AppDbContext dbContext = CreerContexte();

        // Demande encore EN COURS : résoumettre n'a pas de sens.
        DemandePublication demande =
            CreerDemande("auteur-a", StatutValidationInterne.EnAttenteUer);

        dbContext.DemandesPublication.Add(demande);
        await dbContext.SaveChangesAsync();

        var service = new ValidationService(
            dbContext, TimeProvider.System);

        ResultatValidation resultat =
            await service.ResoumettreApresRejetAsync(
                demande.Id, "auteur-a");

        Assert.False(resultat.EstReussi);
        Assert.Equal(CodesValidation.ResoumissionImpossible,
            resultat.Code);

        // RIEN n'a bougé : ni le cycle, ni le statut.
        Assert.Equal(1, demande.NumeroCycleValidation);
        Assert.Equal(StatutValidationInterne.EnAttenteUer,
            demande.StatutValidationInterne);
    }

    [Fact]
    public async Task Resoumettre_ParUnAutreQueLAuteur_EstRefuse()
    {
        await using AppDbContext dbContext = CreerContexte();

        DemandePublication demande =
            CreerDemande("auteur-a", StatutValidationInterne.Rejetee);

        dbContext.DemandesPublication.Add(demande);
        await dbContext.SaveChangesAsync();

        var service = new ValidationService(
            dbContext, TimeProvider.System);

        ResultatValidation resultat =
            await service.ResoumettreApresRejetAsync(
                demande.Id, "un-intrus");

        Assert.False(resultat.EstReussi);
        Assert.Equal(CodesValidation.AccesInterdit, resultat.Code);
        Assert.Equal(1, demande.NumeroCycleValidation);
    }

    /* LE scénario en or : rejet -> résoumission -> le Labo
     * peut REdécider au cycle 2 sans être bloqué par
     * l'anti-double-décision, et l'histoire garde les 2 cycles. */
    [Fact]
    public async Task ApresResoumission_LeLaboRedecideAuCycleDeux()
    {
        await using AppDbContext dbContext = CreerContexte();

        DemandePublication demande =
            CreerDemande("auteur-a", StatutValidationInterne.EnAttenteLabo);

        dbContext.DemandesPublication.Add(demande);
        await dbContext.SaveChangesAsync();

        var service = new ValidationService(
            dbContext, TimeProvider.System);

        // Cycle 1 : rejet au Labo.
        await service.RejeterAsync(
            demande.Id, "chef-a", NiveauValidation.Labo,
            "Résumé incomplet.");

        // L'auteur corrige puis résoumet.
        await service.ResoumettreApresRejetAsync(
            demande.Id, "auteur-a");

        // Cycle 2 : le MÊME chef décide à nouveau au MÊME niveau.
        ResultatValidation deuxiemeDecision =
            await service.ValiderAsync(
                demande.Id, "chef-a", NiveauValidation.Labo,
                "OK après correction.");

        Assert.True(deuxiemeDecision.EstReussi,
            deuxiemeDecision.Message);

        // L'historique garde TOUT : 1 ligne par cycle.
        List<EtapeValidation> etapes =
            await dbContext.EtapesValidation
                .OrderBy(etape => etape.NumeroCycleValidation)
                .ToListAsync();

        Assert.Equal(2, etapes.Count);
        Assert.Equal(1, etapes[0].NumeroCycleValidation);
        Assert.Equal(DecisionValidation.Rejetee, etapes[0].Decision);
        Assert.Equal(2, etapes[1].NumeroCycleValidation);
        Assert.Equal(DecisionValidation.Validee, etapes[1].Decision);
    }

        [Fact]
    public async Task ValiderAuNiveau_ChefDUnAutreLabo_EstBloque()
    {
        await using AppDbContext dbContext = CreerContexte();

        DemandePublication demande =
            await CreerMondeAvecDemandeAsync(
                dbContext, StatutValidationInterne.EnAttenteLabo);

        var service = new ValidationService(
            dbContext, TimeProvider.System);

        // chef-b dirige le labo B ; l'auteur est au labo A.
        ResultatValidation resultat =
            await service.ValiderAuNiveauAsync(
                demande.Id, "chef-b", NiveauValidation.Labo, null);

        Assert.False(resultat.EstReussi);
        Assert.Equal(CodesValidation.AccesInterdit, resultat.Code);

        // Preuve que le refus est TOTAL : statut intact, zéro trace.
        Assert.Equal(StatutValidationInterne.EnAttenteLabo,
            demande.StatutValidationInterne);
        Assert.Empty(await dbContext.EtapesValidation.ToListAsync());
    }

    [Fact]
    public async Task ValiderAuNiveau_SansLeRoleBureau_EstBloque()
    {
        await using AppDbContext dbContext = CreerContexte();

        DemandePublication demande =
            await CreerMondeAvecDemandeAsync(
                dbContext, StatutValidationInterne.EnAttenteBureau);

        var service = new ValidationService(
            dbContext, TimeProvider.System);

        // auteur-a n'a AUCUN rôle de validation : mauvais rôle.
        ResultatValidation resultat =
            await service.ValiderAuNiveauAsync(
                demande.Id, "auteur-a", NiveauValidation.Bureau, null);

        Assert.False(resultat.EstReussi);
        Assert.Equal(CodesValidation.AccesInterdit, resultat.Code);
    }

    [Fact]
    public async Task ValiderAuNiveau_UtilisateurInactif_EstBloque()
    {
        await using AppDbContext dbContext = CreerContexte();

        DemandePublication demande =
            await CreerMondeAvecDemandeAsync(
                dbContext, StatutValidationInterne.EnAttenteConseil);

        // On désactive le compte du secrétariat…
        ApplicationUser secretaire =
            await dbContext.Users.SingleAsync(
                utilisateur => utilisateur.Id == "secretaire");

        secretaire.EstActif = false;
        await dbContext.SaveChangesAsync();

        var service = new ValidationService(
            dbContext, TimeProvider.System);

        // …le rôle ne suffit plus : compte inactif = dehors.
        ResultatValidation resultat =
            await service.ValiderAuNiveauAsync(
                demande.Id, "secretaire", NiveauValidation.Conseil, null);

        Assert.False(resultat.EstReussi);
        Assert.Equal(CodesValidation.AccesInterdit, resultat.Code);
    }

    [Fact]
    public async Task ValiderAuNiveau_ChefLaboManquant_EstBloque()
    {
        await using AppDbContext dbContext = CreerContexte();

        DemandePublication demande =
            await CreerMondeAvecDemandeAsync(
                dbContext, StatutValidationInterne.EnAttenteLabo);

        // Le labo A perd son chef (départ, non remplacé).
        Laboratoire laboA =
            await dbContext.Laboratoires.SingleAsync(
                laboratoire => laboratoire.Id == 1);

        laboA.ChefLaboId = null;
        await dbContext.SaveChangesAsync();

        var service = new ValidationService(
            dbContext, TimeProvider.System);

        // La demande n'apparaît dans la file de PERSONNE…
        IReadOnlyList<DemandeValidationListeDto> file =
            await service.ListerDemandesEnAttenteAsync(
                "chef-a", NiveauValidation.Labo);

        Assert.Empty(file);

        // …et personne ne peut la décider en douce.
        ResultatValidation resultat =
            await service.ValiderAuNiveauAsync(
                demande.Id, "chef-a", NiveauValidation.Labo, null);

        Assert.Equal(CodesValidation.AccesInterdit, resultat.Code);
    }

    [Fact]
    public async Task ValiderAuNiveau_DemandeDejaAutorisee_EstRefusee()
    {
        await using AppDbContext dbContext = CreerContexte();

        DemandePublication demande =
            await CreerMondeAvecDemandeAsync(
                dbContext, StatutValidationInterne.Autorisee);

        var service = new ValidationService(
            dbContext, TimeProvider.System);

        // Bon rôle, bonne personne… mais dossier déjà clos.
        ResultatValidation resultat =
            await service.ValiderAuNiveauAsync(
                demande.Id, "secretaire", NiveauValidation.Conseil, null);

        Assert.False(resultat.EstReussi);
        Assert.Equal(CodesValidation.NiveauIncorrect, resultat.Code);
    }
    
}