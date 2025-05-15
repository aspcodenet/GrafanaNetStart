using Grafana.OpenTelemetry;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);


var serviceName = builder.Configuration["OpenTelemetry:ServiceName"];
var endpoint = builder.Configuration["OpenTelemetry:OltpEndpoint"];
var protocol = builder.Configuration["OpenTelemetry:Protocol"];

//Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", builder.Configuration["OpenTelemetry:Attributes"]);
//Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", serviceName);
//Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", endpoint);
//Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", builder.Configuration["OpenTelemetry:Protocol"]);


String apiKey = builder.Configuration["OpenTelemetry:ApiToken"];
String instanceId= builder.Configuration["OpenTelemetry:InstanceID"];
String auth = instanceId+ ":" + apiKey;
String base64Auth = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(auth));


//Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_HEADERS", "Authorization=Basic " + base64Auth);
//if(base64Auth != "MTIzMzE4MjpnbGNfZXlKdklqb2lNVFF3TnpRNE9DSXNJbTRpT2lKemRHRmpheTB4TWpNek1UZ3lMVzkwYkhBdGQzSnBkR1V0ZEdWemRERXhNVEV4TVNJc0ltc2lPaUl3T1RJek1XbHRTMFpoTXpRNWRFdEJkalkxV0ZablNrNGlMQ0p0SWpwN0luSWlPaUp3Y205a0xXVjFMVzV2Y25Sb0xUQWlmWDA9")
//{
//    int j;
//    j = 99;
//}




builder.Services.AddOpenTelemetry()
    .WithTracing(configure =>
    {
        configure.UseGrafana().AddAspNetCoreInstrumentation()
            .AddOtlpExporter(options=>{
                options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                options.Endpoint = new Uri(endpoint + "/v1/traces");
                options.Headers = "Authorization=Basic " + base64Auth;
    }).AddConsoleExporter();
    })
    .WithMetrics(configure =>
    {
        configure.UseGrafana().AddAspNetCoreInstrumentation().
        AddPrometheusExporter().
        AddMeter("Microsoft.AspNetCore.Hosting", "Microsoft.AspNetCore.Server.Kestrel")

                        .AddOtlpExporter(options =>
                        {
                            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                            options.Endpoint = new Uri(endpoint + "/v1/metrics");
                            options.Headers = "Authorization=Basic " + base64Auth;
                        })
                .AddConsoleExporter();
    });
builder.Logging.AddOpenTelemetry(options =>
{
    options.UseGrafana()


                        .AddOtlpExporter(options =>
                        {
                            options.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.HttpProtobuf;
                            options.Endpoint = new Uri(endpoint + "/v1/logs");
                            options.Headers = "Authorization=Basic " + base64Auth;
                        })
            .AddConsoleExporter();
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
    if (Random.Shared.Next(0, 100) > 110)
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
