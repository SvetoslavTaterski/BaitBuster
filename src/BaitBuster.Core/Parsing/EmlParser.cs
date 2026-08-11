using System.Text.RegularExpressions;
using BaitBuster.Core.Models;
using MimeKit;

namespace BaitBuster.Core.Parsing;

/// <summary>
/// Парсва .eml (RFC 5322 / MIME) файл до нормализиран <see cref="ParsedEmail"/>.
/// Използва MimeKit — де факто стандартът за .NET.
/// </summary>
public sealed partial class EmlParser
{
    public ParsedEmail Parse(Stream emlStream)
    {
        var message = MimeMessage.Load(emlStream);

        var from = message.From.Mailboxes.FirstOrDefault();
        var replyTo = message.ReplyTo.Mailboxes.FirstOrDefault();

        var headers = message.Headers
            .GroupBy(h => h.Field, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => string.Join("; ", g.Select(h => h.Value)),
                          StringComparer.OrdinalIgnoreCase);

        var bodyHtml = message.HtmlBody;
        var bodyText = message.TextBody
                       ?? (bodyHtml is null ? string.Empty : StripHtml(bodyHtml));

        return new ParsedEmail
        {
            Subject = message.Subject ?? string.Empty,
            FromDisplayName = from?.Name ?? string.Empty,
            FromAddress = from?.Address ?? string.Empty,
            ReplyToAddress = replyTo?.Address,
            ReturnPath = headers.GetValueOrDefault("Return-Path")?.Trim('<', '>', ' '),
            Headers = headers,
            BodyText = bodyText,
            BodyHtml = bodyHtml,
            Links = ExtractLinks(bodyHtml, bodyText),
            AttachmentNames = message.Attachments
                .Select(a => a is MimePart p ? p.FileName ?? "(без име)" : "(вложено съобщение)")
                .ToList()
        };
    }

    private static List<EmailLink> ExtractLinks(string? html, string plainText)
    {
        var links = new List<EmailLink>();

        if (html is not null)
        {
            // <a href="...">текст</a> — показван текст срещу реален href
            foreach (Match m in AnchorRegex().Matches(html))
            {
                var href = m.Groups["href"].Value.Trim();
                var text = StripHtml(m.Groups["text"].Value).Trim();
                if (href.Length > 0)
                    links.Add(new EmailLink(text, href));
            }
        }

        // "голи" URL-и в plain text частта
        foreach (Match m in UrlRegex().Matches(plainText))
        {
            var url = m.Value;
            if (!links.Any(l => l.Href == url))
                links.Add(new EmailLink(url, url));
        }

        return links;
    }

    private static string StripHtml(string html) =>
        HtmlTagRegex().Replace(html, " ").Trim();

    [GeneratedRegex("""<a\s[^>]*href\s*=\s*["'](?<href>[^"']+)["'][^>]*>(?<text>.*?)</a>""",
        RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex AnchorRegex();

    [GeneratedRegex(@"https?://[^\s""'<>]+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();
}
