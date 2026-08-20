using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using BaitBuster.Core.Detection;
using BaitBuster.Core.Detection.Ml;
using BaitBuster.Core.Detection.Rules;
using BaitBuster.Core.Models;
using BaitBuster.Core.Parsing;
using BaitBuster.CorpusEval;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ML;

// Оценява цялото приложение, не само класификатора: сурови .eml файлове
// минават през EmlParser и през четирите детекционни правила, точно както
// при качване на имейл в интерфейса.
//
// BaitBuster.MlTraining мери само едно от правилата и то върху вече
// изчистен текст. Тук се проверява останалото — че парсерът се справя с
// истински MIME, че правилата за header-и и линкове намират нещо, и че
// сборът от приноси води до правилната присъда.
//
// Употреба:
//   dotnet run --project tools/BaitBuster.CorpusEval -c Release -- <spamAssassinDir> <model.zip> [report.json]

Console.OutputEncoding = Encoding.UTF8;

var corpusDir = args.ElementAtOrDefault(0)
    ?? throw new ArgumentException("Първи аргумент: папка с разархивирания SpamAssassin корпус.");
var modelPath = args.ElementAtOrDefault(1)
    ?? throw new ArgumentException("Втори аргумент: път до обучения model.zip.");
var reportPath = args.ElementAtOrDefault(2);

// Правилата се сглобяват през същия DI контейнер, който използва и API-то —
// иначе оценката би мерила друга конфигурация, различна от работещата.
var services = new ServiceCollection();
services.AddSingleton<EmlParser>();
services.AddSingleton<DetectionEngine>();
services.AddPredictionEnginePool<EmailData, EmailPrediction>().FromFile(modelPath);
services.AddSingleton<IDetectionRule, HeaderMismatchRule>();
services.AddSingleton<IDetectionRule, UrlAnalysisRule>();
services.AddSingleton<IDetectionRule, UrgencyContentRule>();
services.AddSingleton<IDetectionRule, MlClassifierRule>();

using var provider = services.BuildServiceProvider();
var parser = provider.GetRequiredService<EmlParser>();
var engine = provider.GetRequiredService<DetectionEngine>();

var folders = CorpusFolders.Discover(corpusDir);

if (folders.Count == 0)
    throw new InvalidOperationException(
        $"В {corpusDir} няма нито една от очакваните папки " +
        $"({string.Join(", ", CorpusFolders.Expected.Keys)}). Разархивиран ли е корпусът?");

var folderReports = new List<FolderReport>();
var overall = new Tally();
var ruleHits = new Dictionary<string, int>(StringComparer.Ordinal);
var parseFailures = 0;

foreach (var folder in folders)
{
    Console.Write($"{folder.Name,-14} ");

    var tally = new Tally();
    var files = Directory.EnumerateFiles(folder.Path).ToList();

    foreach (var file in files)
    {
        AnalysisReport report;

        try
        {
            using var stream = File.OpenRead(file);
            report = engine.Analyze(parser.Parse(stream));
        }
        catch (Exception)
        {
            // Корпусът съдържа и нарочно повредени съобщения — те са част от
            // това, което един пощенски клиент среща, затова се броят отделно,
            // вместо да спират оценката.
            parseFailures++;
            continue;
        }

        foreach (var ruleId in report.Findings.Select(f => f.RuleId).Distinct())
            ruleHits[ruleId] = ruleHits.GetValueOrDefault(ruleId) + 1;

        tally.Add(report.Verdict, report.RiskScore, folder.IsMalicious);
        overall.Add(report.Verdict, report.RiskScore, folder.IsMalicious);
    }

    folderReports.Add(tally.ToReport(folder.Name, folder.IsMalicious));

    Console.WriteLine($"{tally.Total,5} съобщения → " +
                      $"фишинг {tally.Phishing,5}, съмнителни {tally.Suspicious,5}, " +
                      $"легитимни {tally.Legitimate,5}  (среден риск {tally.AverageScore:N1})");
}

Console.WriteLine();
Console.WriteLine("=== Обобщено ===");
Console.WriteLine($"Анализирани съобщения: {overall.Total:N0}");
Console.WriteLine($"Непарсваеми файлове:   {parseFailures:N0}");
Console.WriteLine();

// Приложението има три изхода, а корпусът — два класа. „Съмнително“ не е
// грешка: то е покана към потребителя да погледне сам. Затова се отчита
// отделно, вместо да се залепи към единия клас.
Console.WriteLine($"{"Присъда",-24}{"спам (атака)",16}{"ham (легитимна)",18}");
Console.WriteLine($"{"фишинг",-24}{overall.MaliciousPhishing,16:N0}{overall.BenignPhishing,18:N0}");
Console.WriteLine($"{"съмнително",-24}{overall.MaliciousSuspicious,16:N0}{overall.BenignSuspicious,18:N0}");
Console.WriteLine($"{"легитимно",-24}{overall.MaliciousLegitimate,16:N0}{overall.BenignLegitimate,18:N0}");

var malicious = overall.MaliciousTotal;
var benign = overall.BenignTotal;

Console.WriteLine();
if (malicious > 0)
{
    Console.WriteLine($"От атаките: {(double)overall.MaliciousPhishing / malicious:P2} обявени за фишинг, " +
                      $"{(double)overall.MaliciousSuspicious / malicious:P2} за съмнителни, " +
                      $"{(double)overall.MaliciousLegitimate / malicious:P2} пропуснати.");
}

if (benign > 0)
{
    Console.WriteLine($"От легитимните: {(double)overall.BenignPhishing / benign:P2} обявени за фишинг " +
                      $"(лъжлива тревога), {(double)overall.BenignSuspicious / benign:P2} за съмнителни.");
}

Console.WriteLine();
Console.WriteLine("=== Кое правило колко често се задейства ===");
Console.WriteLine($"{"Правило",-12}{"Съобщения",12}{"Дял",10}");
foreach (var (ruleId, count) in ruleHits.OrderByDescending(p => p.Value))
    Console.WriteLine($"{ruleId,-12}{count,12:N0}{(double)count / overall.Total,10:P1}");

if (reportPath is not null)
{
    var report = new CorpusEvalReport(
        DateTimeOffset.UtcNow,
        corpusDir,
        modelPath,
        overall.Total,
        parseFailures,
        overall.ToReport("общо", isMalicious: null),
        folderReports,
        ruleHits.OrderByDescending(p => p.Value).ToDictionary(p => p.Key, p => p.Value));

    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath))!);
    await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(report,
        new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }));

    Console.WriteLine();
    Console.WriteLine($"Отчетът е записан в {reportPath}");
}
