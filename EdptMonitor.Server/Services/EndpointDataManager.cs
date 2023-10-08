using System.Collections.Concurrent;
using EdptMonitor.Shared;

namespace EndpointMtr.Server.Services;

public class EndpointDataManager
{
    public ConcurrentQueue<EndpointStatusMessage> EndpointStatusMessages { get; set; }
    public ConcurrentQueue<EndpointInfoMessage> EndpointInfoMessages { get; set; }
    public ConcurrentQueue<EndpointProcessMessage> EndpointProcessMessages { get; set; }
    
}