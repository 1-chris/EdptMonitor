using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using EdptMonitor.Shared;
using Microsoft.Win32;
using Microsoft.AspNetCore.SignalR.Client;

namespace EdptMonitor.Client;

public class EndpointMonitorClientAgent
{
    public int DataCollectionInterval = 60000;
    public string EndpointBase { get; set; } = "https://your.domain.com/";
    public string AuthorizationToken { get; set; } = "";
    public Guid DeviceId { get; set; }
    public Guid AzureADDeviceId { get; set; }
    public string ComputerName { get; set; }
    public DateTime LastBootUpTime { get; set; }
    public int TotalPhysicalMemoryMb { get; set; }

    private Dictionary<int, Process> _previousProcesses;
    private HubConnection? _hubConnection;
    

    public EndpointMonitorClientAgent(EndpointMonitorClientConfiguration config)
    {
        EndpointBase = config.EndpointUrl;
        AuthorizationToken = config.AuthorizationToken;
        DataCollectionInterval = config.DataCollectionInterval;

        DeviceId = GetDeviceId();
        AzureADDeviceId = GetAzureAdDeviceId();
        ComputerName = Environment.MachineName;
        LastBootUpTime = GetLastBootUpTime();
        TotalPhysicalMemoryMb = GetTotalPhysicalMemory();

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(EndpointBase + "EdptHub")
            .WithAutomaticReconnect()
            .Build();
    }
    
    public async void Initiate()
    {
        await _hubConnection?.StartAsync()!;
        
        _previousProcesses = GetCurrentProcesses();

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            try {
                string processCreationQuery = "SELECT * FROM Win32_ProcessStartTrace";
                ManagementEventWatcher processCreationWatcher = new ManagementEventWatcher(processCreationQuery);
                processCreationWatcher.EventArrived += (sender, e) => ProcessCreationWatcher_EventArrived(sender, e);
                processCreationWatcher.Start();
                Console.WriteLine("Watching processes.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Unable to start process creation watcher: {ex.Message}");
            }
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
            _ = WatchProcessesAsync();
        }

