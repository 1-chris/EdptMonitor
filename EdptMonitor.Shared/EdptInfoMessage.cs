namespace EdptMonitor.Shared;

public class EdptInfoMessage
{
    public DateTime TimeGenerated { get; set; }
    public Guid DeviceId { get; set; }
    public Guid AzureAdDeviceId { get; set; }
    public string ComputerName { get; set; } = "";
    public DateTime LastBootUpTime { get; set; }
    public double TotalPhysicalMemory { get; set; }
    public string OSEnvironment { get; set; } = "";
}