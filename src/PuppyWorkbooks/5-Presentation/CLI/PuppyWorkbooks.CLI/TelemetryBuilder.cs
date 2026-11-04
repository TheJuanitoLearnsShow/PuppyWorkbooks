using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PuppyWorkbooks.Integration.Engine;

namespace PuppyWorkbooks.CLI;

public class TelemetryBuilder
{
    public static void AddTelemetry(HostApplicationBuilder builder)
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(
                    serviceName: TelemetryManager.ActivitySourceName,
                    serviceVersion: "1.0.0",
                    serviceInstanceId: Environment.MachineName))
            .WithTracing(tracing => tracing
                .AddHttpClientInstrumentation(options => { options.RecordException = true; })
                .AddSource(TelemetryManager.ActivitySourceName)
                .AddOtlpExporter(options =>
                {
                    // Aspire Dashboard default endpoint
                    options.Endpoint = new Uri("http://localhost:4317");
                    options.Protocol = OtlpExportProtocol.Grpc;
                }));

// Configure logging to send to Aspire Dashboard
        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
            logging.AddOtlpExporter(options =>
            {
                options.Endpoint = new Uri("http://localhost:4317");
                options.Protocol = OtlpExportProtocol.Grpc;
            });
        });
    }
}