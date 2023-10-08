namespace EdptMonitor.Shared;

public class EndpointProcessMessage
{
    public DateTime TimeGenerated { get; set; }
    public Guid DeviceId { get; set; }
    public Guid AzureADDeviceId { get; set; }
    public string ComputerName { get; set; }
    public string ProcessName { get; set; }
    public int ProcessId { get; set; }
}