using Microsoft.ML.Data;

namespace BaitBuster.Core.Detection.Ml;

/// <summary>
/// Един ред от тренировъчните данни: текстът на имейла и етикетът
/// (true = фишинг). Колоните отговарят на реда в training-emails.tsv.
/// </summary>
public sealed class EmailData
{
    [LoadColumn(0)]
    public bool Label { get; set; }

    [LoadColumn(1)]
    public string Text { get; set; } = "";
}

/// <summary>
/// Изходът на модела за един имейл.
/// </summary>
public sealed class EmailPrediction
{
    /// <summary>Крайното решение на модела: фишинг или не.</summary>
    [ColumnName("PredictedLabel")]
    public bool IsPhishing { get; set; }

    /// <summary>Вероятност 0–1, че имейлът е фишинг.</summary>
    public float Probability { get; set; }

    /// <summary>Суров резултат преди превръщането му във вероятност.</summary>
    public float Score { get; set; }
}
