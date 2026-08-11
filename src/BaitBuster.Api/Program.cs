using System.Text.Json.Serialization;
using BaitBuster.Core.Detection;
using BaitBuster.Core.Detection.Rules;
using BaitBuster.Core.Parsing;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();

// Ядро на анализа
builder.Services.AddSingleton<EmlParser>();
builder.Services.AddSingleton<DetectionEngine>();

// Детекционни правила — всяко ново правило се добавя само тук.
// По-късно ML класификаторът се регистрира по същия начин.
builder.Services.AddSingleton<IDetectionRule, HeaderMismatchRule>();
builder.Services.AddSingleton<IDetectionRule, UrlAnalysisRule>();
builder.Services.AddSingleton<IDetectionRule, UrgencyContentRule>();

// CORS за Angular dev сървъра
builder.Services.AddCors(o => o.AddPolicy("angular", p =>
    p.WithOrigins("http://localhost:4200").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("angular");
app.MapControllers();

app.Run();