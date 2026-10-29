using Azure.Monitor.OpenTelemetry.Exporter;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Trace;
using PuppyWorkbooks.CLI;
using PuppyWorkbooks.Integration.Engine;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
var azureMonitorConnectionString = new[]
    {
        "APPLICATIONINSIGHTS_CONNECTION_STRING",
        "ApplicationInsights:ConnectionString",
        "AzureMonitor:ConnectionString",
        "ConnectionStrings:AzureMonitor"
    }
    .Select(key => builder.Configuration[key])
    .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

if (!string.IsNullOrWhiteSpace(azureMonitorConnectionString))
{
    builder.Services.AddOpenTelemetry()
        .WithTracing(tracing => tracing
            .AddSource(TelemetryManager.ActivitySourceName)
            .AddAzureMonitorTraceExporter(options =>
                options.ConnectionString = azureMonitorConnectionString));
}

builder.Services.Configure<ExecutionSettings>(builder.Configuration);
builder.Services.AddTransient<PuppyWorksheetWorker>();
builder.Services.AddTransient<PuppyIntegrationWorker>();
builder.Services.AddHostedService<WorkbooksWorker>();

IHost host = builder.Build();
host.Run();
