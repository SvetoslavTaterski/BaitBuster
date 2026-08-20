using System.Text;

namespace BaitBuster.MlTraining;

/// <summary>Един ред от корпуса заедно с корпуса-източник, от който идва.</summary>
internal sealed record CorpusRow(bool IsPhishing, string Text, string Source);

internal static class Corpus
{
    private const string UnknownSource = "неизвестен";

    /// <summary>
    /// Чете TSV файла по имена на колони, а не по позиция — така работи и с
    /// новия корпус (Label, Text, Source), и със стария файл без колона Source.
    /// </summary>
    public static List<CorpusRow> Load(string path)
    {
        using var reader = new StreamReader(path, Encoding.UTF8);

        var header = reader.ReadLine()
            ?? throw new InvalidDataException($"{path} е празен.");

        var columns = header.TrimStart('\uFEFF').Split('\t');
        var labelIndex = Array.FindIndex(columns, c => c.Equals("Label", StringComparison.OrdinalIgnoreCase));
        var textIndex = Array.FindIndex(columns, c => c.Equals("Text", StringComparison.OrdinalIgnoreCase));
        var sourceIndex = Array.FindIndex(columns, c => c.Equals("Source", StringComparison.OrdinalIgnoreCase));

        if (labelIndex < 0 || textIndex < 0)
            throw new InvalidDataException($"{path} трябва да има колони Label и Text.");

        var required = Math.Max(labelIndex, Math.Max(textIndex, sourceIndex)) + 1;
        var rows = new List<CorpusRow>(100_000);

        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0)
                continue;

            var fields = line.Split('\t');
            if (fields.Length < required)
                continue;

            var label = fields[labelIndex].Trim();
            var isPhishing = label is "1" or "true" or "True";

            rows.Add(new CorpusRow(
                isPhishing,
                fields[textIndex],
                sourceIndex >= 0 ? fields[sourceIndex] : UnknownSource));
        }

        return rows;
    }

    /// <summary>
    /// Разделя на обучение и тест, като запазва пропорциите вътре във всяка
    /// двойка (източник, етикет). Обикновеното случайно разделяне би могло да
    /// сложи например почти целия Nazario в тестовата част — тогава метриките
    /// щяха да отразяват късмета на разделянето, а не модела.
    /// </summary>
    public static (List<CorpusRow> Train, List<CorpusRow> Test) StratifiedSplit(
        IReadOnlyList<CorpusRow> rows, double testFraction, int seed)
    {
        var train = new List<CorpusRow>(rows.Count);
        var test = new List<CorpusRow>((int)(rows.Count * testFraction) + 16);

        // Групите се подреждат по име, а семето се извежда от поредността им.
        // GetHashCode() на string е рандомизиран за всеки процес и не става
        // за целта — разделянето трябва да е едно и също при всяко пускане.
        var strata = rows
            .GroupBy(r => (r.Source, r.IsPhishing))
            .OrderBy(g => g.Key.Source, StringComparer.Ordinal)
            .ThenBy(g => g.Key.IsPhishing)
            .ToList();

        for (var s = 0; s < strata.Count; s++)
        {
            var rng = new Random(seed + s);
            var shuffled = strata[s].ToList();

            for (var i = shuffled.Count - 1; i > 0; i--)
            {
                var j = rng.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            var testCount = (int)Math.Round(shuffled.Count * testFraction);
            test.AddRange(shuffled.Take(testCount));
            train.AddRange(shuffled.Skip(testCount));
        }


        // Двата списъка се разбъркват накрая: дотук те са подредени група по
        // група (всички легитимни от един корпус, после всички фишинг от него
        // и т.н.). Онлайн алгоритми като AveragedPerceptron четат примерите в
        // реда, в който им се подават — при подредени данни последната група
        // изтегля тежестите към себе си и точността пада с проценти.
        Shuffle(train, new Random(seed + 1_000));
        Shuffle(test, new Random(seed + 2_000));

        return (train, test);
    }

    private static void Shuffle(List<CorpusRow> items, Random rng)
    {
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}
