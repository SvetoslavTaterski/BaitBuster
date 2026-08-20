using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using BaitBuster.Core.Detection.Ml;
using BaitBuster.DataPrep;
using CsvHelper;
using CsvHelper.Configuration;

// Строи тренировъчния корпус от суровите CSV файлове в data/raw/zenodo.
// Резултатът е един TSV с три колони (Label, Text, Source), готов за
// BaitBuster.MlTraining, плюс JSON отчет какво точно е влязло в него.
//
// Употреба:
//   dotnet run --project tools/BaitBuster.DataPrep -- <rawDir> <outputTsv>

var rawDir = args.ElementAtOrDefault(0)
    ?? throw new ArgumentException("Първи аргумент: папка със суровите CSV файлове.");
var outputPath = args.ElementAtOrDefault(1)
    ?? throw new ArgumentException("Втори аргумент: път до изходния .tsv файл.");

var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    HasHeaderRecord = true,
    // Корпусите са събирани от различни хора през различни години и съдържат
    // редове с непарни кавички. Прескачаме ги един по един, вместо да падаме.
    BadDataFound = null,
    MissingFieldFound = null,
    DetectColumnCountChanges = false,
};

// Хешовете на вече видените съобщения. Пазим само отпечатък, не самия текст —
// иначе корпусът стои двойно в паметта.
var seen = new HashSet<string>(StringComparer.Ordinal);
var rows = new List<CorpusRow>(100_000);
var sourceReports = new List<SourceReport>();

int totalRead = 0, totalShort = 0, totalJunk = 0, totalDup = 0;

foreach (var source in Sources.All)
{
    var path = Path.Combine(rawDir, source.FileName);
    if (!File.Exists(path))
    {
        Console.WriteLine($"[!] Пропускам {source.Name} — липсва {path}");
        continue;
    }

    Console.Write($"{source.Name,-16} ");

    int read = 0, kept = 0, phishing = 0, legit = 0, tooShort = 0, junk = 0, dup = 0;
    var lengths = new List<int>();

    using var reader = new StreamReader(path, Encoding.UTF8);
    using var csv = new CsvReader(reader, csvConfig);

    csv.Read();
    csv.ReadHeader();

    while (true)
    {
        // Отделен try за реда: един счупен ред не бива да спира целия корпус.
        try
        {
            if (!csv.Read())
                break;
        }
        catch (CsvHelperException)
        {
            continue;
        }

        read++;

        csv.TryGetField<string>("subject", out var subject);
        csv.TryGetField<string>("body", out var body);
        if (!csv.TryGetField<string>("label", out var labelRaw))
            continue;

        if (!int.TryParse(labelRaw?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var labelValue))
            continue;

        var text = EmailTextNormalizer.Normalize(subject, body);

        if (!EmailTextNormalizer.IsUsable(text))
        {
            tooShort++;
            continue;
        }

        if (Sources.IsJunk(text))
        {
            junk++;
            continue;
        }

        // Дедупликацията е през целия корпус, не в рамките на един файл:
        // корпусите се препокриват (SpamAssassin присъства и самостоятелно,
        // и вътре в CEAS). Един и същ имейл от двете страни на train/test
        // границата прави оценката безсмислена.
        if (!seen.Add(Fingerprint(text)))
        {
            dup++;
            continue;
        }

        var isPhishing = labelValue == 1;
        rows.Add(new CorpusRow(isPhishing, text, source.Name));
        lengths.Add(text.Length);
        kept++;
        if (isPhishing) phishing++; else legit++;
    }

    totalRead += read;
    totalShort += tooShort;
    totalJunk += junk;
    totalDup += dup;

    sourceReports.Add(new SourceReport(
        source.Name, source.Kind, read, kept, phishing, legit,
        tooShort, junk, dup, Median(lengths)));

    Console.WriteLine($"прочетени {read,6} → запазени {kept,6}  " +
                      $"({phishing} фишинг / {legit} легитимни, " +
                      $"отпаднали: {tooShort} къси, {junk} служебни, {dup} дублирани)");
}

if (rows.Count == 0)
    throw new InvalidOperationException("Нито един ред не беше зареден — проверете пътя до данните.");

// Разбъркване с фиксирано семе: файлът излиза смесен вместо групиран по
// източник, а редът е един и същ при всяко пускане (възпроизводимост).
var rng = new Random(42);
for (var i = rows.Count - 1; i > 0; i--)
{
    var j = rng.Next(i + 1);
    (rows[i], rows[j]) = (rows[j], rows[i]);
}

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);

await using (var writer = new StreamWriter(outputPath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
{
    await writer.WriteLineAsync(string.Join('\t', "Label", "Text", "Source"));
    foreach (var row in rows)
        await writer.WriteLineAsync(string.Join('\t', row.IsPhishing ? "1" : "0", row.Text, row.Source));
}

var phishingTotal = rows.Count(r => r.IsPhishing);

var report = new DatasetReport(
    BuiltAt: DateTimeOffset.UtcNow,
    NormalizationVersion: $"EmailTextNormalizer/max={EmailTextNormalizer.MaxLength}",
    TotalRowsRead: totalRead,
    TotalKept: rows.Count,
    DroppedTooShort: totalShort,
    DroppedJunk: totalJunk,
    DroppedDuplicate: totalDup,
    PhishingKept: phishingTotal,
    LegitimateKept: rows.Count - phishingTotal,
    Sources: sourceReports);

var reportPath = Path.ChangeExtension(outputPath, ".report.json");
await File.WriteAllTextAsync(reportPath,
    JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));

Console.WriteLine();
Console.WriteLine($"Общо {rows.Count} примера — {phishingTotal} фишинг, {rows.Count - phishingTotal} легитимни " +
                  $"({(double)phishingTotal / rows.Count:P1} положителен клас)");
Console.WriteLine($"Корпусът е записан в {outputPath}");
Console.WriteLine($"Отчетът е записан в {reportPath}");

static string Fingerprint(string text)
{
    Span<byte> hash = stackalloc byte[32];
    SHA256.HashData(Encoding.UTF8.GetBytes(text), hash);
    return Convert.ToHexString(hash[..16]);
}

static int Median(List<int> values)
{
    if (values.Count == 0)
        return 0;
    values.Sort();
    return values[values.Count / 2];
}

internal sealed record CorpusRow(bool IsPhishing, string Text, string Source);
