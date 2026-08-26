using System;
using GestionRecherche.Data;
using Microsoft.EntityFrameworkCore;

namespace GestionRecherche.Business;

public class ValidationService : IValidationService
{
     private const int LongueurMaxCommentaire = 1000;
     
     private const string RoleChefLaboNormalise =
    "CHEFLABO";

private const string RoleChefUerNormalise =
    "CHEFUER";

private const string RoleChefBureauNormalise =
    "CHEFBUREAU";

private const string RoleSecretariatConseilNormalise =
    "SECRETARIATCONSEIL";

     private readonly AppDbContext _dbContext;
    private readonly TimeProvider _timeProvider;


    public ValidationService(AppDbContext dbContext, TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
    }

    public Task <ResultatValidation> ValiderAsync( int demandeId,
        string validateurId,
        NiveauValidation niveau,
        string ?commentaire,
        CancellationToken cancellationToken = default)
    {
        
 return ExecuterDecisionAsync(
            demandeId,
            validateurId,
            niveau,
            DecisionValidation.Validee,
            commentaire,
            cancellationToken);

    }

    public Task<ResultatValidation> RejeterAsync( int demandeId,
        string validateurId,
        NiveauValidation niveau,
        string ?commentaire,
        CancellationToken cancellationToken = default)
    {
         return ExecuterDecisionAsync(
            demandeId,
            validateurId,
            niveau,
            DecisionValidation.Rejetee,
            commentaire,
            cancellationToken);
    }