        while (true)
        {
            var freeMemory = GetFreePhysicalMemoryAsync();
            var cpuLoad = GetCpuLoadAsync();
            var pingMs = GetPingMsAsync();
            var freeStorage = GetFreeStorageAsync();
            var processCount = Process.GetProcesses().Length;
            var uptimeTotalDays = (DateTime.Now - LastBootUpTime).TotalDays;

            EdptStatusMessage data = new()
            {
                DeviceId = DeviceId,
                AzureADDeviceId = AzureADDeviceId,
                ComputerName = ComputerName,
                LastBootUpTime = LastBootUpTime,
                CpuLoad = await cpuLoad,
                ProcessCount = processCount,
                FreeStorageMB = await freeStorage,
                FreePhysicalMemoryMB = await freeMemory,
                PingMs = await pingMs,
                TimeGenerated = DateTime.UtcNow
            };
            
            _hubConnection.SendAsync("EdptStatus", data);
            await Task.Delay(DataCollectionInterval);
        }
    }   
    public async Task WatchProcessesAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromSeconds(1));

            var currentProcesses = GetCurrentProcesses();
            var newProcesses = currentProcesses.Keys.Except(_previousProcesses.Keys);

            foreach (var processId in newProcesses)
            {
                Process process = currentProcesses[processId];

                EdptProcessMessage data = new()
                {
                    TimeGenerated = DateTime.UtcNow,
                    DeviceId = DeviceId,
                    AzureADDeviceId = AzureADDeviceId,
                    ComputerName = ComputerName,
                    ProcessName = process.ProcessName,
                    ProcessId = process.Id,
                };
        
                await _hubConnection.SendAsync("EdptProcess", data);
            }

            _previousProcesses = currentProcesses;
        }
    }
    public static Dictionary<int, Process> GetCurrentProcesses()
    {
        return Process.GetProcesses().ToDictionary(p => p.Id);
    }
    public async Task ProcessCreationWatcher_EventArrived(object sender, EventArrivedEventArgs e)
    {
        Console.WriteLine("New processes created");
        EdptProcessMessage data = new()
        {
            TimeGenerated = DateTime.UtcNow,
            DeviceId = DeviceId,
            AzureADDeviceId = AzureADDeviceId,
            ComputerName = ComputerName,
            ProcessName = e.NewEvent.Properties["ProcessName"].Value.ToString(),
            ProcessId = (int)e.NewEvent.Properties["ProcessID"].Value,
        };
        
        await _hubConnection.SendAsync("EdptProcess", data);
    }
    public static Guid GetDeviceId()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Provisioning\Diagnostics\Autopilot\EstablishedCorrelations", false);
            var value = key?.GetValue("EntDMID", "00000000-0000-0000-0000-000000000000") as string;
            var success = Guid.TryParse(value, out var deviceId);
            if (success) return deviceId;
        }
        // TODO: implement for Linux
        return Guid.Empty;
    }
    public static Guid GetAzureAdDeviceId()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
            var startInfo = new ProcessStartInfo("dsregcmd", "/status")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = new Process { StartInfo = startInfo };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var match = Regex.Match(output, @"DeviceId\s*:\s*(?<id>[^ ]+)");

            var guidString =  match.Success ? match.Groups["id"].Value.Trim() : "00000000-0000-0000-0000-000000000000";
            var success = Guid.TryParse(guidString, out var azureAdDeviceId);
            
            if (success) return azureAdDeviceId;
        }
        
        // TODO: implement for Linux
        return Guid.Empty;
    }
    public static DateTime GetLastBootUpTime()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var searcher = new ManagementObjectSearcher("SELECT LastBootUpTime FROM Win32_OperatingSystem");
            using var enumerator = searcher.Get().GetEnumerator();
            enumerator.MoveNext();
            var managementLastBootUpTime = enumerator.Current["LastBootUpTime"].ToString();
            if (managementLastBootUpTime is null)
                throw new Exception("Could not get last boot up time.");

            var lastBootUpTime = ManagementDateTimeConverter.ToDateTime(managementLastBootUpTime);
            return lastBootUpTime;
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var uptimeString = File.ReadAllText("/proc/uptime");
            var uptimeSeconds = double.Parse(uptimeString.Split()[0]);
            var lastBootUpTime = DateTime.UtcNow - TimeSpan.FromSeconds(uptimeSeconds);
            return lastBootUpTime;
        }

        throw new NotSupportedException("The current operating system is not supported.");
    }
    public static async Task<double> GetFreePhysicalMemoryAsync()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var searcher = new ManagementObjectSearcher("SELECT FreePhysicalMemory FROM Win32_OperatingSystem");
            using var enumerator = searcher.Get().GetEnumerator();
            enumerator.MoveNext();
            double freeMemory = Convert.ToDouble(enumerator.Current["FreePhysicalMemory"]) / 1024;
            return freeMemory;
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var startInfo = new ProcessStartInfo("free", "-m")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = new Process { StartInfo = startInfo };
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            process.WaitForExit();

            var match = Regex.Match(output, @"Mem:\s+\d+\s+\d+\s+(?<free>\d+)");
            return match.Success ? double.Parse(match.Groups["free"].Value) : 0;
        }
        
        throw new NotSupportedException("The current operating system is not supported.");
    }
    public static int GetTotalPhysicalMemory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var searcher = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
            using var enumerator = searcher.Get().GetEnumerator();
            enumerator.MoveNext();
            double totalMemory = Convert.ToDouble(enumerator.Current["TotalVisibleMemorySize"]) / 1024;
            return (int)totalMemory;
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var startInfo = new ProcessStartInfo("free", "-m")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = new Process { StartInfo = startInfo };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var match = Regex.Match(output, @"Mem:\s+(?<total>\d+)\s+\d+\s+\d+");
            var success = int.TryParse(match.Groups["total"].Value, out var totalMemoryMb);
            return match.Success ? totalMemoryMb : 0;
        }
        
        throw new NotSupportedException("The current operating system is not supported.");
    }
    public static async Task<double> GetCpuLoadAsync()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            using var searcher = new ManagementObjectSearcher("SELECT LoadPercentage FROM Win32_Processor");
            using var enumerator = searcher.Get().GetEnumerator();
            enumerator.MoveNext();
            double cpuLoad = Convert.ToDouble(enumerator.Current["LoadPercentage"]);
            return cpuLoad;
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var startInfo = new ProcessStartInfo("top", "-bn1")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = new Process { StartInfo = startInfo };
            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            process.WaitForExit();

            var match = Regex.Match(output, @"%Cpu\(s\):\s+(?<load>[0-9.]+)\s+us");
            return match.Success ? double.Parse(match.Groups["load"].Value) : 0;
        }

        throw new NotSupportedException("The current operating system is not supported.");
    }
    public static async Task<long> GetPingMsAsync()
    {
        using var ping = new System.Net.NetworkInformation.Ping();
        var reply = await ping.SendPingAsync("1.1.1.1");
        return reply.RoundtripTime;
    }
    public static Task<long> GetFreeStorageAsync()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var driveInfo = new DriveInfo("C");
            return Task.FromResult(driveInfo.AvailableFreeSpace / 1024 / 1024);
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var startInfo = new ProcessStartInfo("df", "-m --output=avail /")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = new Process { StartInfo = startInfo };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var match = Regex.Match(output, @"\n(?<free>\d+)");
            return Task.FromResult(match.Success ? long.Parse(match.Groups["free"].Value) : 0);
        }

        throw new NotSupportedException("The current operating system is not supported.");
    }
}