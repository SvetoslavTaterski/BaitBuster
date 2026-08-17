using System.Text.Json;
using BaitBuster.Core.Detection.Ml;
using Microsoft.AspNetCore.Mvc;

namespace BaitBuster.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ModelController : ControllerBase
{
    private static readonly string MetadataPath =
        Path.Combine(AppContext.BaseDirectory, "models", "phishing-model.json");

    private static readonly JsonSerializerOptions ReadOptions =
        new() { PropertyNameCaseInsensitive = true };

    /// <summary>Информация за обучения ML модел — метрики, състав на данните, дата.</summary>
    [HttpGet("info")]
    [ProducesResponseType<ModelMetadata>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ModelMetadata>> GetInfo()
    {
        if (!System.IO.File.Exists(MetadataPath))
            return NotFound("Няма обучен модел. Стартирайте BaitBuster.MlTraining.");

        await using var stream = System.IO.File.OpenRead(MetadataPath);
        var metadata = await JsonSerializer.DeserializeAsync<ModelMetadata>(stream, ReadOptions);

        return metadata is null
            ? NotFound("Метаданните на модела не могат да бъдат разчетени.")
            : Ok(metadata);
    }
}
