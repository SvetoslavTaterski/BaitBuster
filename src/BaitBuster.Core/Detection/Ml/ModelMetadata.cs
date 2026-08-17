namespace BaitBuster.Core.Detection.Ml;

/// <summary>
/// Описание на обучения модел — записва се от BaitBuster.MlTraining
/// заедно с модела и се показва в „ML модел" таба на приложението.
/// </summary>
public sealed record ModelMetadata(
    DateTimeOffset TrainedAt,
    string Algorithm,
    int TotalExamples,
    int TrainingExamples,
    int TestExamples,
    int PhishingExamples,
    int LegitimateExamples,
    ModelMetrics Metrics
);

/// <summary>Метрики, измерени върху тестовата извадка (0–1).</summary>
public sealed record ModelMetrics(
    double Accuracy,
    double Precision,
    double Recall,
    double F1Score,
    double AreaUnderRocCurve
);
