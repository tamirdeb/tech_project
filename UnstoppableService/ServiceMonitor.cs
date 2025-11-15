using System.ServiceProcess;

namespace UnstoppableService;

public class ServiceMonitor
{
    private readonly string _serviceToMonitor;
    private readonly ILogger _logger;

    public ServiceMonitor(string serviceToMonitor, ILogger logger)
    {
        _serviceToMonitor = serviceToMonitor;
        _logger = logger;
    }

    public void Monitor()
    {
        try
        {
            var service = new ServiceController(_serviceToMonitor);
            if (service.Status != ServiceControllerStatus.Running)
            {
                _logger.LogWarning("{Service} is not running. Restarting...", _serviceToMonitor);
                service.Start();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to monitor {Service}", _serviceToMonitor);
        }
    }
}
