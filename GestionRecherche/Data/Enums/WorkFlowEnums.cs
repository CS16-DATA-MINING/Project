using System;

namespace GestionRecherche.Data;

public enum StatutValidationInterne
{
 EnAttenteLabo = 1,
    EnAttenteUer = 2,
    EnAttenteBureau = 3,
    EnAttenteConseil = 4,
    Autorisee = 5,
    Rejetee = 6

}

public enum NiveauValidation
{
    Labo = 1,
    Uer = 2,
    Bureau = 3,
    Conseil = 4
}



public enum DecisionValidation
{
    Validee = 1,
    Rejetee = 2
}

public enum StatutModeration
{
    NonRequise = 0,
    Proposee = 1,
    EnAttente = 2,
    Fusionnee = 3,
    Creee = 4
}


public enum StatutEditeur
{
    Soumis = 1,
    UnderReview = 2,
    MinorRevision = 3,
    MajorRevision = 4,
    Accepted = 5,
    Published = 6,
    Rejected = 7
}


public enum TypeAnomalie
{
    NonSoumission = 1,
    Stagnation = 2
}

public enum StatutAnomalie
{
    Ouverte = 1,
    Traitee = 2
}


public enum UniteParametre
{
    Nombre = 1,
    Jours = 2,
    Mois = 3
}