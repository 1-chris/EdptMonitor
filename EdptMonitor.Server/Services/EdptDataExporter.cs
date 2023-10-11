using System.Collections;
using System.Collections.Concurrent;
using Azure.Monitor.Ingestion;
using EdptMonitor.Shared;

namespace EndpointMtr.Server.Services;

public class EdptDataExporter : BackgroundService
{
    private readonly EdptDataManager _dataManager;
    private readonly LogsIngestionClient _ingestionClient;
    private readonly ILogger<EdptDataExporter> _logger;
    private readonly string _logIngestionStatusMessageRuleId;
    private readonly string _logIngestionStatusMessageStreamName;
    private readonly string _logIngestionProcessMessageRuleId;
    private readonly string _logIngestionProcessMessageStreamName;
    private const int MaxBatch = 100;

    public EdptDataExporter(ILogger<EdptDataExporter> logger, EdptDataManager dataManager, LogsIngestionClient ingestionClient, IConfiguration configuration)
    {
        _logger = logger;
        _dataManager = dataManager;
        _ingestionClient = ingestionClient;

        _logIngestionStatusMessageRuleId = configuration["LogIngestionStatusMessageRuleId"] ?? throw new InvalidOperationException();
        _logIngestionStatusMessageStreamName = configuration["LogIngestionStatusMessageStreamName"] ?? throw new InvalidOperationException();
        _logIngestionProcessMessageRuleId = configuration["LogIngestionProcessMessageRuleId"] ?? throw new InvalidOperationException();
        _logIngestionProcessMessageStreamName = configuration["LogIngestionProcessMessageStreamName"] ?? throw new InvalidOperationException();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // _logger.LogInformation("Running exports....");
            await ExportStatusMessages();
            //await ExportProcessMessages();

            await Task.Delay(10000, stoppingToken);
        }
    }

    private async Task ExportStatusMessages()
    {
        ConcurrentQueue<EdptStatusMessage> statusMessagesToSend = new();
        while (_dataManager.EndpointStatusMessages.TryDequeue(out var messageToSend) && statusMessagesToSend?.Count <= MaxBatch)
        {
            if (messageToSend != null) statusMessagesToSend?.Enqueue(messageToSend);
            await Task.Delay(100);
        }

        if (statusMessagesToSend?.Count > 0)
        {
            _logger.LogInformation("Sending data....");
            
            await _ingestionClient.UploadAsync(
                _logIngestionStatusMessageRuleId, 
                _logIngestionStatusMessageStreamName, 
                statusMessagesToSend
            );
        }
    }

    private async Task ExportProcessMessages()
    {
        ConcurrentQueue<EdptStatusMessage> processMessagesToSend = new();
        while (_dataManager.EndpointStatusMessages.TryDequeue(out var messageToSend) && processMessagesToSend?.Count <= MaxBatch)
        {
            if (messageToSend != null) processMessagesToSend?.Enqueue(messageToSend);
        }
            
        if (processMessagesToSend?.Count > 0) 
            await _ingestionClient.UploadAsync(
                _logIngestionProcessMessageRuleId, 
                _logIngestionProcessMessageStreamName, 
                processMessagesToSend
            );

    }
    
}