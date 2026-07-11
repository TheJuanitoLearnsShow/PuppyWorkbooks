using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PuppyWorkbooks.CLI;

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddHostedService<WorkbooksWorker>();

IHost host = builder.Build();
host.Run();