using BaitBuster.Core.Detection;
using BaitBuster.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace BaitBuster.Api.Controllers;

/// <summary>Едно детекционно правило, описано за показване в UI-то.</summary>
public sealed record RuleDescription(
    string RuleId,
    string Name,
    string Category,
    string Description,
    int MaxScore
);

/// <summary>Праговете, които превръщат risk score в присъда.</summary>
public sealed record VerdictThresholds(int Suspicious, int Phishing, int MaxScore);

public sealed record RulesResponse(
    IReadOnlyList<RuleDescription> Rules,
    VerdictThresholds Thresholds
);

[ApiController]
[Route("api/[controller]")]
public sealed class RulesController(IEnumerable<IDetectionRule> rules) : ControllerBase
{
    /// <summary>
    /// Всички активни правила и праговете за присъда. Списъкът идва от самите
    /// регистрирани правила, а праговете — от константите в AnalysisReport,
    /// така че UI-то не може да покаже нещо различно от това, което кодът прави.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<RulesResponse>(StatusCodes.Status200OK)]
    public ActionResult<RulesResponse> GetRules()
    {
        var descriptions = rules
            .Select(r => new RuleDescription(r.RuleId, r.Name, r.Category, r.Description, r.MaxScore))
            .OrderBy(r => r.RuleId)
            .ToList();

        var thresholds = new VerdictThresholds(
            AnalysisReport.SuspiciousThreshold,
            AnalysisReport.PhishingThreshold,
            AnalysisReport.MaxScore);

        return Ok(new RulesResponse(descriptions, thresholds));
    }
}