    private async Task <ResultatValidation> ExecuterDecisionAsync(int demandeId,
        string validateurId,
        NiveauValidation niveau,
        DecisionValidation decision,
        string? commentaire,
        CancellationToken cancellationToken)
    {

        // verifer les diff donnes recus
        if (demandeId <= 0)
        {
            return ResultatValidation.Echec(
                CodesValidation.DonneesInvalides,
                "L’identifiant de la demande est invalide.");
        }

        if (string.IsNullOrWhiteSpace(validateurId))
        {
            return ResultatValidation.Echec(
                CodesValidation.DonneesInvalides,
                "L’identifiant du validateur est obligatoire.");
        }
  
          string? commentaireNormalise =
            string.IsNullOrWhiteSpace(commentaire)
                ? null
                : commentaire.Trim();



          if (decision == DecisionValidation.Rejetee &&
            commentaireNormalise is null)
        {
            return ResultatValidation.Echec(
                CodesValidation.CommentaireRejetObligatoire,
                "Un commentaire est obligatoire pour rejeter une demande.");
        }

        if (commentaireNormalise?.Length > LongueurMaxCommentaire)
        {
            return ResultatValidation.Echec(
                CodesValidation.DonneesInvalides,
                $"Le commentaire ne doit pas dépasser " +
                $"{LongueurMaxCommentaire} caractères.");
        }

      
      DemandePublication? demande =
            await _dbContext.DemandesPublication
                .SingleOrDefaultAsync(
                    demande => demande.Id == demandeId,
                    cancellationToken);

    if (demande is null)
        {
            return ResultatValidation.Echec(
                CodesValidation.DemandeIntrouvable,
                "La demande est introuvable.");
        }       


       if (demande.StatutValidationInterne is
            StatutValidationInterne.Autorisee or
            StatutValidationInterne.Rejetee)
        {
            return ResultatValidation.Echec(
                CodesValidation.DemandeTerminee,
                "Cette demande est déjà terminée.");
        }


         // trouver le niveu 
          NiveauValidation? niveauAttendu =
            ObtenirNiveauAttendu(
                demande.StatutValidationInterne);

         if(niveauAttendu is null)
        {
            return ResultatValidation.Echec(CodesValidation.DemandeTerminee,
                "Le statut actuel ne permet plus de prendre une décision.");

                    }

        if (niveauAttendu.Value != niveau)
        {
            return ResultatValidation.Echec(
                CodesValidation.NiveauIncorrect,
                $"La demande attend une décision du niveau " +
                $"« {niveauAttendu.Value} » et non du niveau " +
                $"« {niveau} ».");
        }

       bool decisionDejaEnregistree =
            await _dbContext.EtapesValidation
                .AnyAsync(
                    etape =>
                        etape.DemandePublicationId == demande.Id &&
                        etape.NumeroCycleValidation ==
                            demande.NumeroCycleValidation &&
                        etape.Niveau == niveau,
                    cancellationToken);

             if (decisionDejaEnregistree)
        {
            return ResultatValidation.Echec(
                CodesValidation.NiveauIncorrect,
                "Une décision existe déjà pour ce niveau " +
                "et ce cycle de validation.");
        }


       DateTime maintenantUtc =
            _timeProvider.GetUtcNow().UtcDateTime;


         StatutValidationInterne nouveauStatut =
            decision == DecisionValidation.Rejetee
                ? StatutValidationInterne.Rejetee
                : ObtenirStatutSuivant(
                    demande.StatutValidationInterne);




         var etapeValidation = new EtapeValidation
        {
            DemandePublicationId = demande.Id,
            ValidateurId = validateurId.Trim(),
            Niveau = niveau,
            Decision = decision,
            Commentaire = commentaireNormalise,
            DateDecisionUtc = maintenantUtc,
            NumeroCycleValidation =
                demande.NumeroCycleValidation
        };       
 
     _dbContext.EtapesValidation.Add(etapeValidation);

      demande.StatutValidationInterne = nouveauStatut;

  if (nouveauStatut ==
            StatutValidationInterne.Autorisee)
        {
            demande.DateAutorisationConseilUtc =
                maintenantUtc;

             await MettreNouveauxJournauxEnAttenteModerationAsync(
        demande.Id,
        cancellationToken);



        }
        


        try
        {
            /*
             * Un seul SaveChangesAsync enregistre :
             * - le nouveau statut ;
             * - la nouvelle ligne EtapeValidation.
             *
             * Avec SQL Server, EF Core utilise une transaction.
             */
            await _dbContext.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException)
        {
            /*
             * L’index unique du Jour 2 peut bloquer deux
             * décisions simultanées pour le même niveau.
             */
            _dbContext.ChangeTracker.Clear();

            return ResultatValidation.Echec(
                CodesValidation.NiveauIncorrect,
                "La décision n’a pas pu être enregistrée. " +
                "La demande a peut-être déjà été traitée.");
        }


    
    string message =
            decision == DecisionValidation.Validee
                ? $"La demande a été validée au niveau {niveau}."
                : $"La demande a été rejetée au niveau {niveau}.";

        return ResultatValidation.Reussite(message);
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

 private static StatutValidationInterne ObtenirStatutSuivant(
        StatutValidationInterne statutActuel)
    {
        return statutActuel switch
        {
            StatutValidationInterne.EnAttenteLabo =>
                StatutValidationInterne.EnAttenteUer,

            StatutValidationInterne.EnAttenteUer =>
                StatutValidationInterne.EnAttenteBureau,

            StatutValidationInterne.EnAttenteBureau =>
                StatutValidationInterne.EnAttenteConseil,

            StatutValidationInterne.EnAttenteConseil =>
                StatutValidationInterne.Autorisee,

            _ => throw new InvalidOperationException(
                $"Aucune transition n’existe depuis " +
                $"le statut « {statutActuel} ».")
        };


        


    }











private static StatutValidationInterne ObtenirStatutAttendu(
        NiveauValidation niveau)
    {
        return niveau switch
        {
            NiveauValidation.Labo => StatutValidationInterne.EnAttenteLabo,
            NiveauValidation.Uer => StatutValidationInterne.EnAttenteUer,
            NiveauValidation.Bureau => StatutValidationInterne.EnAttenteBureau,
            NiveauValidation.Conseil => StatutValidationInterne.EnAttenteConseil,

            _ => throw new ArgumentOutOfRangeException(
                nameof(niveau),
                $"Niveau de validation inconnu : {niveau}.")
        };
    }



     /*
     * Construit la requête « demandes visibles par CE validateur à CE niveau ».
     * le tout en UNE seule requête SQL au moment du ToListAsync.
     */
     //quelles demandes cette personne a-t-elle le droit de voir
         private IQueryable<DemandePublication> ConstruireRequetePourNiveau(
        string validateurId,
        NiveauValidation niveau)
    {
        IQueryable<DemandePublication> demandes =
            _dbContext.DemandesPublication.AsNoTracking();

        return niveau switch
        {
            // File PAR STRUCTURE : le labo de l'auteur, dirigé par la personne connectée.
            NiveauValidation.Labo => demandes.Where(demande =>
                demande.StatutValidationInterne ==
                    StatutValidationInterne.EnAttenteLabo &&
                demande.Auteur.Laboratoire != null &&
                demande.Auteur.Laboratoire.EstActif &&
                demande.Auteur.Laboratoire.ChefLaboId == validateurId),

            // File PAR STRUCTURE : l'UER de l'auteur, dirigée par la personne connectée.
            NiveauValidation.Uer => demandes.Where(demande =>
                demande.StatutValidationInterne ==
                    StatutValidationInterne.EnAttenteUer &&
                demande.Auteur.Uer != null &&
                demande.Auteur.Uer.EstActive &&
                demande.Auteur.Uer.ChefUerId == validateurId),


      // file par structure : car on doit filtrer les demnades selon labo/uer
    

            // File GLOBALE : un seul Bureau pour l'institut.
            // Le droit d'accès est vérifié par le RÔLE (VerifierAccesAsync),
            // pas par une structure.
            NiveauValidation.Bureau => demandes.Where(demande =>
                demande.StatutValidationInterne ==
                    StatutValidationInterne.EnAttenteBureau),

// File GLOBALE : un seul Conseil Scientifique pour l'institut.
// Même logique que le Bureau : le droit d'accès est vérifié
// par le RÔLE (VerifierAccesAsync), pas par une structure.

            
   NiveauValidation.Conseil => demandes.Where(demande =>
    demande.StatutValidationInterne ==
        StatutValidationInterne.EnAttenteConseil),
    
             _  => demandes.Where(demande => false)
        };


   // Sécurité : tout niveau inconnu => aucune demande.
      

    }








public async Task<
    IReadOnlyList<DemandeValidationListeDto>>
    ListerDemandesEnAttenteAsync(
        string validateurId,
        NiveauValidation niveau,
        CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(validateurId))
    {
        return Array.Empty<
            DemandeValidationListeDto>();
    }

    string idNormalise = validateurId.Trim();

    bool autorise =
        await EstValidateurActifPourNiveauAsync(
            idNormalise,
            niveau,
            cancellationToken);

    if (!autorise)
    {
        return Array.Empty<
            DemandeValidationListeDto>();
    }

    return await ConstruireRequetePourNiveau(
            validateurId,
            niveau)
        .OrderBy(demande => demande.DateCreationUtc)
        .Select(demande => new DemandeValidationListeDto
        {
            Id = demande.Id,
            Titre = demande.Titre,

            AuteurNomComplet =
                demande.Auteur.Prenom + " " +
                demande.Auteur.Nom,



 StructureNom =
        niveau == NiveauValidation.Uer
            ? demande.Auteur.Uer!.Nom
            : demande.Auteur.Laboratoire!.Nom,



            DateCreationUtc =
                demande.DateCreationUtc,

            NumeroCycleValidation =
                demande.NumeroCycleValidation
        })
        .ToListAsync(cancellationToken);
}




/*
 * Passe toutes les PROPOSITIONS de nouveaux journaux de la
 * demande au statut "EnAttente" (de modération).
 *
 * Filtres importants :
 * - EstNouveauJournal : les journaux existants sélectionnés
 *   en base ne sont pas concernés (StatutModeration reste NonRequise) ;
 * - JournalId == null : si une proposition a déjà été rattachée
 *   à un journal officiel (fusion déjà faite par le Chef de
 *   Bureau lors d'une autre demande), on n'y touche plus.
 */
private async Task MettreNouveauxJournauxEnAttenteModerationAsync(
    int demandeId,
    CancellationToken cancellationToken)
{
    List<JournalCibleDemande> propositions =
        await _dbContext.JournauxCiblesDemande
            .Where(cible =>
                cible.DemandePublicationId == demandeId &&
                cible.EstNouveauJournal &&
                cible.JournalId == null)
            .ToListAsync(cancellationToken);

    foreach (JournalCibleDemande proposition in propositions)
    {
        proposition.StatutModeration = StatutModeration.EnAttente;
    }


}








