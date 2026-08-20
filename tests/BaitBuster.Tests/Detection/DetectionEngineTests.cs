using BaitBuster.Core.Detection;
using BaitBuster.Core.Models;
using BaitBuster.Tests.TestSupport;
using FluentAssertions;
using static BaitBuster.Tests.TestSupport.EmailBuilder;

namespace BaitBuster.Tests.Detection;

/// <summary>
/// Engine-ът не решава нищо сам — само пуска регистрираните правила и
/// сглобява доклада. Тестовете използват подставени правила, за да проверят
/// точно тази оркестрация, независимо от истинската детекционна логика.
/// </summary>
public class DetectionEngineTests
{
    [Fact]
    public void CollectsFindingsFromAllRegisteredRules()
    {
        var engine = new DetectionEngine([
            new StubRule("A-001", StubRule.Finding("A-001", 15)),
            new StubRule("B-001", StubRule.Finding("B-001", 25), StubRule.Finding("B-001", 10))
        ]);

        var report = engine.Analyze(Email().Build());

        report.Findings.Should().HaveCount(3);
        report.Findings.Select(f => f.RuleId).Should().Contain(["A-001", "B-001"]);
        report.RiskScore.Should().Be(50);
    }

    [Fact]
    public void InvokesEachRuleExactlyOnce()
    {
        var first = new StubRule("A-001");
        var second = new StubRule("B-001");
        var engine = new DetectionEngine([first, second]);

        engine.Analyze(Email().Build());

        first.EvaluateCallCount.Should().Be(1);
        second.EvaluateCallCount.Should().Be(1);
    }

    [Fact]
    public void WithNoRulesReportIsEmptyButValid()
    {
        var engine = new DetectionEngine([]);

        var report = engine.Analyze(Email().Build());

        report.Findings.Should().BeEmpty();
        report.Verdict.Should().Be(Verdict.Legitimate);
    }

    [Fact]
    public void CopiesSubjectAndSenderIntoReport()
    {
        var engine = new DetectionEngine([]);
        var email = Email()
            .WithSubject("Важно съобщение")
            .WithFrom("security@paypa1-support.com")
            .Build();

        var report = engine.Analyze(email);

        report.EmailSubject.Should().Be("Важно съобщение");
        report.FromAddress.Should().Be("security@paypa1-support.com");
    }

    [Fact]
    public void RuleWithNoFindingsDoesNotAffectResult()
    {
        var engine = new DetectionEngine([
            new StubRule("A-001", StubRule.Finding("A-001", 30)),
            new StubRule("B-001")
        ]);

        var report = engine.Analyze(Email().Build());

        report.Findings.Should().ContainSingle();
        report.RiskScore.Should().Be(30);
    }
}
