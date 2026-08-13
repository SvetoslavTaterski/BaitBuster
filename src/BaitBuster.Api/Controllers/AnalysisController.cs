using BaitBuster.Api.Persistence;
using BaitBuster.Core.Detection;
using BaitBuster.Core.Models;
using BaitBuster.Core.Parsing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BaitBuster.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AnalysisController(EmlParser parser, DetectionEngine engine, BaitBusterDbContext db)
    : ControllerBase
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB
    private const int MaxHistoryItems = 50;

    /// <summary>Анализ на качен .eml файл.</summary>
    [HttpPost("upload")]
    [ProducesResponseType<AnalysisReport>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AnalysisReport>> AnalyzeUpload(IFormFile file)
    {
        if (file.Length is 0 or > MaxFileSizeBytes)
            return BadRequest("Файлът е празен или надвишава 10 MB.");

        if (!file.FileName.EndsWith(".eml", StringComparison.OrdinalIgnoreCase))
            return BadRequest("Поддържа се само .eml формат.");

        await using var stream = file.OpenReadStream();

        ParsedEmail email;
        try
        {
            email = parser.Parse(stream);
        }
        catch (Exception)
        {
            return BadRequest("Файлът не може да бъде разчетен като валиден имейл.");
        }

        return Ok(await AnalyzeAndSave(email));
    }

    /// <summary>Анализ на ръчно въведено съдържание (суров MIME текст).</summary>
    [HttpPost("raw")]
    [Consumes("text/plain")]
    [ProducesResponseType<AnalysisReport>(StatusCodes.Status200OK)]
    public async Task<ActionResult<AnalysisReport>> AnalyzeRaw()
    {
        using var reader = new StreamReader(Request.Body);
        var raw = await reader.ReadToEndAsync();

        if (string.IsNullOrWhiteSpace(raw))
            return BadRequest("Празно съдържание.");

        using var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(raw));

        ParsedEmail email;
        try
        {
            email = parser.Parse(ms);
        }
        catch (Exception)
        {
            return BadRequest("Съдържанието не може да бъде разчетено като имейл.");
        }

        return Ok(await AnalyzeAndSave(email));
    }

    /// <summary>Последните анализирани имейли (обобщено, без находки).</summary>
    [HttpGet("history")]
    [ProducesResponseType<List<HistoryListItem>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<HistoryListItem>>> GetHistory()
    {
        var items = (await db.Analyses
            .OrderByDescending(a => a.AnalyzedAt)
            .Take(MaxHistoryItems)
            .Select(a => new { a.Id, a.EmailSubject, a.FromAddress, a.RiskScore, a.Verdict, a.AnalyzedAt, FindingsCount = a.Findings.Count })
            .ToListAsync())
            .Select(a => new HistoryListItem(
                a.Id, a.EmailSubject, a.FromAddress, a.RiskScore, a.Verdict,
                new DateTimeOffset(a.AnalyzedAt, TimeSpan.Zero), a.FindingsCount))
            .ToList();

        return Ok(items);
    }

    /// <summary>Пълен доклад за конкретен минал анализ, включително находките.</summary>
    [HttpGet("history/{id:int}")]
    [ProducesResponseType<HistoryDetailResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<HistoryDetailResponse>> GetHistoryDetail(int id)
    {
        var record = await db.Analyses
            .Include(a => a.Findings)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (record is null)
            return NotFound();

        var response = new HistoryDetailResponse(
            record.Id,
            record.EmailSubject,
            record.FromAddress,
            new DateTimeOffset(record.AnalyzedAt, TimeSpan.Zero),
            record.RiskScore,
            record.Verdict,
            record.Findings
                .Select(f => new HistoryFindingDto(f.RuleId, f.Category, f.Severity, f.Score, f.Description, f.Evidence))
                .ToList()
        );

        return Ok(response);
    }

    /// <summary>Изтрива минал анализ (заедно с находките му).</summary>
    [HttpDelete("history/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteHistoryItem(int id)
    {
        var record = await db.Analyses.FindAsync(id);
        if (record is null)
            return NotFound();

        db.Analyses.Remove(record);
        await db.SaveChangesAsync();

        return NoContent();
    }

    private async Task<AnalysisReport> AnalyzeAndSave(ParsedEmail email)
    {
        var report = engine.Analyze(email);

        var record = new AnalysisRecord
        {
            EmailSubject = report.EmailSubject,
            FromAddress = report.FromAddress,
            RiskScore = report.RiskScore,
            Verdict = report.Verdict.ToString(),
            AnalyzedAt = report.AnalyzedAt.UtcDateTime,
            Findings = report.Findings
                .Select(f => new FindingRecord
                {
                    RuleId = f.RuleId,
                    Category = f.Category,
                    Severity = f.Severity.ToString(),
                    Score = f.Score,
                    Description = f.Description,
                    Evidence = f.Evidence
                })
                .ToList()
        };

        db.Analyses.Add(record);
        await db.SaveChangesAsync();

        return report;
    }
}
