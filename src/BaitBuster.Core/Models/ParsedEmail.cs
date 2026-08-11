namespace BaitBuster.Core.Models;

/// <summary>
/// Нормализирано представяне на имейл след парсване на .eml файла.
/// Детекционните правила работят само върху този модел — не върху суровия MIME.
/// </summary>
public sealed class ParsedEmail
{
    public required string Subject { get; init; }
    public required string FromDisplayName { get; init; }
    public required string FromAddress { get; init; }
    public string? ReplyToAddress { get; init; }
    public string? ReturnPath { get; init; }

    /// <summary>Всички header-и (име → стойност), за правила по header-ите.</summary>
    public required IReadOnlyDictionary<string, string> Headers { get; init; }

    /// <summary>Чист текст на тялото (plain text или конвертиран от HTML).</summary>
    public required string BodyText { get; init; }

    /// <summary>Суров HTML на тялото, ако има такъв.</summary>
    public string? BodyHtml { get; init; }

    /// <summary>Извлечени линкове: (показван текст, реален href).</summary>
    public required IReadOnlyList<EmailLink> Links { get; init; }

    /// <summary>Имена на прикачените файлове.</summary>
    public required IReadOnlyList<string> AttachmentNames { get; init; }
}

public sealed record EmailLink(string DisplayText, string Href);
