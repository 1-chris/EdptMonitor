namespace EdptMonitor.Shared;

public class EdptStatusMessage
{
    public DateTime TimeGenerated { get; set; } = DateTime.UtcNow;
    public Guid DeviceId { get; set; }
    public Guid AzureADDeviceId { get; set; }
    public string ComputerName { get; set; }
    public DateTime LastBootUpTime { get; set; }
    public double FreePhysicalMemoryMB { get; set; }
    public double CpuLoad { get; set; }
    public int ProcessCount { get; set; }
    public long FreeStorageMB { get; set; }
    public double PingMs { get; set; }
}