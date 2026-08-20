using BaitBuster.Core.Models;
using FluentAssertions;

namespace BaitBuster.Tests.Models;

/// <summary>
/// Тук се решава какво вижда потребителят накрая: сборът на находките и
/// праговете, които го превръщат в присъда. Промяна в тези числа мени
/// поведението на цялото приложение, затова са заковани с тестове.
/// </summary>
public class AnalysisReportTests
{
    private static AnalysisReport ReportWithScores(params int[] scores)
    {
        var report = new AnalysisReport
        {
            EmailSubject = "Тема",
            FromAddress = "sender@example.com"
        };

        foreach (var score in scores)
            report.Findings.Add(new Finding("TST-001", "Test", Severity.Medium, score, "Описание", "Доказателство"));

        return report;
    }

    [Fact]
    public void EmptyReportIsLegitimateWithZeroScore()
    {
        var report = ReportWithScores();

        report.RiskScore.Should().Be(0);
        report.Verdict.Should().Be(Verdict.Legitimate);
    }

    [Fact]
    public void ScoreIsSumOfFindingContributions()
    {
        var report = ReportWithScores(15, 10, 5);

        report.RiskScore.Should().Be(30);
    }

    [Fact]
    public void ScoreIsCappedAtHundred()
    {
        var report = ReportWithScores(25, 25, 25, 25, 25, 25);

        report.RiskScore.Should().Be(100);
    }

    [Theory]
    [InlineData(0, Verdict.Legitimate)]
    [InlineData(29, Verdict.Legitimate)]
    [InlineData(30, Verdict.Suspicious)]   // долната граница на „подозрителен"
    [InlineData(59, Verdict.Suspicious)]
    [InlineData(60, Verdict.Phishing)]     // долната граница на „фишинг"
    [InlineData(100, Verdict.Phishing)]
    public void VerdictFollowsThirtyAndSixtyThresholds(int score, Verdict expected)
    {
        var report = ReportWithScores(score);

        report.Verdict.Should().Be(expected);
    }

    [Fact]
    public void AnalyzedAtIsSetAutomatically()
    {
        var report = ReportWithScores();

        report.AnalyzedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }
}