    public async Task<DemandeValidationDetailsDto?> ObtenirDetailsAsync(
        int demandeId,
        string validateurId,
        NiveauValidation niveau,
        CancellationToken cancellationToken = default)
    {
        if (demandeId <= 0 || string.IsNullOrWhiteSpace(validateurId))
        {
            return null;
        }

        string idNormalise = validateurId.Trim();

        /*
         * On repart de la MÊME requête filtrée : si la demande n'est pas
         * dans la file de ce validateur, le résultat est null -> le
         * contrôleur renvoie 404 sans révéler que la demande existe.
         */


         bool autorise =
    await EstValidateurActifPourNiveauAsync(
        idNormalise,
        niveau,
        cancellationToken);

if (!autorise)
{
    return null;
}

        return await ConstruireRequetePourNiveau(idNormalise, niveau)
            .Where(demande => demande.Id == demandeId)
            .Select(demande => new DemandeValidationDetailsDto
            {
                Id = demande.Id,

                Titre = demande.Titre,

                Abstract = demande.Abstract,

                AuteurNomComplet =
                    demande.Auteur.Prenom + " " + demande.Auteur.Nom,

                StructureNom = niveau == NiveauValidation.Uer
                    ? (demande.Auteur.Uer != null
                        ? demande.Auteur.Uer.Nom
                        : "UER inconnue")
                    : (demande.Auteur.Laboratoire != null
                        ? demande.Auteur.Laboratoire.Nom
                        : "Laboratoire inconnu"),

                DateCreationUtc = demande.DateCreationUtc,

                NumeroCycleValidation = demande.NumeroCycleValidation,

                Journaux = demande.JournauxCibles
                    .OrderBy(cible => cible.OrdrePreference)
                    .Select(cible => new JournalCibleValidationDto
                    {
                        OrdrePreference = cible.OrdrePreference,

                        Nom = cible.EstNouveauJournal
                            ? cible.NomJournalSaisiManuellement
                                ?? "Journal proposé"
                            : cible.Journal != null
                                ? cible.Journal.Nom
                                : "Journal indisponible",

                        Editeur = cible.EstNouveauJournal
                            ? cible.EditeurSaisiManuellement
                            : cible.Journal != null
                                ? cible.Journal.Editeur
                                : null,

                        Issn = cible.EstNouveauJournal
                            ? cible.IssnSaisiManuellement
                            : cible.Journal != null
                                ? cible.Journal.Issn
                                : null,

                        EstNouveauJournal = cible.EstNouveauJournal
                    })
                    .ToList()
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    /*
     * Contrôle d'accès AVANT décision (défense en profondeur).
     * Retourne null si l'accès est autorisé, sinon l'erreur métier.
     */
private async Task<bool>
    EstValidateurActifPourNiveauAsync(
        string validateurId,
        NiveauValidation niveau,
        CancellationToken cancellationToken)
{
    string? roleNormalise = niveau switch
    {
        NiveauValidation.Labo =>
            RoleChefLaboNormalise,

        NiveauValidation.Uer =>
            RoleChefUerNormalise,

        NiveauValidation.Bureau =>
            RoleChefBureauNormalise,

        NiveauValidation.Conseil =>
            RoleSecretariatConseilNormalise,

        _ => null
    };

    if (roleNormalise is null)
    {
        return false;
    }

    bool possedeRoleEtEstActif =
        await EstDansRoleActifAsync(
            validateurId,
            roleNormalise,
            cancellationToken);

    if (!possedeRoleEtEstActif)
    {
        return false;
    }

    return niveau switch
    {
        NiveauValidation.Labo =>
            await _dbContext.Laboratoires
                .AsNoTracking()
                .AnyAsync(
                    laboratoire =>
                        laboratoire.EstActif &&
                        laboratoire.ChefLaboId ==
                            validateurId,
                    cancellationToken),

        NiveauValidation.Uer =>
            await _dbContext.Uers
                .AsNoTracking()
                .AnyAsync(
                    uer =>
                        uer.EstActive &&
                        uer.ChefUerId ==
                            validateurId,
                    cancellationToken),

        NiveauValidation.Bureau => true,
        NiveauValidation.Conseil => true,

        _ => false
    };
}


    private async Task<ResultatValidation?> VerifierAccesAsync(
        int demandeId,
        string validateurId,
        NiveauValidation niveau,
        CancellationToken cancellationToken)
    {
        if (demandeId <= 0 || string.IsNullOrWhiteSpace(validateurId))
        {
            return ResultatValidation.Echec(
                CodesValidation.DonneesInvalides,
                "Les informations de la décision sont invalides.");
        }

        string idNormalise = validateurId.Trim();


bool validateurAutorise =
    await EstValidateurActifPourNiveauAsync(
        idNormalise,
        niveau,
        cancellationToken);

if (!validateurAutorise)
{
    return ResultatValidation.Echec(
        CodesValidation.AccesInterdit,
        "Vous ne pouvez pas traiter cette demande.");
}


        var contexte =
            await _dbContext.DemandesPublication
                .AsNoTracking()
                .Where(demande => demande.Id == demandeId)
                .Select(demande => new
                {
                    demande.StatutValidationInterne,

                    LaboratoireActif =
                        demande.Auteur.Laboratoire != null &&
                        demande.Auteur.Laboratoire.EstActif,

                    ChefLaboId =
                        demande.Auteur.Laboratoire == null
                            ? null
                            : demande.Auteur.Laboratoire.ChefLaboId,

                    UerActive =
                        demande.Auteur.Uer != null &&
                        demande.Auteur.Uer.EstActive,

                    ChefUerId =
                        demande.Auteur.Uer == null
                            ? null
                            : demande.Auteur.Uer.ChefUerId
                })
                .SingleOrDefaultAsync(cancellationToken);

        if (contexte is null)
        {
            return ResultatValidation.Echec(
                CodesValidation.DemandeIntrouvable,
                "La demande est introuvable.");
        }

        bool accesAutorise;

        switch (niveau)
        {
            case NiveauValidation.Labo:
                accesAutorise =
                    contexte.LaboratoireActif &&
                    string.Equals(
                        contexte.ChefLaboId,
                        idNormalise,
                        StringComparison.Ordinal);
                break;

            case NiveauValidation.Uer:
                accesAutorise =
                    contexte.UerActive &&
                    string.Equals(
                        contexte.ChefUerId,
                        idNormalise,
                        StringComparison.Ordinal);
                break;

            case NiveauValidation.Bureau:
                // Pas de structure : on vérifie le rôle en base,
                // comme le fait déjà ton RoutingService.
                accesAutorise = await EstChefBureauActifAsync(
                    idNormalise,
                    cancellationToken);
                break;

     case NiveauValidation.Conseil:
    // Comme le Bureau : pas de structure, un rôle.
    accesAutorise = await EstDansRoleActifAsync(
        idNormalise,
        RoleSecretariatConseilNormalise,
        cancellationToken);

    break;

            default:
                accesAutorise = false;
                break;
        }

        if (!accesAutorise)
        {
            return ResultatValidation.Echec(
                CodesValidation.AccesInterdit,
                "Vous ne pouvez pas traiter cette demande à ce niveau.");
        }

        if (contexte.StatutValidationInterne !=
            ObtenirStatutAttendu(niveau))
        {
            return ResultatValidation.Echec(
                CodesValidation.NiveauIncorrect,
                $"Cette demande n'est plus en attente de validation "
                + $"« {niveau} ».");
        }

        return null;
    }
    /*
 * Vérifie qu'un utilisateur est ACTIF et possède un rôle donné.
 * Utilisé pour les niveaux "globaux" (Bureau, Conseil) qui ne
 * sont pas liés à une structure mais à un rôle Identity.
 *
 */
private async Task<bool> EstDansRoleActifAsync(
    string utilisateurId,
    string roleNormalise,
    CancellationToken cancellationToken)
{
    return await (
        from user in _dbContext.Users.AsNoTracking()

        join userRole in _dbContext.UserRoles.AsNoTracking()
            on user.Id equals userRole.UserId

        join role in _dbContext.Roles.AsNoTracking()
            on userRole.RoleId equals role.Id

        where
            user.Id == utilisateurId &&
            user.EstActif &&
            role.NormalizedName == roleNormalise
            //identity stocke chaque role en 2 version  on commpare a la version normalise


        select user.Id)
        .AnyAsync(cancellationToken);
}


private Task<bool> EstChefBureauActifAsync(
    string utilisateurId,
    CancellationToken cancellationToken)
    => EstDansRoleActifAsync(
        utilisateurId,
        RoleChefBureauNormalise,
        cancellationToken);


    public async Task<ResultatValidation> ValiderAuNiveauAsync(
        int demandeId,
        string validateurId,
        NiveauValidation niveau,
        string? commentaire = null,
        CancellationToken cancellationToken = default)
    {
        ResultatValidation? erreur =
            await VerifierAccesAsync(
                demandeId, validateurId, niveau, cancellationToken);

        if (erreur is not null)
        {
            return erreur;
        }

        return await ValiderAsync(
            demandeId, validateurId, niveau, commentaire, cancellationToken);
    }

    public async Task<ResultatValidation> RejeterAuNiveauAsync(
        int demandeId,
        string validateurId,
        NiveauValidation niveau,
        string? commentaire,
        CancellationToken cancellationToken = default)
    {
        ResultatValidation? erreur =
            await VerifierAccesAsync(
                demandeId, validateurId, niveau, cancellationToken);

        if (erreur is not null)
        {
            return erreur;
        }

        return await RejeterAsync(
            demandeId, validateurId, niveau, commentaire, cancellationToken);
    }

    // ----- Compatibilité Jour 5 : le Labo n'est plus qu'un cas particulier -----

    public Task<IReadOnlyList<DemandeValidationListeDto>>
        ListerDemandesEnAttenteLaboAsync(
            string chefLaboId,
            CancellationToken cancellationToken = default)
        => ListerDemandesEnAttenteAsync(
            chefLaboId, NiveauValidation.Labo, cancellationToken);

    public Task<DemandeValidationDetailsDto?> ObtenirDetailsLaboAsync(
        int demandeId,
        string chefLaboId,
        CancellationToken cancellationToken = default)
        => ObtenirDetailsAsync(
            demandeId, chefLaboId, NiveauValidation.Labo, cancellationToken);

    public Task<ResultatValidation> ValiderLaboAsync(
        int demandeId,
        string chefLaboId,
        string? commentaire = null,
        CancellationToken cancellationToken = default)
        => ValiderAuNiveauAsync(
            demandeId, chefLaboId, NiveauValidation.Labo,
            commentaire, cancellationToken);

    public Task<ResultatValidation> RejeterLaboAsync(
        int demandeId,
        string chefLaboId,
        string? commentaire,
        CancellationToken cancellationToken = default)
        => RejeterAuNiveauAsync(
            demandeId, chefLaboId, NiveauValidation.Labo,
            commentaire, cancellationToken);


/*
 * Règle de correction/résoumission  :
 * seul l'auteur, uniquement si Rejetée.
 * On ouvre un NOUVEAU cycle : l'historique du cycle
 * précédent (dont le motif du rejet) est conservé.
 */
public async Task<ResultatValidation> ResoumettreApresRejetAsync(
    int demandeId,
    string auteurId,
    CancellationToken cancellationToken = default)
{
    if (demandeId <= 0 || string.IsNullOrWhiteSpace(auteurId))
    {
        return ResultatValidation.Echec(
            CodesValidation.DonneesInvalides,
            "Les informations de la résoumission sont invalides.");
    }

    DemandePublication? demande =
        await _dbContext.DemandesPublication
            .SingleOrDefaultAsync(
                d => d.Id == demandeId,
                cancellationToken);

    if (demande is null)
    {
        return ResultatValidation.Echec(
            CodesValidation.DemandeIntrouvable,
            "La demande est introuvable.");
    }

    if (!string.Equals(demande.AuteurId,
            auteurId.Trim(), StringComparison.Ordinal))
    {
        return ResultatValidation.Echec(
            CodesValidation.AccesInterdit,
            "Seul l’auteur de la demande peut la résoumettre.");
    }

     

    if (demande.StatutValidationInterne !=
        StatutValidationInterne.Rejetee)
    {
        return ResultatValidation.Echec(
            CodesValidation.ResoumissionImpossible,
            "Seule une demande rejetée peut être corrigée "
            + "puis résoumise.");
    }

    demande.NumeroCycleValidation += 1;
    demande.StatutValidationInterne =
        StatutValidationInterne.EnAttenteLabo;

    await _dbContext.SaveChangesAsync(cancellationToken);

    return ResultatValidation.Reussite(
        $"La demande repart au niveau Labo "
        + $"(cycle n° {demande.NumeroCycleValidation}).");
}


public async Task<IReadOnlyList<EtapeHistoriqueDto>>
    ObtenirHistoriqueAsync(
        int demandeId,
        CancellationToken cancellationToken = default)
{
    if (demandeId <= 0)
    {
        return Array.Empty<EtapeHistoriqueDto>();
    }

    return await _dbContext.EtapesValidation      // ➊
        .AsNoTracking()                           // ➋
        .Where(etape =>
            etape.DemandePublicationId == demandeId)   // ➌
        .OrderBy(etape => etape.NumeroCycleValidation) // ➍
        .ThenBy(etape => etape.DateDecisionUtc)        // ➎
        .Select(etape => new EtapeHistoriqueDto        // ➏
        {
            Niveau = etape.Niveau,
            Decision = etape.Decision,

            ValidateurNomComplet =
                etape.Validateur.Prenom + " " +
                etape.Validateur.Nom,                  // ➐

            Commentaire = etape.Commentaire,
            DateDecisionUtc = etape.DateDecisionUtc,
            NumeroCycleValidation =
                etape.NumeroCycleValidation
        })
        .ToListAsync(cancellationToken);               // ➑
}



    }
















