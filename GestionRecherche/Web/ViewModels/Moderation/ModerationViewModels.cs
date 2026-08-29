namespace GestionRecherche.Web;

public sealed class ModerationIndexViewModel
{
    public List<ModerationGroupSummaryViewModel> Groups { get; set; } = new();
}

public sealed class ModerationGroupSummaryViewModel
{
    public int GroupId { get; set; }
    public string RepresentativeTitle { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class ModerationGroupViewModel
{
    public int GroupId { get; set; }
    public List<ModerationCandidateViewModel> Candidates { get; set; } = new();
}

public sealed class ModerationCandidateViewModel
{
    public int CandidateId { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string? Issn { get; set; }
    public string? Editeur { get; set; }
    public int DemandeId { get; set; }
}

public sealed class ModerationMergeViewModel
{
    public int GroupId { get; set; }
    public int SelectedCandidateId { get; set; }
}
