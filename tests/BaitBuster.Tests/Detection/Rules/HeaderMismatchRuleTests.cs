using BaitBuster.Core.Detection.Rules;
using BaitBuster.Core.Models;
using FluentAssertions;
using static BaitBuster.Tests.TestSupport.EmailBuilder;

namespace BaitBuster.Tests.Detection.Rules;

/// <summary>
/// HDR-001 сравнява адресните header-и помежду им и чете резултата от
/// SPF/DKIM/DMARC. Това са най-техническите индикатори в системата —
/// подателят може да напише каквото си иска във From, но не и да мине
/// проверките на приемащия сървър.
/// </summary>
public class HeaderMismatchRuleTests
{
    private readonly HeaderMismatchRule _rule = new();

    [Fact]
    public void ReplyToOnDifferentDomainIsFinding()
    {
        var email = Email()
            .WithFrom("security@paypal.com")
            .WithReplyTo("attacker@evil-domain.ru")
            .Build();

        var findings = _rule.Evaluate(email).ToList();

        findings.Should().ContainSingle();
        findings[0].Category.Should().Be("Headers");
        findings[0].Evidence.Should().Contain("attacker@evil-domain.ru");
    }

    [Fact]
    public void ReplyToOnSameDomainIsClean()
    {
        var email = Email()
            .WithFrom("security@paypal.com")
            .WithReplyTo("support@paypal.com")
            .Build();

        _rule.Evaluate(email).Should().BeEmpty();
    }

    [Fact]
    public void DomainComparisonIsCaseInsensitive()
    {
        var email = Email()
            .WithFrom("security@PayPal.com")
            .WithReplyTo("support@paypal.COM")
            .Build();

        _rule.Evaluate(email).Should().BeEmpty();
    }

    [Fact]
    public void ReturnPathOnDifferentDomainIsFinding()
    {
        var email = Email()
            .WithFrom("security@paypal.com")
            .WithReturnPath("bounce@another-domain.cn")
            .Build();

        var findings = _rule.Evaluate(email).ToList();

        findings.Should().ContainSingle();
        findings[0].Evidence.Should().Contain("bounce@another-domain.cn");
    }

    [Theory]
    [InlineData("spf")]
    [InlineData("dkim")]
    [InlineData("dmarc")]
    public void FailedAuthenticationCheckIsHighSeverityFinding(string protocol)
    {
        var email = Email()
            .WithHeader("Authentication-Results", $"mx.example.com; {protocol}=fail")
            .Build();

        var findings = _rule.Evaluate(email).ToList();

        findings.Should().ContainSingle();
        findings[0].Severity.Should().Be(Severity.High);
        findings[0].Description.Should().Contain(protocol.ToUpperInvariant());
    }

    [Fact]
    public void EachFailedCheckIsReportedSeparately()
    {
        var email = Email()
            .WithHeader("Authentication-Results", "mx.example.com; spf=fail; dkim=fail; dmarc=fail")
            .Build();

        _rule.Evaluate(email).Should().HaveCount(3);
    }

    [Fact]
    public void PassedChecksProduceNoFindings()
    {
        var email = Email()
            .WithHeader("Authentication-Results", "mx.example.com; spf=pass; dkim=pass; dmarc=pass")
            .Build();

        _rule.Evaluate(email).Should().BeEmpty();
    }

    [Fact]
    public void WithoutSenderAddressRuleStaysSilent()
    {
        // Без From няма с какво да се сравняват другите header-и, а
        // предположение на сляпо би дало лъжлива тревога.
        var email = Email()
            .WithFrom("")
            .WithReplyTo("attacker@evil-domain.ru")
            .Build();

        _rule.Evaluate(email).Should().BeEmpty();
    }

    [Fact]
    public void DetectsMismatchInBothHeadersAtOnce()
    {
        var email = Email()
            .WithFrom("security@paypal.com")
            .WithReplyTo("attacker@evil-domain.ru")
            .WithReturnPath("bounce@another-domain.cn")
            .Build();

        _rule.Evaluate(email).Should().HaveCount(2);
    }
}
