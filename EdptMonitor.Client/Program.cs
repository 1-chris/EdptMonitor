using System.Runtime.InteropServices;
using EdptMonitor.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

if (args.Length > 0)
{
    if (args[0] == "uninstall")
    {
        EndpointMonitorClientInstaller.Uninstall();
        return;
    }

    if (args[0] == "install" && args.Length == 3)
    {
        EndpointMonitorClientInstaller.Install(args[1], args[2]);
        return;
    }

    throw new Exception("Invalid arguments.");
}

var builder = new HostBuilder()
    .ConfigureServices((hostContext, services) =>
    {
        services.AddHostedService<EndpointMonitorClientService>();
    });

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
    builder.UseWindowsService();
}

await builder.Build().RunAsync();