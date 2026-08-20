using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace BaitBuster.Core.Detection.Ml;

/// <summary>
/// Привежда текста на имейл до един и същ вид преди да стигне до модела.
///
/// Използва се на две места и това е целта му: веднъж от BaitBuster.DataPrep,
/// когато се строи тренировъчният корпус, и веднъж от <c>MlClassifierRule</c>,
/// когато се анализира реален имейл. Ако двете обработки се разминат, моделът
/// вижда при работа думи, различни от тези, на които е учен (train/serve skew).
///
/// Отделно нормализацията изравнява разликите между корпусите-източници:
/// Enron и Ling са публикувани вече токенизирани (малки букви, отделена
/// пунктуация — „may 25 , 2001“), а Nazario и CEAS са сурови. Без изравняване
/// класификаторът може да се научи да разпознава корпуса вместо фишинга.
/// </summary>
public static partial class EmailTextNormalizer
{
    /// <summary>
    /// Горна граница на дължината. Опашката на много дълги съобщения е почти
    /// винаги цитирана кореспонденция или base64 остатъци и носи повече шум,
    /// отколкото сигнал — а и умножена по десетки хиляди редове тежи на паметта.
    /// </summary>
    public const int MaxLength = 8_000;

    /// <summary>Под тази дължина съобщението няма достатъчно текст за преценка.</summary>
    public const int MinLength = 20;

    public static string Normalize(string? subject, string? body)
        => Normalize($"{subject}\n{body}");

    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;

        var text = raw;

        // 1. HTML → видим текст. Стиловете и скриптовете отпадат изцяло,
        //    иначе CSS правилата се броят като „думи“.
        if (text.Contains('<'))
        {
            text = ScriptStyleRegex().Replace(text, " ");
            text = HtmlTagRegex().Replace(text, " ");
        }

        text = WebUtility.HtmlDecode(text);

        // 2. Заместители за неща, които са уникални за всеки имейл. Конкретният
        //    адрес или сума не се повтарят достатъчно, за да се научи тежест за
        //    тях — а присъствието им само по себе си е сигнал. Линковете имат
        //    отделно правило (URL-001), затова тук остава само следата от тях.
        text = UrlRegex().Replace(text, " __url__ ");
        text = EmailRegex().Replace(text, " __email__ ");
        text = MoneyRegex().Replace(text, " __money__ ");
        text = LongNumberRegex().Replace(text, " __num__ ");

        text = text.ToLowerInvariant();

        // 3. Изравняване на токенизацията между корпусите: „loser , you“ → „loser, you“.
        text = SpaceBeforePunctuationRegex().Replace(text, "$1");

        // 4. Кирилица, латиница, цифри, долна черта и основната пунктуация остават;
        //    всичко останало (контролни знаци, емоджи, декоративни рамки) става интервал.
        var sb = new StringBuilder(Math.Min(text.Length, MaxLength) + 16);
        var lastWasSpace = false;
        foreach (var ch in text)
        {
            var keep = char.IsLetterOrDigit(ch) || ch is '_' or '.' or ',' or '!' or '?' or '\'' or '-' or '$' or '%' or '@';
            if (keep)
            {
                sb.Append(ch);
                lastWasSpace = false;
            }
            else if (!lastWasSpace)
            {
                sb.Append(' ');
                lastWasSpace = true;
            }

            if (sb.Length >= MaxLength)
                break;
        }

        return sb.ToString().Trim();
    }

    /// <summary>Достатъчно ли е съобщението, за да влезе в корпуса или в оценка?</summary>
    public static bool IsUsable(string normalized) => normalized.Length >= MinLength;

    [GeneratedRegex(@"<(script|style)\b[^>]*>.*?</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptStyleRegex();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"(https?://|www\.)[^\s""'<>]+", RegexOptions.IgnoreCase)]
    private static partial Regex UrlRegex();

    [GeneratedRegex(@"[\w.+-]+@[\w-]+\.[\w.-]+", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    [GeneratedRegex(@"[$€£]\s?\d[\d.,]*|\b\d[\d.,]*\s?(usd|eur|gbp|dollars?|euros?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex MoneyRegex();

    [GeneratedRegex(@"\b\d{4,}\b")]
    private static partial Regex LongNumberRegex();

    [GeneratedRegex(@"\s+([.,!?])")]
    private static partial Regex SpaceBeforePunctuationRegex();
}
