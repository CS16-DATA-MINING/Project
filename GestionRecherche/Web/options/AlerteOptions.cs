using System;

namespace GestionRecherche.Web;

public sealed class AlerteOptions
{
    public const string SectionName = "Alerte";
    
    public int FrequenceHeures { get; set; } = 24;

}