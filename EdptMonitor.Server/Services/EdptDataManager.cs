using System.Collections.Concurrent;
using EdptMonitor.Shared;

namespace EndpointMtr.Server.Services;

public class EdptDataManager
{
    public ConcurrentQueue<EdptStatusMessage> EndpointStatusMessages { get; set; }
    public ConcurrentQueue<EdptInfoMessage> EndpointInfoMessages { get; set; }
    public ConcurrentQueue<EdptProcessMessage> EndpointProcessMessages { get; set; }
    public ConcurrentDictionary<string, Edpt> ConnectedEndpoints { get; set; }
    
}