using BaitBuster.Core.Detection.Rules;
using BaitBuster.Core.Models;
using FluentAssertions;
using static BaitBuster.Tests.TestSupport.EmailBuilder;

namespace BaitBuster.Tests.Detection.Rules;

/// <summary>
/// URL-001 проверява линковете — мястото, където жертвата всъщност кликва.
/// Най-важният случай е разминаването между показания текст и реалната
/// дестинация, защото точно то заблуждава потребителя.
/// </summary>
public class UrlAnalysisRuleTests
{
    private readonly UrlAnalysisRule _rule = new();

    [Fact]
    public void LinkWithIpAddressInsteadOfDomainIsHighSeverity()
    {
        var email = Email().WithLink("Влез тук", "http://192.168.5.23/login").Build();

        var findings = _rule.Evaluate(email).ToList();

        findings.Should().Contain(f => f.Severity == Severity.High && f.Description.Contains("IP адрес"));
    }

    [Fact]
    public void ShortenedUrlIsFinding()
    {
        var email = Email().WithLink("Виж", "https://bit.ly/3xYzAbC").Build();

        var findings = _rule.Evaluate(email).ToList();

        findings.Should().ContainSingle();
        findings[0].Description.Should().Contain("Съкратен");
    }

    [Fact]
    public void DisplayedAddressDifferentFromRealIsHighSeverity()
    {
        var email = Email()
            .WithLink("https://www.paypal.com/login", "https://evil.example/login")
            .Build();

        var findings = _rule.Evaluate(email).ToList();

        findings.Should().ContainSingle();
        findings[0].Severity.Should().Be(Severity.High);
        findings[0].Evidence.Should().Contain("www.paypal.com").And.Contain("evil.example");
    }

    [Fact]
    public void UnencryptedConnectionIsLowSeverity()
    {
        var email = Email().WithLink("Начало", "http://example.com/page").Build();

        var findings = _rule.Evaluate(email).ToList();

        findings.Should().ContainSingle();
        findings[0].Severity.Should().Be(Severity.Low);
    }

    [Fact]
    public void PlainHttpsLinkIsClean()
    {
        var email = Email().WithLink("Начало", "https://example.com/page").Build();

        _rule.Evaluate(email).Should().BeEmpty();
    }

    [Fact]
    public void NonUrlDisplayTextIsNotTreatedAsMismatch()
    {
        // Обикновен текст като „Натисни тук" не е URL, така че няма какво
        // да се сравнява — иначе всеки нормален линк би давал лъжлива тревога.
        var email = Email().WithLink("Натисни тук", "https://example.com/page").Build();

        _rule.Evaluate(email).Should().BeEmpty();
    }

    [Fact]
    public void InvalidUrlIsSkippedWithoutError()
    {
        var email = Email().WithLink("Странен", "не-е-адрес").Build();

        _rule.Evaluate(email).Should().BeEmpty();
    }

    [Fact]
    public void SingleLinkCanProduceMultipleFindings()
    {
        // IP адрес + нешифрована връзка + подменен показан текст.
        var email = Email()
            .WithLink("https://www.paypal.com/login", "http://192.168.5.23/login")
            .Build();

        var findings = _rule.Evaluate(email).ToList();

        findings.Should().HaveCount(3);
        findings.Sum(f => f.Score).Should().Be(55);
    }

    [Fact]
    public void ChecksEachLinkIndependently()
    {
        var email = Email()
            .WithLink("Първи", "https://bit.ly/aaa")
            .WithLink("Втори", "https://tinyurl.com/bbb")
            .Build();

        _rule.Evaluate(email).Should().HaveCount(2);
    }

    [Fact]
    public void EmailWithoutLinksProducesNoFindings()
    {
        _rule.Evaluate(Email().Build()).Should().BeEmpty();
    }
}
