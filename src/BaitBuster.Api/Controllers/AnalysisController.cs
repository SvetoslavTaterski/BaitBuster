using BaitBuster.Core.Detection;
using BaitBuster.Core.Models;
using BaitBuster.Core.Parsing;
using Microsoft.AspNetCore.Mvc;

namespace BaitBuster.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class AnalysisController(EmlParser parser, DetectionEngine engine)
    : ControllerBase
{
    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB

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

        return Ok(engine.Analyze(email));
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

        return Ok(engine.Analyze(email));
    }
}