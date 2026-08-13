namespace BaitBuster.Api.Persistence;

public sealed record HistoryListItem(
    int Id,
    string EmailSubject,
    string FromAddress,
    int RiskScore,
    string Verdict,
    DateTimeOffset AnalyzedAt,
    int FindingsCount
);

public sealed record HistoryFindingDto(
    string RuleId,
    string Category,
    string Severity,
    int Score,
    string Description,
    string Evidence
);

public sealed record HistoryDetailResponse(
    int Id,
    string EmailSubject,
    string FromAddress,
    DateTimeOffset AnalyzedAt,
    int RiskScore,
    string Verdict,
    List<HistoryFindingDto> Findings
);
