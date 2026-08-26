using System;

namespace GestionRecherche.Business;

public sealed record ResultatPassageAlerte(
    bool EstReussi,
    string Code,
    string Message,
    DateTime DateDebutUtc,
    DateTime DateFinUtc,
    int? DelaiD1Jours,
    int? DelaiD2Mois,
    int DemandesExaminees,
    int AnomaliesCreees);
    