using System.Text;
using BaitBuster.Core.Parsing;
using FluentAssertions;

namespace BaitBuster.Tests.Parsing;

/// <summary>
/// Парсерът е входната врата на системата — всичко след него работи само
/// върху ParsedEmail. Ако тук нещо се загуби, правилата няма как да го
/// открият, колкото и добри да са.
/// </summary>
public class EmlParserTests
{
    private readonly EmlParser _parser = new();

    private Core.Models.ParsedEmail Parse(string raw)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(raw));
        return _parser.Parse(stream);
    }

    [Fact]
    public void ExtractsSubjectAndSender()
    {
        var email = Parse("""
            From: "PayPal Security" <security@paypa1-support.com>
            Subject: Urgent Action Required
            Content-Type: text/plain; charset="UTF-8"

            Тяло на съобщението.
            """);

        email.Subject.Should().Be("Urgent Action Required");
        email.FromAddress.Should().Be("security@paypa1-support.com");
        email.FromDisplayName.Should().Be("PayPal Security");
    }

    [Fact]
    public void ExtractsReplyToAndReturnPathWithoutAngleBrackets()
    {
        var email = Parse("""
            From: sender@example.com
            Reply-To: attacker@evil-domain.ru
            Return-Path: <bounce@another-domain.cn>
            Subject: Тест

            Тяло.
            """);

        email.ReplyToAddress.Should().Be("attacker@evil-domain.ru");
        email.ReturnPath.Should().Be("bounce@another-domain.cn");
    }

    [Fact]
    public void HeaderLookupIsCaseInsensitive()
    {
        var email = Parse("""
            From: sender@example.com
            Subject: Тест
            Authentication-Results: mx.example.com; spf=fail

            Тяло.
            """);

        email.Headers.ContainsKey("authentication-results").Should().BeTrue();
        email.Headers["AUTHENTICATION-RESULTS"].Should().Contain("spf=fail");
    }

    [Fact]
    public void SeparatesDisplayTextFromActualLinkTarget()
    {
        var email = Parse("""
            From: sender@example.com
            Subject: Тест
            Content-Type: text/html; charset="UTF-8"

            <html><body><a href="http://192.168.5.23/login">https://www.paypal.com/login</a></body></html>
            """);

        email.Links.Should().Contain(l =>
            l.Href == "http://192.168.5.23/login" &&
            l.DisplayText == "https://www.paypal.com/login");
    }

    [Fact]
    public void ExtractsBareUrlsFromPlainTextPart()
    {
        var email = Parse("""
            From: sender@example.com
            Subject: Тест
            Content-Type: text/plain; charset="UTF-8"

            Отворете https://example.com/promo за подробности.
            """);

        email.Links.Should().Contain(l => l.Href == "https://example.com/promo");
    }

    [Fact]
    public void FallsBackToStrippedHtmlWhenNoPlainTextPart()
    {
        var email = Parse("""
            From: sender@example.com
            Subject: Тест
            Content-Type: text/html; charset="UTF-8"

            <html><body><p>Вашият <b>акаунт</b> е блокиран</p></body></html>
            """);

        email.BodyText.Should().Contain("акаунт");
        email.BodyText.Should().NotContain("<b>");
        email.BodyHtml.Should().NotBeNull();
    }

    [Fact]
    public void ListsAttachmentFileNames()
    {
        var email = Parse("""
            From: sender@example.com
            Subject: Тест
            Content-Type: multipart/mixed; boundary="граница"

            --граница
            Content-Type: text/plain; charset="UTF-8"

            Виж прикачения файл.
            --граница
            Content-Type: application/pdf; name="фактура.pdf"
            Content-Disposition: attachment; filename="фактура.pdf"
            Content-Transfer-Encoding: base64

            SGVsbG8=
            --граница--
            """);

        email.AttachmentNames.Should().Contain("фактура.pdf");
    }

    [Fact]
    public void EmailWithoutSubjectOrBodyDoesNotBreakParser()
    {
        var email = Parse("""
            From: sender@example.com

            """);

        email.Subject.Should().BeEmpty();
        email.BodyText.Should().BeEmpty();
        email.Links.Should().BeEmpty();
        email.AttachmentNames.Should().BeEmpty();
    }
}
