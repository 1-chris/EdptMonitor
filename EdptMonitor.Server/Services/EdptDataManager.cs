using System.Collections.Concurrent;
using EdptMonitor.Shared;

namespace EndpointMtr.Server.Services;

public class EdptDataManager
{
    public ConcurrentQueue<EdptStatusMessage> EndpointStatusMessages { get; set; } = new();
    public ConcurrentQueue<EdptProcessMessage> EndpointProcessMessages { get; set; } = new();
    public ConcurrentDictionary<string, Edpt> ConnectedEndpoints { get; set; } = new();
    
}