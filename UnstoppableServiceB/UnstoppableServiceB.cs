using UnstoppableService;

namespace UnstoppableServiceB;

public class UnstoppableServiceB : BackgroundService
{
    private readonly ILogger<UnstoppableServiceB> _logger;
    private readonly ServiceMonitor _serviceMonitor;
    private Timer? _monitorTimer;

    public UnstoppableServiceB(ILogger<UnstoppableServiceB> logger)
    {
        _logger = logger;
        _serviceMonitor = new ServiceMonitor("UnstoppableService", _logger);
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unstoppable Service B is starting.");

        _monitorTimer = new Timer(
            _ => _serviceMonitor.Monitor(),
            null,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(5));

        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unstoppable Service B is stopping.");
        _monitorTimer?.Dispose();
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

    }
}
