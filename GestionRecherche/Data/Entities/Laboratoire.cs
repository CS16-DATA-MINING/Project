using System;

namespace GestionRecherche.Data;

public class Laboratoire
{
public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Nom { get; set; } = string.Empty;

    public int UerId { get; set; }

    public Uer Uer { get; set; } = null!;

    public string? ChefLaboId { get; set; }

    public ApplicationUser? ChefLabo { get; set; }

    public bool EstActif { get; set; } = true;

    public ICollection<ApplicationUser> Membres { get; set; }
        = new List<ApplicationUser>();
}
