using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace GestionRecherche.Data;

public class ApplicationUser : IdentityUser
{

  [MaxLength(100)]
    public string Nom { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Prenom { get; set; } = string.Empty;    

    public string? GradeActuel { get; set; }

    public bool EstActif { get; set; } = true;

    public int? LaboratoireId { get; set; }

    public Laboratoire? Laboratoire { get; set; }

    public int? UerId { get; set; }

    public Uer? Uer { get; set; }

    public ICollection<DemandePublication> DemandesPublication { get; set; }
        = new List<DemandePublication>();  //Liste des demandes associées à ce journal.

}
