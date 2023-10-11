using EdptMonitor.Shared;
using EndpointMtr.Server.Services;
using Microsoft.AspNetCore.SignalR;
using System.Text.Json;

namespace EndpointMtr.Server.Hubs;

public class EdptHub : Hub
{
    private readonly EdptDataManager _dataManager;
    private readonly ILogger<EdptHub> _logger;
    
    public EdptHub(EdptDataManager dataManager, ILogger<EdptHub> logger)
    {
        _dataManager = dataManager;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"Client connected {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }
    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
        _dataManager.ConnectedEndpoints.TryRemove(Context.ConnectionId, out _);
        
        return base.OnDisconnectedAsync(exception);
    }

    public async Task EdptReady(string computerName)
    {
        _logger.LogInformation($"Ready message received from {Context.ConnectionId} : {computerName}");
        Edpt endpoint = new();
        endpoint.ComputerName = computerName;
        endpoint.ConnectionId = Context.ConnectionId;
        _dataManager.ConnectedEndpoints.TryAdd(Context.ConnectionId, endpoint);
    }
    
    public async Task EdptStatus(EdptStatusMessage statusMessage)
    {
        _logger.LogInformation($"{JsonSerializer.Serialize(statusMessage)}");
        _dataManager.EndpointStatusMessages.Enqueue(statusMessage);
    }
    
    public async Task EdptProcess(EdptProcessMessage processMessage)
    {
        _logger.LogInformation($"{JsonSerializer.Serialize(processMessage)}");
        _dataManager.EndpointProcessMessages.Enqueue(processMessage);
    }
}