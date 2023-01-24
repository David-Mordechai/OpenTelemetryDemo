using Microsoft.AspNetCore.Mvc;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

// Define some important constants to initialize tracing with
const string serviceName = "OpenTelemetryDemo.WebApi";
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
        b.AddPrometheusExporter();
        
        // receive metrics from our own custom sources
        b.AddMeter(serviceName);

        // decorate our service name so we can find it when we look inside Prometheus
        b.SetResourceBuilder(resource);

        // receive metrics from built-in sources
        b.AddHttpClientInstrumentation();
        b.AddAspNetCoreInstrumentation();
    });


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/weatherforecast", async ([FromServices]IHttpClientFactory httpClient) =>
{
    using var client = httpClient.CreateClient();
    var forecast = await client.GetFromJsonAsync<WeatherForecast[]?>("http://localhost:5156/weatherforecast");
    return forecast;
})
.WithName("GetWeatherForecast");

//// add the /metrics endpoint which will be scraped by Prometheus
//app.UseOpenTelemetryPrometheusScrapingEndpoint();

app.Run();

internal record WeatherForecast(DateTime Date, int TemperatureC, string? Summary, int TemperatureF);