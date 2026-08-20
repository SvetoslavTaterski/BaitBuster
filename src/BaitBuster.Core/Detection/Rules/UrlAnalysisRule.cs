using System.Text.RegularExpressions;
using BaitBuster.Core.Models;

namespace BaitBuster.Core.Detection.Rules;

/// <summary>
/// URL-001: Анализ на линковете в имейла — IP вместо домейн, URL shortener-и,
/// несъответствие между показван текст и реален href.
/// </summary>
public sealed partial class UrlAnalysisRule : IDetectionRule
{
    public string RuleId => "URL-001";
    public string Name => "Анализ на линковете";
    public string Category => "Urls";
    public int MaxScore => 25;

    public string Description =>
        "Проверява всеки линк в имейла: IP адрес вместо домейн, съкратени URL-и, " +
        "разминаване между показания текст и реалната дестинация, както и " +
        "нешифрована HTTP връзка.";

    private static readonly HashSet<string> Shorteners = new(StringComparer.OrdinalIgnoreCase)
    {
        "bit.ly", "tinyurl.com", "goo.gl", "t.co", "is.gd", "cutt.ly", "rb.gy", "shorturl.at"
    };

    public IEnumerable<Finding> Evaluate(ParsedEmail email)
    {
        foreach (var link in email.Links)
        {
            if (!Uri.TryCreate(link.Href, UriKind.Absolute, out var uri))
                continue;

            var host = uri.Host.ToLowerInvariant();

            if (IpHostRegex().IsMatch(host))
            {
                yield return new Finding(RuleId, "Urls", Severity.High, 25,
                    "Линк с IP адрес вместо домейн — легитимните услуги не правят това.",
                    link.Href);
            }

            if (Shorteners.Contains(host))
            {
                yield return new Finding(RuleId, "Urls", Severity.Medium, 10,
                    "Съкратен URL — крайната дестинация е скрита от потребителя.",
                    link.Href);
            }

            // Показваният текст изглежда като URL, но сочи към друг хост
            if (Uri.TryCreate(link.DisplayText.Trim(), UriKind.Absolute, out var displayUri)
                && !displayUri.Host.Equals(uri.Host, StringComparison.OrdinalIgnoreCase))
            {
                yield return new Finding(RuleId, "Urls", Severity.High, 25,
                    "Показаният адрес се различава от реалната дестинация на линка.",
                    $"Показва: {displayUri.Host} · Реално: {uri.Host}");
            }

            if (!uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
            {
                yield return new Finding(RuleId, "Urls", Severity.Low, 5,
                    "Линкът използва нешифрована HTTP връзка.",
                    link.Href);
            }
        }
    }

    [GeneratedRegex(@"^\d{1,3}(\.\d{1,3}){3}$")]
    private static partial Regex IpHostRegex();
}
