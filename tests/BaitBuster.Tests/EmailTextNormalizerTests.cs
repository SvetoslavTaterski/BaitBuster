using BaitBuster.Core.Detection.Ml;
using FluentAssertions;

namespace BaitBuster.Tests;

/// <summary>
/// Нормализаторът стои между корпуса и модела и се вика както при обучение,
/// така и при анализ на реален имейл. Ако поведението му се промени, моделът
/// започва да вижда различни думи от тези, на които е учен — затова
/// правилата му са заковани с тестове.
/// </summary>
public class EmailTextNormalizerTests
{
    [Fact]
    public void Премахва_html_и_оставя_видимия_текст()
    {
        var result = EmailTextNormalizer.Normalize(
            "Важно", "<p>Вашият <b>акаунт</b> е блокиран</p>");

        result.Should().Contain("акаунт");
        result.Should().NotContain("<");
        result.Should().NotContain("p>");
    }

    [Fact]
    public void Изхвърля_съдържанието_на_script_и_style()
    {
        var result = EmailTextNormalizer.Normalize(
            null, "<style>.a{color:red}</style><script>alert(1)</script>Здравей");

        result.Should().Be("здравей");
    }

    [Fact]
    public void Заменя_линкове_адреси_и_суми_със_заместители()
    {
        var result = EmailTextNormalizer.Normalize(
            null, "Пишете на support@paypal-secure.tk или отворете https://evil.example/login за $250");

        result.Should().Contain("__email__");
        result.Should().Contain("__url__");
        result.Should().Contain("__money__");
        result.Should().NotContain("evil.example");
    }

    [Fact]
    public void Декодира_html_същности()
    {
        var result = EmailTextNormalizer.Normalize(null, "Tom &amp; Jerry &mdash; среща");

        result.Should().Contain("tom");
        result.Should().Contain("jerry");
        result.Should().NotContain("amp");
    }

    [Fact]
    public void Изравнява_токенизацията_между_корпусите()
    {
        // Enron и Ling са публикувани с отделена пунктуация, останалите не са.
        // След нормализация двата варианта трябва да съвпаднат, иначе моделът
        // може да разпознава корпуса вместо фишинга.
        var preTokenized = EmailTextNormalizer.Normalize(null, "hello , world . how are you ?");
        var raw = EmailTextNormalizer.Normalize(null, "Hello, world. How are you?");

        preTokenized.Should().Be(raw);
    }

    [Fact]
    public void Не_пропуска_табулации_и_нови_редове_защото_корпусът_е_tsv()
    {
        var result = EmailTextNormalizer.Normalize("Тема\tс таб", "тяло\nна\rдва реда");

        result.Should().NotContain("\t");
        result.Should().NotContain("\n");
        result.Should().NotContain("\r");
    }

    [Fact]
    public void Реже_прекалено_дългите_съобщения()
    {
        var result = EmailTextNormalizer.Normalize(null, new string('a', EmailTextNormalizer.MaxLength * 3));

        result.Length.Should().BeLessThanOrEqualTo(EmailTextNormalizer.MaxLength);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("<p></p>")]
    public void Празният_вход_дава_неизползваем_резултат(string? body)
    {
        var result = EmailTextNormalizer.Normalize(null, body);

        EmailTextNormalizer.IsUsable(result).Should().BeFalse();
    }

    [Fact]
    public void Кратките_съобщения_не_стигат_за_преценка()
    {
        EmailTextNormalizer.IsUsable(EmailTextNormalizer.Normalize(null, "ok thanks")).Should().BeFalse();
        EmailTextNormalizer.IsUsable(EmailTextNormalizer.Normalize(
            "Verify your account", "Your account will be suspended within 24 hours.")).Should().BeTrue();
    }
}
