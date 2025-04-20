using Grafana.OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);


var serviceName = builder.Configuration["OpenTelemetry:ServiceName"];
Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", builder.Configuration["OpenTelemetry:Attributes"]);
Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", serviceName);
Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", builder.Configuration["OpenTelemetry:OltpEndpoint"]);
Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", builder.Configuration["OpenTelemetry:Protocol"]);


String apiKey = builder.Configuration["OpenTelemetry:ApiToken"];
String instanceId= builder.Configuration["OpenTelemetry:InstanceID"];
String auth = instanceId+ ":" + apiKey;
String base64Auth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(auth));


Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS", "Authorization=Basic " + base64Auth);


builder.Services.AddOpenTelemetry()
    .WithTracing(configure =>
    {
        configure.UseGrafana().AddAspNetCoreInstrumentation().
        AddOtlpExporter();
    })
    .WithMetrics(configure =>
    {
        configure.UseGrafana().AddAspNetCoreInstrumentation().
        AddPrometheusExporter().
        AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel")
            .AddOtlpExporter();
    });
builder.Logging.AddOpenTelemetry(options =>
{
    options.UseGrafana().
        AddOtlpExporter();
});



// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseOpenTelemetryPrometheusScrapingEndpoint();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    app.Logger.LogInformation("Starting weather");
    Thread.Sleep(Random.Shared.Next(10, 1000));
    if (Random.Shared.Next(0, 100) > 80)
    {
        app.Logger.LogError("Something went wrong");
        throw new Exception("Stupid Error");
    }
    app.Logger.LogInformation("SQL was SELECT * FROM WHATEVER");
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

internal record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
