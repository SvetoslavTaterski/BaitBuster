using System.Diagnostics;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using BaitBuster.Core.Detection.Ml;
using BaitBuster.MlTraining;
using Microsoft.ML;

// Обучава класификатора на BaitBuster и записва модела заедно с метаданни.
//
// Употреба:
//   dotnet run --project tools/BaitBuster.MlTraining -- <corpus.tsv> <model.zip> [--folds N] [--skip-cv]
//
// Стъпките отговарят на три различни въпроса:
//   1. Сравнение между алгоритми — кой се справя най-добре с тези данни.
//   2. Обучение на избрания и оценка върху отделена тестова извадка,
//      която моделът не е виждал — колко добър е върху нови имейли.
//   3. Кръстосана проверка — доколко този резултат е устойчив, а не късмет.

Console.OutputEncoding = Encoding.UTF8;

var positional = args.Where(a => !a.StartsWith("--")).ToArray();

var dataPath = positional.ElementAtOrDefault(0)
    ?? throw new ArgumentException("Първи аргумент: път до .tsv файла с тренировъчни данни.");
var modelPath = positional.ElementAtOrDefault(1)
    ?? throw new ArgumentException("Втори аргумент: път, на който да се запише model.zip.");

var skipCrossValidation = args.Contains("--skip-cv");

var folds = 5;
var foldsFlag = Array.IndexOf(args, "--folds");
if (foldsFlag >= 0 && foldsFlag + 1 < args.Length)
    folds = int.Parse(args[foldsFlag + 1]);

const double TestFraction = 0.2;
const int Seed = 42;

var ml = new MLContext(seed: Seed);

// ── 1. Данни ────────────────────────────────────────────────────────────────

Console.WriteLine($"Зареждам корпуса от {dataPath}");
var rows = Corpus.Load(dataPath);

if (rows.Count == 0)
    throw new InvalidOperationException("Корпусът е празен.");

var phishingCount = rows.Count(r => r.IsPhishing);
Console.WriteLine($"{rows.Count:N0} примера — {phishingCount:N0} фишинг, " +
                  $"{rows.Count - phishingCount:N0} легитимни " +
                  $"({(double)phishingCount / rows.Count:P1} положителен клас)");

Console.WriteLine();
Console.WriteLine("Състав по източник:");
foreach (var group in rows.GroupBy(r => r.Source).OrderByDescending(g => g.Count()))
{
    var groupPhishing = group.Count(r => r.IsPhishing);
    Console.WriteLine($"  {group.Key,-16} {group.Count(),6:N0}  " +
                      $"({groupPhishing:N0} фишинг / {group.Count() - groupPhishing:N0} легитимни)");
}

var (train, test) = Corpus.StratifiedSplit(rows, TestFraction, Seed);

Console.WriteLine();
Console.WriteLine($"Разделяне с запазени пропорции по източник и клас: " +
                  $"{train.Count:N0} за обучение, {test.Count:N0} за тест");

var trainView = ml.Data.LoadFromEnumerable(train.Select(ToEmailData));
var testView = ml.Data.LoadFromEnumerable(test.Select(ToEmailData));

// ── 2. Сравнение между алгоритмите ──────────────────────────────────────────

// Превръщането на текста в числа е една и съща стъпка за всички кандидати,
// затова се прави веднъж и резултатът се кешира. Иначе всеки алгоритъм би
// плащал наново най-скъпата част от работата.
Console.WriteLine();
Console.WriteLine("Извличам признаци от текста (думи и словосъчетания)…");

var featurizer = ml.Transforms.Text.FeaturizeText("Features", nameof(EmailData.Text));
var featurizerModel = featurizer.Fit(trainView);

var trainFeatures = ml.Data.Cache(featurizerModel.Transform(trainView));
var testFeatures = ml.Data.Cache(featurizerModel.Transform(testView));

Console.WriteLine();
Console.WriteLine("=== Сравнение между алгоритми (върху тестовата извадка) ===");
Console.WriteLine($"{"Алгоритъм",-26}{"Accuracy",10}{"Precision",11}{"Recall",9}{"F1",9}{"AUC",9}{"Време",10}");

