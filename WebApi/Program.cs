using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Logs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

const string serviceName = "OpenTelemetryDemo.WebApi";
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

app.MapGet("/weatherforecast", async ([FromServices]IHttpClientFactory httpClient, [FromServices]ILogger<Program> logger) =>
{
    logger.LogInformation("Sending request to WebApi2...");
    using var client = httpClient.CreateClient();
    var forecast = await client.GetFromJsonAsync<WeatherForecast[]?>("http://localhost:5156/weatherforecast2");
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary, int TemperatureF);