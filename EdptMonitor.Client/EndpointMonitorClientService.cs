using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Hosting;

namespace EdptMonitor.Client;

public class EndpointMonitorClientService : BackgroundService
{
    private readonly ManualResetEvent _shutdownEvent = new ManualResetEvent(false);
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new EndpointMonitorClientConfiguration();
        config = null;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var configPath = Path.Combine(appDataPath, "EdptMonitorClient", "config.json");
            if (File.Exists(configPath))
                config = JsonSerializer.Deserialize<EndpointMonitorClientConfiguration>(File.ReadAllText(configPath));
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var path = Path.Combine("/etc", "EdptMonitorClient", "config.json");
            if (File.Exists(path))
                config = JsonSerializer.Deserialize<EndpointMonitorClientConfiguration>(path);
        }
        
        if (config is null)
            throw new Exception($"Create a config.json file in C:\\ProgramData\\EdptMonitorClient\\ or /etc/EdptMonitorClient/ with the following content: {JsonSerializer.Serialize(new EndpointMonitorClientConfiguration())}");

        Console.WriteLine("Service has started.");
        var clientAgent = new EndpointMonitorClientAgent(config);
        clientAgent.Initiate();

        stoppingToken.Register(() =>
        {
            Console.WriteLine("Service has stopped.");
            _shutdownEvent.Set();
        });

        await Task.Run(() => _shutdownEvent.WaitOne());
    }
}