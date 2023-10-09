using EdptMonitor.Shared;
using EndpointMtr.Server.Services;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace EndpointMtr.Server.Hubs;

public class EdptHub : Hub
{
    private readonly EdptDataManager _dataManager;
    
    public EdptHub(EdptDataManager dataManager)
    {
        _dataManager = dataManager;
    }

    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"Client connected {Context.ConnectionId}");
        
        await base.OnConnectedAsync();
    }

    public async Task EdptReady(string computerName)
    {
        Console.WriteLine($"Ready message received from {Context.ConnectionId} : {computerName}");
        Edpt endpoint = new();
        endpoint.ComputerName = computerName;
        endpoint.ConnectionId = Context.ConnectionId;
        _dataManager.ConnectedEndpoints.TryAdd(Context.ConnectionId, endpoint);
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
        _dataManager.ConnectedEndpoints.TryRemove(Context.ConnectionId, out _);
        
        return base.OnDisconnectedAsync(exception);
    }

    public async Task EdptStatus(EdptStatusMessage statusMessage)
    {
        Console.WriteLine($"Received: {JsonSerializer.Serialize(statusMessage)}");
        _dataManager.EndpointStatusMessages.Enqueue(statusMessage);
    }
}