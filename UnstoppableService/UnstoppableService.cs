using System.ServiceProcess;

namespace UnstoppableService;

public class UnstoppableService : BackgroundService
{
    private readonly ILogger<UnstoppableService> _logger;
    private readonly WfpController _wfpController;
    private readonly LockdownManager _lockdownManager;
    private Timer? _processBlockerTimer;
    private readonly ServiceMonitor _serviceMonitor;
    private Timer? _monitorTimer;

    public UnstoppableService(ILogger<UnstoppableService> logger)
    {
        _logger = logger;
        _wfpController = new WfpController();
        _lockdownManager = new LockdownManager();
        _serviceMonitor = new ServiceMonitor("UnstoppableServiceB", _logger);
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unstoppable Service is starting.");

        if (!_lockdownManager.IsLockdownActive)
        {
            _lockdownManager.StartLockdown(TimeSpan.FromHours(1));
            _logger.LogInformation("Lockdown initiated for 1 hour.");
        }
        else
        {
            _logger.LogInformation("Lockdown is currently active.");
            RegistryProtector.LockSafeBoot();
        }

        if (_lockdownManager.IsLockdownActive)
        {
            _processBlockerTimer = new Timer(
                _ => ProcessBlocker.TerminateBlockedProcesses(),
                null,
                TimeSpan.Zero,
                TimeSpan.FromSeconds(1));
        }

        _monitorTimer = new Timer(
            _ => _serviceMonitor.Monitor(),
            null,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(5));

        ProcessProtector.Protect();
        _wfpController.Start();
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Unstoppable Service is stopping.");
        _processBlockerTimer?.Dispose();
        _monitorTimer?.Dispose();
        _wfpController.Stop();
        return base.StopAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

    }
}
