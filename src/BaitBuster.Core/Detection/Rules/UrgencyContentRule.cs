using BaitBuster.Core.Models;

namespace BaitBuster.Core.Detection.Rules;

/// <summary>
/// CNT-001: Съдържателни индикатори — език на спешност и натиск,
/// характерен за социалното инженерство. Списъкът е стартов и
/// ще се разширява (вкл. с български фрази) в хода на разработката.
/// </summary>
public sealed class UrgencyContentRule : IDetectionRule
{
    public string RuleId => "CNT-001";
    public string Name => "Език на спешност и натиск";
    public string Category => "Content";
    public int MaxScore => 10;

    public string Description =>
        "Търси в темата и тялото фрази, характерни за социалното инженерство — " +
        "заплаха за спиране на акаунта, кратък срок, искане за потвърждаване на " +
        "самоличност. Списъкът покрива английски и български.";

    private static readonly string[] UrgencyPhrases =
    [
        // EN
        "verify your account", "account suspended", "urgent action",
        "click here immediately", "your password has expired",
        "unusual sign-in activity", "confirm your identity",
        // BG
        "акаунтът ви ще бъде спрян", "потвърдете данните си",
        "незабавно", "изтича до 24 часа", "верифицирайте профила си",
        "открихме подозрителна активност"
    ];

    public IEnumerable<Finding> Evaluate(ParsedEmail email)
    {
        var text = $"{email.Subject}\n{email.BodyText}";

        foreach (var phrase in UrgencyPhrases)
        {
            var idx = text.IndexOf(phrase, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) continue;

            yield return new Finding(RuleId, "Content", Severity.Medium, 10,
                "Език на спешност/натиск — типична техника на социално инженерство.",
                ExtractContext(text, idx, phrase.Length));
        }
    }

    /// <summary>Връща фразата с малко контекст около нея като доказателство.</summary>
    private static string ExtractContext(string text, int index, int length)
    {
        const int pad = 30;
        var start = Math.Max(0, index - pad);
        var end = Math.Min(text.Length, index + length + pad);
        var snippet = text[start..end].ReplaceLineEndings(" ").Trim();
        return $"…{snippet}…";
    }
}
