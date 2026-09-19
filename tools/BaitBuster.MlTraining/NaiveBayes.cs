using System.Diagnostics;
using BaitBuster.Core.Detection.Ml;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace BaitBuster.MlTraining;

/// <summary>
/// Naive Bayes стои настрани от останалите кандидати по две причини.
///
/// Първо, ML.NET го предлага само като многокласов алгоритъм — няма двоичен
/// вариант. Затова етикетът се превръща в ключ преди обучението и обратно в
/// булев след предсказанието, а метриките не идват от
/// BinaryClassification.Evaluate, а се смятат от твърдите предсказания.
///
/// Второ, изходът му не е калибрирана вероятност. MlClassifierRule обаче
/// показва увереност в проценти, затова Naive Bayes участва в сравнението за
/// записката, но никога не се избира за модел на приложението.
/// </summary>
internal static class NaiveBayes
{
    public const string Name = "NaiveBayes";

    private const string KeyLabelColumn = "NbLabel";
    private const string PredictedColumn = "NbPredicted";

    public static AlgorithmResult Evaluate(
        MLContext ml,
        IDataView trainFeatures,
        IDataView testFeatures,
        IReadOnlyList<CorpusRow> testRows,
        string labelColumn,
        string featureColumn)
    {
        var pipeline = ml.Transforms.Conversion.MapValueToKey(KeyLabelColumn, labelColumn)
            .Append(ml.MulticlassClassification.Trainers.NaiveBayes(KeyLabelColumn, featureColumn))
            .Append(ml.Transforms.Conversion.MapKeyToValue(PredictedColumn, "PredictedLabel"));

        var stopwatch = Stopwatch.StartNew();
        var model = pipeline.Fit(trainFeatures);
        stopwatch.Stop();

        var predictions = ml.Data
            .CreateEnumerable<Prediction>(model.Transform(testFeatures), reuseRowObject: false)
            .ToList();

        var phishingSlot = FindPhishingSlot(predictions);

        // Transform запазва реда на редовете, затова предсказанията се
        // съпоставят едно към едно с тестовата извадка.
        var scored = predictions
            .Select((p, index) => new Scored(
                testRows[index],
                p.IsPhishing,
                phishingSlot is null ? 0f : p.Score[phishingSlot.Value]))
            .ToList();

        var confusion = Evaluation.Confusion(scored);
        var metrics = Evaluation.Metrics(confusion, Evaluation.RocAuc(scored));

        return new AlgorithmResult(Name, metrics, stopwatch.Elapsed.TotalSeconds, Selected: false);
    }

    /// <summary>
    /// Кой елемент от вектора със стойности отговаря на класа „фишинг".
    /// Подредбата на ключовете зависи от реда, в който стойностите се срещат в
    /// данните, затова се определя от самите предсказания: при ред, обявен за
    /// фишинг, най-високата стойност сочи търсения елемент.
    /// </summary>
    private static int? FindPhishingSlot(IReadOnlyList<Prediction> predictions)
    {
        var phishing = predictions.FirstOrDefault(p => p.IsPhishing && p.Score.Length > 0);
        if (phishing is not null)
            return ArgMax(phishing.Score);

        // Ако моделът не е обявил нито един ред за фишинг, ориентир е обратното:
        // при два класа другият елемент е този на фишинга.
        var legitimate = predictions.FirstOrDefault(p => p.Score.Length == 2);
        return legitimate is null ? null : 1 - ArgMax(legitimate.Score);
    }

    private static int ArgMax(IReadOnlyList<float> values)
    {
        var best = 0;
        for (var i = 1; i < values.Count; i++)
            if (values[i] > values[best])
                best = i;

        return best;
    }

    private sealed class Prediction
    {
        [ColumnName(PredictedColumn)]
        public bool IsPhishing { get; set; }

        [ColumnName("Score")]
        public float[] Score { get; set; } = [];
    }
}
