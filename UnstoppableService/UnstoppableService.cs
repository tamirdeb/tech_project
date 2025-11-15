namespace UnstoppableService;

public class UnstoppableService : BackgroundService
{
    private readonly ILogger<UnstoppableService> _logger;
    private readonly WfpController _wfpController;

    public UnstoppableService(ILogger<UnstoppableService> logger)
    {
        _logger = logger;
        _wfpController = new WfpController();
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unstoppable Service is starting.");
        ProcessProtector.Protect();
        _wfpController.Start();
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unstoppable Service is stopping.");
        _wfpController.Stop();
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

    }
}
