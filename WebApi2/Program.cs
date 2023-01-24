using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Define some important constants to initialize tracing with
const string serviceName = "OpenTelemetryDemo.WebApi2";
const string serviceVersion = "1.0.0";
var resource = ResourceBuilder.CreateDefault().AddService(serviceName: serviceName, serviceVersion: serviceVersion);

// Configure important OpenTelemetry settings, the console exporter, and instrumentation library
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProviderBuilder =>
    {
        tracerProviderBuilder
            .AddJaegerExporter()
            .AddConsoleExporter()
            .AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"))
            .AddSource(serviceName)
            .SetResourceBuilder(resource)
            .AddHttpClientInstrumentation()
            .AddAspNetCoreInstrumentation();
    })
    .WithMetrics(b =>
    {
        // add prometheus exporter
        //b.AddPrometheusExporter();
        b.AddOtlpExporter(options => options.Endpoint = new Uri("http://localhost:4317"));

        // receive metrics from our own custom sources
        b.AddMeter(serviceName);

        // decorate our service name so we can find it when we look inside Prometheus
        b.SetResourceBuilder(resource);

        // receive metrics from built-in sources
        b.AddHttpClientInstrumentation();
        b.AddAspNetCoreInstrumentation();
    })
    .StartWithHost();

// Clear default logging providers used by WebApplication host.
builder.Logging.ClearProviders();
// Configure OpenTelemetry Logging.
builder.Logging.AddOpenTelemetry(options =>
{
    options.SetResourceBuilder(resource);
    options.AddOtlpExporter(otlpOptions =>
    {
        // Use IConfiguration directly for Otlp exporter endpoint option.
        otlpOptions.Endpoint = new Uri("http://localhost:4317");
    });
    options.IncludeFormattedMessage = true;
}).SetMinimumLevel(LogLevel.Information);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", ([FromServices]ILogger<Program> logger) =>
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
.WithName("GetWeatherForecast");

app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}