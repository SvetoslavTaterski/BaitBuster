namespace BaitBuster.Api.Persistence;

/// <summary>Обобщение върху всички запазени анализи.</summary>
public sealed record StatisticsResponse(
    int TotalAnalyses,
    double AverageRiskScore,
    int PhishingCount,
    int SuspiciousCount,
    int LegitimateCount,
    IReadOnlyList<NamedCount> FindingsByCategory,
    IReadOnlyList<NamedCount> TopRules,
    IReadOnlyList<DailyCount> LastDays
);

/// <summary>Име и брой — използва се за категориите и за най-честите правила.</summary>
public sealed record NamedCount(string Name, int Count);

/// <summary>Брой анализи за един ден.</summary>
public sealed record DailyCount(DateOnly Date, int Count);
