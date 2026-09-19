using BaitBuster.Core.Detection.Ml;

namespace BaitBuster.MlTraining;

/// <summary>Предсказанието на модела за един ред от тестовата извадка.</summary>
internal sealed record Scored(CorpusRow Row, bool Predicted, float Probability);

/// <summary>
/// Метриките се смятат тук на ръка, вместо да се вземат наготово от
/// ML.NET, по две причини: за да е ясно в текста на дипломната работа как
/// точно е получено всяко число, и защото разбивката по източник изисква
/// достъп до реда, от който идва всяко предсказание.
/// </summary>
internal static class Evaluation
{
    public static ConfusionMatrix Confusion(IEnumerable<Scored> scored)
    {
        int tp = 0, fp = 0, tn = 0, fn = 0;

        foreach (var s in scored)
        {
            if (s.Row.IsPhishing)
            {
                if (s.Predicted) tp++; else fn++;
            }
            else
            {
                if (s.Predicted) fp++; else tn++;
            }
        }

        return new ConfusionMatrix(tp, fp, tn, fn);
    }

    public static ModelMetrics Metrics(ConfusionMatrix c, double auc)
    {
        var total = c.TruePositives + c.FalsePositives + c.TrueNegatives + c.FalseNegatives;
        var predictedPositive = c.TruePositives + c.FalsePositives;
        var actualPositive = c.TruePositives + c.FalseNegatives;

        var accuracy = total == 0 ? 0 : (double)(c.TruePositives + c.TrueNegatives) / total;
        var precision = predictedPositive == 0 ? 0 : (double)c.TruePositives / predictedPositive;
        var recall = actualPositive == 0 ? 0 : (double)c.TruePositives / actualPositive;
        var f1 = precision + recall == 0 ? 0 : 2 * precision * recall / (precision + recall);

        return new ModelMetrics(accuracy, precision, recall, f1, auc);
    }

    /// <summary>
    /// Как се справя моделът върху частта от теста, дошла от всеки корпус.
    /// Това е проверката дали моделът е научил езика на фишинга, или просто
    /// разпознава по кой корпус гледа.
    /// </summary>
    public static List<SourceAccuracy> PerSource(IEnumerable<Scored> scored)
    {
        return scored
            .GroupBy(s => s.Row.Source)
            .OrderByDescending(g => g.Count())
            .Select(g =>
            {
                var c = Confusion(g);
                var actualPositive = c.TruePositives + c.FalseNegatives;
                var actualNegative = c.TrueNegatives + c.FalsePositives;
                var total = actualPositive + actualNegative;

                return new SourceAccuracy(
                    Source: g.Key,
                    TestExamples: total,
                    Accuracy: total == 0 ? 0 : (double)(c.TruePositives + c.TrueNegatives) / total,
                    Recall: actualPositive == 0 ? null : (double)c.TruePositives / actualPositive,
                    FalsePositiveRate: actualNegative == 0 ? null : (double)c.FalsePositives / actualNegative);
            })
            .ToList();
    }

    /// <summary>
    /// AUC по метода на Ман–Уитни: вероятността случайно избран фишинг имейл
    /// да получи по-висок резултат от случайно избран легитимен. Смята се тук,
    /// защото ML.NET предлага готово AUC само за двоичните си алгоритми, а
    /// Naive Bayes минава по многокласовия път.
    /// </summary>
    public static double RocAuc(IEnumerable<Scored> scored)
    {
        var ranked = scored.OrderBy(s => s.Probability).ToList();

        var positives = ranked.Count(s => s.Row.IsPhishing);
        var negatives = ranked.Count - positives;

        // С един-единствен клас в извадката AUC е неопределено.
        if (positives == 0 || negatives == 0)
            return 0;

        // Равните резултати получават среден ранг, иначе стойността би
        // зависела от случайната подредба между тях.
        var ranks = new double[ranked.Count];
        var i = 0;

        while (i < ranked.Count)
        {
            var j = i;
            while (j + 1 < ranked.Count && ranked[j + 1].Probability == ranked[i].Probability)
                j++;

            var averageRank = (i + j) / 2.0 + 1;
            for (var k = i; k <= j; k++)
                ranks[k] = averageRank;

            i = j + 1;
        }

        var positiveRankSum = 0.0;
        for (var k = 0; k < ranked.Count; k++)
            if (ranked[k].Row.IsPhishing)
                positiveRankSum += ranks[k];

        return (positiveRankSum - positives * (positives + 1) / 2.0) / ((double)positives * negatives);
    }

    public static double StdDev(IReadOnlyCollection<double> values)
    {
        if (values.Count < 2)
            return 0;

        var mean = values.Average();
        return Math.Sqrt(values.Sum(v => (v - mean) * (v - mean)) / (values.Count - 1));
    }
}
