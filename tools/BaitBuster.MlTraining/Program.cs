using System.Text.Json;
using BaitBuster.Core.Detection.Ml;
using Microsoft.ML;

// Пътищата се подават като аргументи, за да не зависи програмата от
// работната директория, от която е стартирана.
var dataPath = args.ElementAtOrDefault(0)
    ?? throw new ArgumentException("Първи аргумент: път до .tsv файла с тренировъчни данни.");
var modelPath = args.ElementAtOrDefault(1)
    ?? throw new ArgumentException("Втори аргумент: път, на който да се запише model.zip.");

const string algorithm = "SdcaLogisticRegression";

var ml = new MLContext(seed: 42);

// 1. Зареждане на данните от TSV файла.
Console.WriteLine($"Зареждам данни от {dataPath}");
var allData = ml.Data.LoadFromTextFile<EmailData>(
    dataPath, separatorChar: '\t', hasHeader: true);

var rows = ml.Data.CreateEnumerable<EmailData>(allData, reuseRowObject: false).ToList();
var phishingCount = rows.Count(r => r.Label);
Console.WriteLine($"{rows.Count} примера ({phishingCount} фишинг, {rows.Count - phishingCount} легитимни)");

// 2. Разделяне на данните: 80% за обучение, 20% за проверка.
//    Моделът никога не вижда тестовите редове по време на обучението,
//    затова резултатът върху тях показва как се справя с нови имейли.
var split = ml.Data.TrainTestSplit(allData, testFraction: 0.2, seed: 42);
var trainCount = ml.Data.CreateEnumerable<EmailData>(split.TrainSet, reuseRowObject: false).Count();
var testCount = ml.Data.CreateEnumerable<EmailData>(split.TestSet, reuseRowObject: false).Count();

// 3. Дефиниране на pipeline-а — последователността от стъпки:
//    FeaturizeText превръща текста в числа (честоти на думи и словосъчетания),
//    защото алгоритъмът работи с числа, не с текст.
//    SdcaLogisticRegression е класическият алгоритъм за двоична класификация:
//    учи тежест за всяка дума и ги комбинира във вероятност.
var pipeline = ml.Transforms.Text
    .FeaturizeText("Features", nameof(EmailData.Text))
    .Append(ml.BinaryClassification.Trainers.SdcaLogisticRegression(
        labelColumnName: nameof(EmailData.Label),
        featureColumnName: "Features"));

// 4. Самото обучение.
Console.WriteLine("Обучавам модела…");
var model = pipeline.Fit(split.TrainSet);

// 5. Оценка върху тестовите данни.
var metrics = ml.BinaryClassification.Evaluate(
    model.Transform(split.TestSet), labelColumnName: nameof(EmailData.Label));

Console.WriteLine();
Console.WriteLine("=== Резултати върху тестовите данни ===");
Console.WriteLine($"Accuracy  (общ дял верни отговори): {metrics.Accuracy:P1}");
Console.WriteLine($"Precision (от маркираните като фишинг, колко наистина са): {metrics.PositivePrecision:P1}");
Console.WriteLine($"Recall    (от истинските фишинг, колко е хванал): {metrics.PositiveRecall:P1}");
Console.WriteLine($"F1 score  (баланс между precision и recall): {metrics.F1Score:P1}");
Console.WriteLine($"AUC       (способност да разграничава двата класа): {metrics.AreaUnderRocCurve:P1}");
Console.WriteLine();

// 6. Записване на обучения модел, за да го зареди API-то.
Directory.CreateDirectory(Path.GetDirectoryName(modelPath)!);
ml.Model.Save(model, allData.Schema, modelPath);
Console.WriteLine($"Моделът е записан в {modelPath}");

// 7. Метаданните пътуват до модела и захранват „ML модел" таба в UI-то.
var metadata = new ModelMetadata(
    TrainedAt: DateTimeOffset.UtcNow,
    Algorithm: algorithm,
    TotalExamples: rows.Count,
    TrainingExamples: trainCount,
    TestExamples: testCount,
    PhishingExamples: phishingCount,
    LegitimateExamples: rows.Count - phishingCount,
    Metrics: new ModelMetrics(
        metrics.Accuracy,
        metrics.PositivePrecision,
        metrics.PositiveRecall,
        metrics.F1Score,
        metrics.AreaUnderRocCurve));

var metadataPath = Path.ChangeExtension(modelPath, ".json");
File.WriteAllText(metadataPath,
    JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine($"Метаданните са записани в {metadataPath}");
