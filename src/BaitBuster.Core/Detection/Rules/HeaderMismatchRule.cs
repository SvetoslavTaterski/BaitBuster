using BaitBuster.Core.Models;

namespace BaitBuster.Core.Detection.Rules;

/// <summary>
/// HDR-001: Несъответствия в адресните header-и — класически фишинг индикатор.
/// Проверява From срещу Reply-To и Return-Path (различен домейн = червен флаг),
/// както и резултата от Authentication-Results (SPF/DKIM/DMARC), ако е наличен.
/// </summary>
public sealed class HeaderMismatchRule : IDetectionRule
{
    public string RuleId => "HDR-001";

    public IEnumerable<Finding> Evaluate(ParsedEmail email)
    {
        var fromDomain = DomainOf(email.FromAddress);
        if (fromDomain is null)
            yield break;

        var replyToDomain = DomainOf(email.ReplyToAddress);
        if (replyToDomain is not null && replyToDomain != fromDomain)
        {
            yield return new Finding(RuleId, "Headers", Severity.Medium, 15,
                "Reply-To сочи към различен домейн от подателя — отговорите отиват другаде.",
                $"From: {email.FromAddress} · Reply-To: {email.ReplyToAddress}");
        }

        var returnDomain = DomainOf(email.ReturnPath);
        if (returnDomain is not null && returnDomain != fromDomain)
        {
            yield return new Finding(RuleId, "Headers", Severity.Medium, 15,
                "Return-Path е от различен домейн — вероятно подправен подател.",
                $"From: {email.FromAddress} · Return-Path: {email.ReturnPath}");
        }

        // Authentication-Results се добавя от приемащия сървър; ако присъства,
        // fail на SPF/DKIM/DMARC е силен технически индикатор.
        if (email.Headers.TryGetValue("Authentication-Results", out var auth))
        {
            foreach (var proto in (string[])["spf", "dkim", "dmarc"])
            {
                if (auth.Contains($"{proto}=fail", StringComparison.OrdinalIgnoreCase))
                {
                    yield return new Finding(RuleId, "Headers", Severity.High, 25,
                        $"Имейлът не преминава {proto.ToUpperInvariant()} проверка.",
                        Truncate(auth, 160));
                }
            }
        }
    }

    private static string? DomainOf(string? address)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        var at = address.LastIndexOf('@');
        return at < 0 ? null : address[(at + 1)..].ToLowerInvariant();
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}
