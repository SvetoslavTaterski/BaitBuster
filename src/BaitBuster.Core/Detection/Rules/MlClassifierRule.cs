using BaitBuster.Core.Detection.Ml;
using BaitBuster.Core.Models;
using Microsoft.Extensions.ML;

namespace BaitBuster.Core.Detection.Rules;

/// <summary>
/// ML-001: Обучен класификатор върху текста на имейла (тема + тяло).
/// За разлика от останалите правила тук няма ръчно написани условия —
/// моделът е извлякъл закономерностите сам от тренировъчните примери.
/// </summary>
public sealed class MlClassifierRule(PredictionEnginePool<EmailData, EmailPrediction> engine)
    : IDetectionRule
{
    public string RuleId => "ML-001";

    /// <summary>Под този праг на увереност не докладваме нищо.</summary>
    private const float ReportThreshold = 0.60f;

    public IEnumerable<Finding> Evaluate(ParsedEmail email)
    {
        var text = $"{email.Subject}\n{email.BodyText}".Trim();
        if (text.Length == 0)
            yield break;

        var prediction = engine.Predict(new EmailData { Text = text });

        if (!prediction.IsPhishing || prediction.Probability < ReportThreshold)
            yield break;

        // Приносът към общия score расте с увереността на модела:
        // 60% увереност → 10 точки, 100% → 30 точки.
        var score = (int)Math.Round(10 + (prediction.Probability - ReportThreshold) / (1 - ReportThreshold) * 20);

        yield return new Finding(RuleId, "Ml",
            prediction.Probability >= 0.85f ? Severity.High : Severity.Medium,
            score,
            "Класификаторът разпознава езикови модели, характерни за фишинг съобщения.",
            $"Увереност на модела: {prediction.Probability:P0}");
    }
}
