using EdptMonitor.Shared;
using EndpointMtr.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace EndpointMtr.Server.Hubs;

public class EndpointHub : Hub
{
    private static EndpointDataManager _dataManager;
    
    public EndpointHub(EndpointDataManager dataManager)
    {
        _dataManager = dataManager;
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"Client connected {Context.ConnectionId}");
        base.OnConnectedAsync();
    }

    public async Task EndpointStatus(EndpointStatusMessage statusMessage)
    {
        Console.WriteLine($"Received: {statusMessage}");
    }
}