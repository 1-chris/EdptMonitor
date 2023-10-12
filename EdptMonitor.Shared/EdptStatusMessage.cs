namespace EdptMonitor.Shared;

public class EdptStatusMessage
{
    public DateTime TimeGenerated { get; set; } = DateTime.UtcNow;
    public Guid DeviceId { get; set; }
    public Guid AzureAdDeviceId { get; set; }
    public string? ComputerName { get; set; }
    public DateTime LastBootUpTime { get; set; }
    public double FreePhysicalMemoryMb { get; set; }
    public double CpuLoad { get; set; }
    public int ProcessCount { get; set; }
    public long FreeStorageMb { get; set; }
    public double PingMs { get; set; }
}