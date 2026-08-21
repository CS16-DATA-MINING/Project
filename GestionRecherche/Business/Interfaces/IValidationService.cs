using System;
using GestionRecherche.Data;

namespace GestionRecherche.Business;

public interface IValidationService
{
    

    // jai gardes les methode de ...labo ... a case des test 

    Task<IReadOnlyList<DemandeValidationListeDto>>
    ListerDemandesEnAttenteLaboAsync(
        string chefLaboId,
        CancellationToken cancellationToken = default);

Task<DemandeValidationDetailsDto?>
    ObtenirDetailsLaboAsync(
        int demandeId,
        string chefLaboId,
        CancellationToken cancellationToken = default);

Task<ResultatValidation> ValiderLaboAsync(
    int demandeId,
    string chefLaboId,
    string? commentaire = null,
    CancellationToken cancellationToken = default);

Task<ResultatValidation> RejeterLaboAsync(
    int demandeId,
    string chefLaboId,
    string? commentaire,
    CancellationToken cancellationToken = default);


    Task<ResultatValidation> ValiderAsync(int demandeId,string validateruId,NiveauValidation niveau,string? commentaire =null,CancellationToken cancellationToken=default);


    Task<ResultatValidation> RejeterAsync(int demandeId,string validateruId,NiveauValidation niveau,string? commentaire =null,CancellationToken cancellationToken=default);

//
// ces methodes supoerte tous les niveaux 
   Task<IReadOnlyList<DemandeValidationListeDto>> ListerDemandesEnAttenteAsync(
        string validateurId,
        NiveauValidation niveau,
        CancellationToken cancellationToken = default);
  
     Task<DemandeValidationDetailsDto?> ObtenirDetailsAsync(
        int demandeId,
        string validateurId,
        NiveauValidation niveau,
        CancellationToken cancellationToken = default);

Task<ResultatValidation> ValiderAuNiveauAsync(
        int demandeId,
        string validateurId,
        NiveauValidation niveau,
        string? commentaire = null,
        CancellationToken cancellationToken = default);


 Task<ResultatValidation> RejeterAuNiveauAsync(
        int demandeId,
        string validateurId,
        NiveauValidation niveau,
        string? commentaire,
        CancellationToken cancellationToken = default);
    

Task<ResultatValidation> ResoumettreApresRejetAsync(
    int demandeId,
    string auteurId,
    CancellationToken cancellationToken = default);


/*
 * Retourne l'historique complet des décisions d'une demande,
 * trié par cycle puis par date.
 * ATTENTION : aucun contrôle d'accès ici. L'appelant doit
 * avoir vérifié AVANT que l'utilisateur a le droit de voir
 * cette demande.
 */
Task<IReadOnlyList<EtapeHistoriqueDto>> ObtenirHistoriqueAsync(
    int demandeId,
    CancellationToken cancellationToken = default);
    

}
