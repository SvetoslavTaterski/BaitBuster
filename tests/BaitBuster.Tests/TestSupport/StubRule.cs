using BaitBuster.Core.Detection;
using BaitBuster.Core.Models;

namespace BaitBuster.Tests.TestSupport;

/// <summary>
/// Правило с предварително зададен резултат. Позволява тестовете на
/// <see cref="DetectionEngine"/> да проверяват оркестрацията, без да зависят
/// от поведението на истинските правила.
/// </summary>
internal sealed class StubRule(string ruleId, params Finding[] findings) : IDetectionRule
{
    public string RuleId { get; } = ruleId;
    public string Name => $"Тестово правило {RuleId}";
    public string Category => "Test";
    public string Description => "Използва се само в тестовете.";
    public int MaxScore => findings.Length == 0 ? 0 : findings.Max(f => f.Score);

    /// <summary>Колко пъти engine-ът е извикал правилото.</summary>
    public int EvaluateCallCount { get; private set; }

    public IEnumerable<Finding> Evaluate(ParsedEmail email)
    {
        EvaluateCallCount++;
        return findings;
    }

    public static Finding Finding(string ruleId, int score, Severity severity = Severity.Medium) =>
        new(ruleId, "Test", severity, score, "Описание", "Доказателство");
}