var comparison = new List<AlgorithmResult>();

foreach (var candidate in Candidates.Build(ml, nameof(EmailData.Label), "Features"))
{
    var stopwatch = Stopwatch.StartNew();
    var predictor = candidate.Trainer.Fit(trainFeatures);
    stopwatch.Stop();

    var metrics = Score(ml, predictor, testFeatures, test).Metrics;

    comparison.Add(new AlgorithmResult(
        candidate.Name, metrics, stopwatch.Elapsed.TotalSeconds, Selected: false));

    Console.WriteLine($"{candidate.Name,-26}{metrics.Accuracy,10:P2}{metrics.Precision,11:P2}" +
                      $"{metrics.Recall,9:P2}{metrics.F1Score,9:P2}{metrics.AreaUnderRocCurve,9:P2}" +
                      $"{stopwatch.Elapsed.TotalSeconds,9:N1}с");
}

// Изборът е по F1, защото и двете грешки тежат: пропуснатият фишинг е риск
// за потребителя, а излишното предупреждение го учи да ги пренебрегва.
var best = comparison.MaxBy(r => r.Metrics.F1Score)!;

Console.WriteLine();
Console.WriteLine($"Избран алгоритъм: {best.Algorithm} (най-висок F1)");

// ── 3. Окончателен модел ────────────────────────────────────────────────────

// Победителят се обучава наново с пълния pipeline — точно този обект се
// записва на диска, затова докладваните метрики идват от него, а не от
// междинния вариант с кеширани признаци.
var winner = Candidates.Build(ml, nameof(EmailData.Label), "Features")
    .First(c => c.Name == best.Algorithm);

Console.WriteLine("Обучавам окончателния модел…");

// Кешът след извличането на признаците повтаря същите условия, при които
// беше направено сравнението — и позволява на алгоритъма да мине през
// данните повече от веднъж, вместо да ги чете поточно.
var finalPipeline = featurizer.AppendCacheCheckpoint(ml).Append(winner.Trainer);
var finalModel = finalPipeline.Fit(trainView);

var evaluation = Score(ml, finalModel, testView, test);

Console.WriteLine();
Console.WriteLine("=== Резултати върху тестовата извадка ===");
Console.WriteLine($"Accuracy  (общ дял верни отговори):                        {evaluation.Metrics.Accuracy:P2}");
Console.WriteLine($"Precision (от маркираните като фишинг, колко наистина са): {evaluation.Metrics.Precision:P2}");
Console.WriteLine($"Recall    (от истинските фишинг, колко е хванал):          {evaluation.Metrics.Recall:P2}");
Console.WriteLine($"F1 score  (баланс между precision и recall):               {evaluation.Metrics.F1Score:P2}");
Console.WriteLine($"AUC       (способност да разграничава двата класа):        {evaluation.Metrics.AreaUnderRocCurve:P2}");

var confusion = evaluation.Confusion;

Console.WriteLine();
Console.WriteLine("Разпределение на решенията:");
Console.WriteLine($"  вярно разпознат фишинг:        {confusion.TruePositives,6:N0}");
Console.WriteLine($"  пропуснат фишинг:              {confusion.FalseNegatives,6:N0}");
Console.WriteLine($"  вярно разпозната легитимна:    {confusion.TrueNegatives,6:N0}");
Console.WriteLine($"  легитимна, обявена за фишинг:  {confusion.FalsePositives,6:N0}");

Console.WriteLine();
Console.WriteLine("=== По източник (тестова извадка) ===");
Console.WriteLine($"{"Източник",-18}{"Примери",9}{"Accuracy",11}{"Recall",11}{"Лъжлива тревога",18}");

foreach (var source in evaluation.PerSource)
{
    Console.WriteLine($"{source.Source,-18}{source.TestExamples,9:N0}{source.Accuracy,11:P2}" +
                      $"{FormatRate(source.Recall),11}{FormatRate(source.FalsePositiveRate),18}");
}

// ── 4. Кръстосана проверка ──────────────────────────────────────────────────

