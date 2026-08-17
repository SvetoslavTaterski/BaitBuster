using System.Text.Json.Serialization;
using BaitBuster.Api.Persistence;
using BaitBuster.Core.Detection;
using BaitBuster.Core.Detection.Ml;
using BaitBuster.Core.Detection.Rules;
using BaitBuster.Core.Parsing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.ML;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

// Persistence — SQLite за история на анализите.
// Пазим файла извън дървото на проекта (в LocalApplicationData), защото
// нативната SQLite библиотека на Windows има проблеми с non-ASCII (кирилица)
// сегменти в пътя при P/Invoke marshaling — проектната папка тук съдържа такива.
var dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BaitBuster");
Directory.CreateDirectory(dataDir);
var dbPath = Path.Combine(dataDir, "baitbuster.db");
builder.Services.AddDbContext<BaitBusterDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// Ядро на анализа
builder.Services.AddSingleton<EmlParser>();
builder.Services.AddSingleton<DetectionEngine>();

// Обученият ML модел. PredictionEnginePool се грижи за thread-safety —
// самият PredictionEngine не е безопасен за паралелни заявки.
builder.Services
    .AddPredictionEnginePool<EmailData, EmailPrediction>()
    .FromFile(Path.Combine(AppContext.BaseDirectory, "models", "phishing-model.zip"));

// Детекционни правила — всяко ново правило се добавя само тук.
builder.Services.AddSingleton<IDetectionRule, HeaderMismatchRule>();
builder.Services.AddSingleton<IDetectionRule, UrlAnalysisRule>();
builder.Services.AddSingleton<IDetectionRule, UrgencyContentRule>();
builder.Services.AddSingleton<IDetectionRule, MlClassifierRule>();

// CORS за Angular dev сървъра
builder.Services.AddCors(o => o.AddPolicy("angular", p =>
    p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<BaitBusterDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("angular");
app.MapControllers();

app.Run();