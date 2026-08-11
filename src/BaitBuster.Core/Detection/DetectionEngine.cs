using BaitBuster.Core.Models;

namespace BaitBuster.Core.Detection;

/// <summary>
/// Едно детекционно правило. Всяко правило е независимо, обяснимо и
/// връща нула или повече Finding-а. ML класификаторът по-късно се
/// интегрира като поредна имплементация на същия интерфейс.
/// </summary>
public interface IDetectionRule
{
    string RuleId { get; }
    IEnumerable<Finding> Evaluate(ParsedEmail email);
}

/// <summary>
/// Оркестрира всички регистрирани правила и сглобява крайния доклад.
/// Правилата се инжектират чрез DI — добавянето на ново правило не
/// изисква промяна тук (Open/Closed принцип, добър аргумент за записката).
/// </summary>
public sealed class DetectionEngine(IEnumerable<IDetectionRule> rules)
{
    public AnalysisReport Analyze(ParsedEmail email)
    {
        var report = new AnalysisReport
        {
            EmailSubject = email.Subject,
            FromAddress = email.FromAddress
        };

        foreach (var rule in rules)
        {
            report.Findings.AddRange(rule.Evaluate(email));
        }

        return report;
    }
}
