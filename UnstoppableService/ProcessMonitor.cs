using System.Diagnostics;

namespace UnstoppableService;

public class ProcessMonitor
{
    private readonly string _processNameToMonitor;
    private readonly string _processPath;
    private readonly ILogger _logger;

    public ProcessMonitor(string processNameToMonitor, string processPath, ILogger logger)
    {
        _processNameToMonitor = processNameToMonitor;
        _processPath = processPath;
        _logger = logger;
    }

    public void Monitor()
    {
        var processes = Process.GetProcessesByName(_processNameToMonitor);
        if (processes.Length == 0)
        {
            _logger.LogWarning("{Process} is not running. Restarting...", _processNameToMonitor);
            try
            {
                Process.Start(_processPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restart {Process}", _processNameToMonitor);
            }
        }
    }
}
