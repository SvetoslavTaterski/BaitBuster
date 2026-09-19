namespace BaitBuster.Core.Models;

/// <summary>Крайна класификация на анализирания имейл.</summary>
public enum Verdict
{
    Legitimate,
    Suspicious,
    Phishing
}

/// <summary>Тежест на отделен индикатор.</summary>
public enum Severity
{
    Info,
    Low,
    Medium,
    High
}

/// <summary>
/// Единичен индикатор (finding), открит от правило в детекционния engine.
/// Всеки finding е обясним: категория, описание и конкретното доказателство.
/// </summary>
public sealed record Finding(
    string RuleId,          // напр. "HDR-001"
    string Category,        // Headers | Urls | Content | Attachments | Ml
    Severity Severity,
    int Score,              // принос към общия risk score (0–100)
    string Description,     // човешко обяснение защо това е проблем
    string Evidence         // конкретното доказателство от имейла
);

/// <summary>
/// Пълният доклад от анализа — това, което API-то връща и от което
/// се генерира форензичният доклад за потребителя.
/// </summary>
public sealed class AnalysisReport
{
    public required string EmailSubject { get; init; }
    public required string FromAddress { get; init; }
    public DateTimeOffset AnalyzedAt { get; init; } = DateTimeOffset.UtcNow;

    public List<Finding> Findings { get; } = [];

    /// <summary>Таванът на risk score-а; сумата от приносите се ограничава дотук.</summary>
    public const int MaxScore = 100;

    /// <summary>От този score нагоре имейлът е „фишинг".</summary>
    public const int PhishingThreshold = 60;

    /// <summary>От този score нагоре (до <see cref="PhishingThreshold"/>) — „подозрителен".</summary>
    public const int SuspiciousThreshold = 30;

    /// <summary>Общ risk score 0–100 (сума от приносите, ограничена до 100).</summary>
    public int RiskScore => Math.Min(MaxScore, Findings.Sum(f => f.Score));

    public Verdict Verdict => RiskScore switch
    {
        >= PhishingThreshold => Verdict.Phishing,
        >= SuspiciousThreshold => Verdict.Suspicious,
        _ => Verdict.Legitimate
    };
}
