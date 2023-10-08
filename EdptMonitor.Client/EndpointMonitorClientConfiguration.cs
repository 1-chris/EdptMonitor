namespace EdptMonitor.Client;

public class EndpointMonitorClientConfiguration
{
    public string EndpointUrl { get; set; } = "";
    public string AuthorizationToken { get; set; } = "";
    public int DataCollectionInterval { get; set; } = 60000;
}