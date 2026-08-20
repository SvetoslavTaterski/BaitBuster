using BaitBuster.Api.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BaitBuster.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class StatisticsController(BaitBusterDbContext db) : ControllerBase
{
    private const int TopRulesCount = 5;
    private const int TrendDays = 7;

    /// <summary>Обобщени данни за всички запазени анализи.</summary>
    [HttpGet]
    [ProducesResponseType<StatisticsResponse>(StatusCodes.Status200OK)]
    public async Task<ActionResult<StatisticsResponse>> GetStatistics()
    {
        var total = await db.Analyses.CountAsync();

        if (total == 0)
            return Ok(new StatisticsResponse(0, 0, 0, 0, 0, [], [], BuildEmptyTrend()));

        var averageRiskScore = await db.Analyses.AverageAsync(a => (double)a.RiskScore);

        var verdicts = await db.Analyses
            .GroupBy(a => a.Verdict)
            .Select(g => new { Verdict = g.Key, Count = g.Count() })
            .ToListAsync();

        int VerdictCount(string verdict) =>
            verdicts.FirstOrDefault(v => v.Verdict == verdict)?.Count ?? 0;

        // Групирането и подредбата остават в SQL, но проекцията към NamedCount
        // се прави след това: EF Core не превежда извикване на конструктор
        // на собствен тип вътре в GroupBy проекция, а анонимният тип минава.
        var findingsByCategory = (await db.Findings
            .GroupBy(f => f.Category)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(c => c.Count)
            .ToListAsync())
            .Select(c => new NamedCount(c.Name, c.Count))
            .ToList();

        var topRules = (await db.Findings
            .GroupBy(f => f.RuleId)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(r => r.Count)
            .Take(TopRulesCount)
            .ToListAsync())
            .Select(r => new NamedCount(r.Name, r.Count))
            .ToList();

        // Датите се групират в паметта, а не в SQL — SQLite-ят provider превежда
        // част от операциите върху дати непълно, а прозорецът е само няколко дни,
        // така че цената е нищожна.
        var since = DateTime.UtcNow.Date.AddDays(-(TrendDays - 1));
        var recentDates = await db.Analyses
            .Where(a => a.AnalyzedAt >= since)
            .Select(a => a.AnalyzedAt)
            .ToListAsync();

        var countsByDay = recentDates
            .GroupBy(d => DateOnly.FromDateTime(d.Date))
            .ToDictionary(g => g.Key, g => g.Count());

        var lastDays = BuildEmptyTrend()
            .Select(day => day with { Count = countsByDay.GetValueOrDefault(day.Date) })
            .ToList();

        return Ok(new StatisticsResponse(
            total,
            averageRiskScore,
            VerdictCount("Phishing"),
            VerdictCount("Suspicious"),
            VerdictCount("Legitimate"),
            findingsByCategory,
            topRules,
            lastDays));
    }

    /// <summary>Последните дни с нулеви стойности — дните без анализи също се показват.</summary>
    private static List<DailyCount> BuildEmptyTrend()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);

        return Enumerable.Range(0, TrendDays)
            .Select(offset => new DailyCount(today.AddDays(offset - (TrendDays - 1)), 0))
            .ToList();
    }
}
