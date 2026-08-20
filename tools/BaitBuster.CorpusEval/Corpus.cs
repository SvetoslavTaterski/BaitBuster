using BaitBuster.Core.Models;

namespace BaitBuster.CorpusEval;

/// <summary>Една папка от SpamAssassin корпуса и какво съдържа.</summary>
internal sealed record CorpusFolder(string Name, string Path, bool IsMalicious);

internal static class CorpusFolders
{
    /// <summary>
    /// Папките на публичния SpamAssassin корпус и класът им.
    /// „hard_ham“ са легитимни съобщения, които приличат на спам (реклами,
    /// бюлетини с много HTML) — те са най-честият източник на лъжливи
    /// тревоги и затова се отчитат отделно от „easy_ham“.
    /// </summary>
    public static readonly Dictionary<string, bool> Expected = new(StringComparer.Ordinal)
    {
        ["easy_ham"] = false,
        ["easy_ham_2"] = false,
        ["hard_ham"] = false,
        ["spam"] = true,
        ["spam_2"] = true,
    };

    public static List<CorpusFolder> Discover(string root) =>
        Expected
            .Select(pair => new CorpusFolder(pair.Key, Path.Combine(root, pair.Key), pair.Value))
            .Where(folder => Directory.Exists(folder.Path))
            .OrderBy(folder => folder.IsMalicious)
            .ThenBy(folder => folder.Name, StringComparer.Ordinal)
            .ToList();
}

/// <summary>
/// Брои присъдите за една папка (или за целия корпус). Държи класовете
/// разделени, защото една и съща присъда значи различно нещо според това
/// дали съобщението е атака или не.
/// </summary>
internal sealed class Tally
{
    private long scoreSum;

    public int Total { get; private set; }
    public int Phishing { get; private set; }
    public int Suspicious { get; private set; }
    public int Legitimate { get; private set; }

    public int MaliciousPhishing { get; private set; }
    public int MaliciousSuspicious { get; private set; }
    public int MaliciousLegitimate { get; private set; }

    public int BenignPhishing { get; private set; }
    public int BenignSuspicious { get; private set; }
    public int BenignLegitimate { get; private set; }

    public int MaliciousTotal => MaliciousPhishing + MaliciousSuspicious + MaliciousLegitimate;
    public int BenignTotal => BenignPhishing + BenignSuspicious + BenignLegitimate;

    public double AverageScore => Total == 0 ? 0 : (double)scoreSum / Total;

    public void Add(Verdict verdict, int riskScore, bool isMalicious)
    {
        Total++;
        scoreSum += riskScore;

        switch (verdict)
        {
            case Verdict.Phishing:
                Phishing++;
                if (isMalicious) MaliciousPhishing++; else BenignPhishing++;
                break;

            case Verdict.Suspicious:
                Suspicious++;
                if (isMalicious) MaliciousSuspicious++; else BenignSuspicious++;
                break;

            default:
                Legitimate++;
                if (isMalicious) MaliciousLegitimate++; else BenignLegitimate++;
                break;
        }
    }

    public FolderReport ToReport(string name, bool? isMalicious) => new(
        name, isMalicious, Total,
        Phishing, Suspicious, Legitimate,
        MaliciousPhishing, MaliciousSuspicious, MaliciousLegitimate,
        BenignPhishing, BenignSuspicious, BenignLegitimate,
        Math.Round(AverageScore, 2));
}

internal sealed record FolderReport(
    string Name,
    bool? IsMalicious,
    int Total,
    int Phishing,
    int Suspicious,
    int Legitimate,
    int MaliciousPhishing,
    int MaliciousSuspicious,
    int MaliciousLegitimate,
    int BenignPhishing,
    int BenignSuspicious,
    int BenignLegitimate,
    double AverageRiskScore
);

internal sealed record CorpusEvalReport(
    DateTimeOffset EvaluatedAt,
    string CorpusDirectory,
    string ModelPath,
    int MessagesAnalyzed,
    int ParseFailures,
    FolderReport Overall,
    IReadOnlyList<FolderReport> Folders,
    IReadOnlyDictionary<string, int> RuleHits
);
