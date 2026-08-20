namespace BaitBuster.Core.Detection.Ml;

/// <summary>
/// Описание на обучения модел — записва се от BaitBuster.MlTraining
/// заедно с модела и се показва в „ML модел“ таба на приложението.
///
/// Полетата след <see cref="Metrics"/> са незадължителни, за да може
/// приложението да прочете и метаданни, записани от по-ранна версия
/// на тренировъчния инструмент.
/// </summary>
public sealed record ModelMetadata(
    DateTimeOffset TrainedAt,
    string Algorithm,
    int TotalExamples,
    int TrainingExamples,
    int TestExamples,
    int PhishingExamples,
    int LegitimateExamples,
    ModelMetrics Metrics,
    ConfusionMatrix? Confusion = null,
    CrossValidationSummary? CrossValidation = null,
    IReadOnlyList<AlgorithmResult>? Candidates = null,
    IReadOnlyList<SourceAccuracy>? PerSource = null,
    string? Dataset = null
);

/// <summary>Метрики, измерени върху тестовата извадка (0–1).</summary>
public sealed record ModelMetrics(
    double Accuracy,
    double Precision,
    double Recall,
    double F1Score,
    double AreaUnderRocCurve
);

/// <summary>
/// Разпределение на решенията върху тестовата извадка. Четирите числа
/// показват това, което процентите скриват: колко легитимни имейла биха
/// били обявени за фишинг (<see cref="FalsePositives"/> — най-скъпата
/// грешка за потребителя) и колко атаки биха минали незабелязано.
/// </summary>
public sealed record ConfusionMatrix(
    int TruePositives,
    int FalsePositives,
    int TrueNegatives,
    int FalseNegatives
);

/// <summary>
/// Резултат от k-кратна кръстосана проверка. Едно разделяне може да е
/// било щастливо; стандартното отклонение показва колко се клати оценката
/// при друго разделяне на същите данни.
/// </summary>
public sealed record CrossValidationSummary(
    int Folds,
    double AccuracyMean,
    double AccuracyStdDev,
    double F1Mean,
    double F1StdDev,
    double AucMean,
    double AucStdDev
);

/// <summary>Един изпробван алгоритъм и как се е представил спрямо останалите.</summary>
public sealed record AlgorithmResult(
    string Algorithm,
    ModelMetrics Metrics,
    double TrainingSeconds,
    bool Selected
);

/// <summary>
/// Точност върху частта от тестовата извадка, дошла от един корпус.
/// Ако числата се разминават силно между източниците, моделът е научил
/// особеностите на конкретен корпус, а не езика на фишинга.
/// </summary>
public sealed record SourceAccuracy(
    string Source,
    int TestExamples,
    double Accuracy,
    double? Recall,
    double? FalsePositiveRate
);