CrossValidationSummary? crossValidation = null;

if (!skipCrossValidation)
{
    Console.WriteLine();
    Console.WriteLine($"Кръстосана проверка с {folds} дяла върху целия корпус…");

    var allView = ml.Data.LoadFromEnumerable(rows.Select(ToEmailData));
    var cv = ml.BinaryClassification.CrossValidate(
        allView, finalPipeline, numberOfFolds: folds, labelColumnName: nameof(EmailData.Label));

    var accuracies = cv.Select(r => r.Metrics.Accuracy).ToList();
    var f1Scores = cv.Select(r => r.Metrics.F1Score).ToList();
    var aucs = cv.Select(r => r.Metrics.AreaUnderRocCurve).ToList();

    crossValidation = new CrossValidationSummary(
        folds,
        accuracies.Average(), Evaluation.StdDev(accuracies),
        f1Scores.Average(), Evaluation.StdDev(f1Scores),
        aucs.Average(), Evaluation.StdDev(aucs));

    for (var i = 0; i < cv.Count; i++)
        Console.WriteLine($"  дял {i + 1}: accuracy {cv[i].Metrics.Accuracy:P2}, F1 {cv[i].Metrics.F1Score:P2}");

    Console.WriteLine($"  средно: accuracy {crossValidation.AccuracyMean:P2} " +
                      $"(± {crossValidation.AccuracyStdDev:P2}), " +
                      $"F1 {crossValidation.F1Mean:P2} (± {crossValidation.F1StdDev:P2})");
}

// ── 5. Записване ────────────────────────────────────────────────────────────

Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(modelPath))!);
ml.Model.Save(finalModel, trainView.Schema, modelPath);

Console.WriteLine();
Console.WriteLine($"Моделът е записан в {modelPath}");

var metadata = new ModelMetadata(
    TrainedAt: DateTimeOffset.UtcNow,
    Algorithm: best.Algorithm,
    TotalExamples: rows.Count,
    TrainingExamples: train.Count,
    TestExamples: test.Count,
    PhishingExamples: phishingCount,
    LegitimateExamples: rows.Count - phishingCount,
    Metrics: evaluation.Metrics,
    Confusion: evaluation.Confusion,
    CrossValidation: crossValidation,
    Candidates: comparison
        .Select(r => r with { Selected = r.Algorithm == best.Algorithm })
        .OrderByDescending(r => r.Metrics.F1Score)
        .ToList(),
    PerSource: evaluation.PerSource,
    Dataset: string.Join(", ", rows.Select(r => r.Source).Distinct().Order(StringComparer.Ordinal)));

var metadataPath = Path.ChangeExtension(modelPath, ".json");
await File.WriteAllTextAsync(metadataPath,
    JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));

Console.WriteLine($"Метаданните са записани в {metadataPath}");

return;

static EmailData ToEmailData(CorpusRow row) => new() { Label = row.IsPhishing, Text = row.Text };

static string FormatRate(double? value) => value?.ToString("P2") ?? "—";

// Прекарва извадката през модела и събира всичко, което зависи от
// предсказанията: метрики, разпределение на решенията и разбивка по източник.
static (ModelMetrics Metrics, ConfusionMatrix Confusion, List<SourceAccuracy> PerSource) Score(
    MLContext ml, ITransformer model, IDataView features, IReadOnlyList<CorpusRow> rows)
{
    var scoredView = model.Transform(features);

    var auc = ml.BinaryClassification
        .Evaluate(scoredView, labelColumnName: nameof(EmailData.Label))
        .AreaUnderRocCurve;

    // Transform запазва реда на редовете, затова предсказанията се съпоставят
    // едно към едно със списъка, от който е построен IDataView-ът.
    var predictions = ml.Data
        .CreateEnumerable<EmailPrediction>(scoredView, reuseRowObject: false)
        .Select((prediction, index) => new Scored(rows[index], prediction.IsPhishing, prediction.Probability))
        .ToList();

    var confusion = Evaluation.Confusion(predictions);

    return (Evaluation.Metrics(confusion, auc), confusion, Evaluation.PerSource(predictions));
}
