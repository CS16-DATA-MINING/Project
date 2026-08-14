using System;

namespace GestionRecherche.Data;

public class Uer
{
 public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Nom { get; set; } = string.Empty;

    public string ChefUerId { get; set; }=string.Empty;

    public ApplicationUser ChefUer { get; set; }

    public bool EstActive { get; set; } = true;

    public ICollection<Laboratoire> Laboratoires { get; set; }
        = new List<Laboratoire>();

    public ICollection<ApplicationUser> Membres { get; set; }
        = new List<ApplicationUser>();
}
