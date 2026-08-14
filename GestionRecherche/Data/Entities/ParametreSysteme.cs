using System;

namespace GestionRecherche.Data;

public class ParametreSysteme
{

public string Cle { get; set; } = string.Empty;

    public int Valeur { get; set; }

    public UniteParametre Unite { get; set; }

    public string Description { get; set; } = string.Empty;

    public DateTime? DateDerniereModificationUtc { get; set; }
    
}
