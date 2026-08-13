namespace BaitBuster.Api.Persistence;

public sealed class AnalysisRecord
{
    public int Id { get; set; }
    public string EmailSubject { get; set; } = "";
    public string FromAddress { get; set; } = "";
    public int RiskScore { get; set; }
    public string Verdict { get; set; } = "";

    // SQLite-ят provider на EF Core не поддържа ORDER BY върху DateTimeOffset,
    // затова пазим UTC DateTime и го увиваме обратно в DateTimeOffset в DTO-тата.
    public DateTime AnalyzedAt { get; set; }

    public List<FindingRecord> Findings { get; set; } = [];
}
