namespace EdptMonitor.Shared;

public class EdptProcessMessage
{
    public DateTime TimeGenerated { get; set; }
    public Guid DeviceId { get; set; }
    public Guid AzureAdDeviceId { get; set; }
    public string? ComputerName { get; set; }
    public string? ProcessName { get; set; }
    public int ProcessId { get; set; }
}