using Microsoft.AspNetCore.SignalR.Client;

namespace EdptMonitor.Client;

public class SignalRRetryPolicy : IRetryPolicy
{
    public TimeSpan? NextRetryDelay(RetryContext retryContext)
    {
        return TimeSpan.FromSeconds(30);
    }
    
}