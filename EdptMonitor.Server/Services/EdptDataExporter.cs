namespace EndpointMtr.Server.Services;

public class EdptDataExporter : BackgroundService
{
    private readonly EdptDataManager _dataManager;

    public EdptDataExporter(EdptDataManager dataManager)
    {
        _dataManager = dataManager;
    }
    
    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            

            await Task.Delay(10000, stoppingToken);
        }
        
    }
    
}