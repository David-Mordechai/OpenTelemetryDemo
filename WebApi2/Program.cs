using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

const string serviceName = "OpenTelemetryDemo.WebApi2";
const string serviceVersion = "1.0.0";
var resource = ResourceBuilder.CreateDefault().AddService(serviceName: serviceName, serviceVersion: serviceVersion);

builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"))
            .AddSource(serviceName)
            .SetResourceBuilder(resource)
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation();
    })
    .WithMetrics(meterProviderBuilder =>
    {
        meterProviderBuilder
            .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"))
            .AddMeter(serviceName)
            .SetResourceBuilder(resource)
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation();
    })
    .StartWithHost();

builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(telemetryLoggerOptions =>
{
    telemetryLoggerOptions.SetResourceBuilder(resource);
    telemetryLoggerOptions.AddOtlpExporter(otlpExporterOptions =>
    {
        otlpExporterOptions.Endpoint = new Uri("http://localhost:4317");
    });
    telemetryLoggerOptions.IncludeFormattedMessage = true;
}).SetMinimumLevel(LogLevel.Information);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast2", ([FromServices] ILogger<Program> logger) =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateTime.Now.AddDays(index),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    logger.LogInformation("Sending response to WebApi");
    return forecast;
})
.WithName("GetWeatherForecast2");

app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}