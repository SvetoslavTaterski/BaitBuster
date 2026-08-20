namespace BaitBuster.DataPrep;

/// <summary>
/// Един корпус-източник от сборника „Phishing Email Curated Datasets“
/// (Zenodo 8339691, Al-Subaiey et al., 2024).
/// </summary>
/// <param name="FileName">Име на CSV файла в data/raw/zenodo.</param>
/// <param name="Name">Кратко име, с което източникът се води в отчета.</param>
/// <param name="Kind">Какво означава етикет 1 в този корпус.</param>
internal sealed record SourceSpec(string FileName, string Name, string Kind);

internal static class Sources
{
    /// <summary>
    /// Шестте корпуса и какво представлява положителният клас във всеки.
    /// Разликата има значение: Enron и Ling внасят предимно легитимна поща,
    /// а Nazario и Nigerian_Fraud — почти изцяло атаки. Ако корпус се добави
    /// или махне, съставът на данните се променя — затова отчетът разбива
    /// метриките по източник.
    /// </summary>
    public static readonly SourceSpec[] All =
    [
        new("Enron.csv",           "Enron",           "спам в корпоративна поща"),
        new("Ling.csv",            "Ling",            "спам в пощенски списък"),
        new("SpamAssasin.csv",     "SpamAssassin",    "спам"),
        new("CEAS_08.csv",         "CEAS_08",         "фишинг/спам"),
        new("Nazario.csv",         "Nazario",         "фишинг"),
        new("Nigerian_Fraud.csv",  "Nigerian_Fraud",  "измама с авансово плащане"),
    ];

    /// <summary>
    /// Служебни съобщения, които са попаднали в корпусите заедно с истинските
    /// имейли. Носят етикета на папката, в която са лежали, но не са атака —
    /// ако останат, моделът учи текста на mail сървъра като „фишинг“.
    /// </summary>
    public static readonly string[] JunkMarkers =
    [
        "folder internal data",
        "this text is part of the internal format of your mail folder",
    ];

    public static bool IsJunk(string normalizedText)
    {
        foreach (var marker in JunkMarkers)
            if (normalizedText.Contains(marker, StringComparison.Ordinal))
                return true;
        return false;
    }
}
