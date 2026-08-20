using BaitBuster.Core.Detection.Rules;
using BaitBuster.Core.Models;
using FluentAssertions;
using static BaitBuster.Tests.TestSupport.EmailBuilder;

namespace BaitBuster.Tests.Detection.Rules;

/// <summary>
/// CNT-001 търси езика на социалното инженерство. За разлика от останалите
/// правила тук няма технически факт — само формулировки, затова тежестта
/// е умерена и находката служи повече като контекст.
/// </summary>
public class UrgencyContentRuleTests
{
    private readonly UrgencyContentRule _rule = new();

    [Fact]
    public void DetectsEnglishPressurePhrase()
    {
        var email = Email().WithBody("Please verify your account within 24 hours.").Build();

        var findings = _rule.Evaluate(email).ToList();

        findings.Should().ContainSingle();
        findings[0].Category.Should().Be("Content");
        findings[0].Severity.Should().Be(Severity.Medium);
    }

    [Fact]
    public void DetectsBulgarianPressurePhrase()
    {
        var email = Email().WithBody("Моля, потвърдете данните си, за да не спрем достъпа.").Build();

        _rule.Evaluate(email).Should().ContainSingle();
    }

    [Fact]
    public void MatchingIsCaseInsensitive()
    {
        var email = Email().WithBody("PLEASE VERIFY YOUR ACCOUNT NOW").Build();

        _rule.Evaluate(email).Should().ContainSingle();
    }

    [Fact]
    public void SearchesSubjectAsWell()
    {
        var email = Email()
            .WithSubject("Urgent action required")
            .WithBody("Съвсем безобидно съдържание.")
            .Build();

        _rule.Evaluate(email).Should().ContainSingle();
    }

    [Fact]
    public void EachDistinctPhraseIsReportedSeparately()
    {
        var email = Email()
            .WithSubject("Urgent action required")
            .WithBody("Your password has expired. Please confirm your identity.")
            .Build();

        _rule.Evaluate(email).Should().HaveCount(3);
    }

    [Fact]
    public void OrdinaryMessageProducesNoFindings()
    {
        var email = Email()
            .WithSubject("Обяд утре")
            .WithBody("Здравей, ще се видим ли утре в 13:00 за обяд?")
            .Build();

        _rule.Evaluate(email).Should().BeEmpty();
    }

    [Fact]
    public void EvidenceShowsPhraseInContext()
    {
        var email = Email().WithBody("Здравейте. Please verify your account веднага.").Build();

        var evidence = _rule.Evaluate(email).Single().Evidence;

        evidence.Should().Contain("verify your account");
        evidence.Should().StartWith("…").And.EndWith("…");
    }

    [Fact]
    public void EvidenceIsSingleLineEvenForMultilineBody()
    {
        // Находките се показват в списък — нов ред в доказателството би
        // счупил подредбата на картата в интерфейса.
        var email = Email().WithBody("Първи ред\nverify your account\nтрети ред").Build();

        _rule.Evaluate(email).Single().Evidence.Should().NotContain("\n");
    }
}
