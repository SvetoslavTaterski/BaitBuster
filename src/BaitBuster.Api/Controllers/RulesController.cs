using BaitBuster.Core.Detection;
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

[ApiController]
[Route("api/[controller]")]
public sealed class RulesController(IEnumerable<IDetectionRule> rules) : ControllerBase
{
    /// <summary>
    /// Всички активни правила. Списъкът идва от самите регистрирани правила,
    /// затова ново правило се появява тук автоматично, без промяна в контролера.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<List<RuleDescription>>(StatusCodes.Status200OK)]
    public ActionResult<List<RuleDescription>> GetRules()
    {
        var descriptions = rules
            .Select(r => new RuleDescription(r.RuleId, r.Name, r.Category, r.Description, r.MaxScore))
            .OrderBy(r => r.RuleId)
            .ToList();

        return Ok(descriptions);
    }
}
