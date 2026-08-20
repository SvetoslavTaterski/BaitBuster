using BaitBuster.Core.Models;

namespace BaitBuster.Tests.TestSupport;

/// <summary>
/// Сглобява <see cref="ParsedEmail"/> за тестовете. Моделът има много
/// задължителни полета, а всеки тест се интересува само от едно-две от тях —
/// builder-ът дава разумни стойности по подразбиране, за да остане в теста
/// видимо само това, което той всъщност проверява.
/// </summary>
internal sealed class EmailBuilder
{
    private string _subject = "Обикновена тема";
    private string _fromDisplayName = "Подател";
    private string _fromAddress = "sender@example.com";
    private string? _replyToAddress;
    private string? _returnPath;
    private string _bodyText = "Обикновено съдържание без индикатори.";
    private string? _bodyHtml;

    private readonly Dictionary<string, string> _headers = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<EmailLink> _links = [];
    private readonly List<string> _attachmentNames = [];

    public static EmailBuilder Email() => new();

    public EmailBuilder WithSubject(string subject)
    {
        _subject = subject;
        return this;
    }

    public EmailBuilder WithFrom(string address, string displayName = "Подател")
    {
        _fromAddress = address;
        _fromDisplayName = displayName;
        return this;
    }

    public EmailBuilder WithReplyTo(string? address)
    {
        _replyToAddress = address;
        return this;
    }

    public EmailBuilder WithReturnPath(string? returnPath)
    {
        _returnPath = returnPath;
        return this;
    }

    public EmailBuilder WithHeader(string name, string value)
    {
        _headers[name] = value;
        return this;
    }

    public EmailBuilder WithBody(string bodyText, string? bodyHtml = null)
    {
        _bodyText = bodyText;
        _bodyHtml = bodyHtml;
        return this;
    }

    /// <param name="displayText">Текстът, който потребителят вижда.</param>
    /// <param name="href">Адресът, към който линкът наистина води.</param>
    public EmailBuilder WithLink(string displayText, string href)
    {
        _links.Add(new EmailLink(displayText, href));
        return this;
    }

    public EmailBuilder WithAttachment(string fileName)
    {
        _attachmentNames.Add(fileName);
        return this;
    }

    public ParsedEmail Build() => new()
    {
        Subject = _subject,
        FromDisplayName = _fromDisplayName,
        FromAddress = _fromAddress,
        ReplyToAddress = _replyToAddress,
        ReturnPath = _returnPath,
        Headers = _headers,
        BodyText = _bodyText,
        BodyHtml = _bodyHtml,
        Links = _links,
        AttachmentNames = _attachmentNames
    };
}
