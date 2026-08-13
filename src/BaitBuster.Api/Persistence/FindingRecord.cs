namespace BaitBuster.Api.Persistence;

public sealed class FindingRecord
{
    public int Id { get; set; }
    public string RuleId { get; set; } = "";
    public string Category { get; set; } = "";
    public string Severity { get; set; } = "";
    public int Score { get; set; }
    public string Description { get; set; } = "";
    public string Evidence { get; set; } = "";

    public int AnalysisRecordId { get; set; }
    public AnalysisRecord AnalysisRecord { get; set; } = null!;
}
