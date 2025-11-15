using System.ServiceProcess;
using System.Diagnostics;

const string ServiceName = "UnstoppableService";
const string WatchdogProcessName = "Watchdog";

// Ensure only one instance of the watchdog is running
var currentProcess = Process.GetCurrentProcess();
var watchdogs = Process.GetProcessesByName(WatchdogProcessName)
    .Where(p => p.Id != currentProcess.Id)
    .ToArray();

if (watchdogs.Any())
{
    Console.WriteLine("Another Watchdog process is already running. Exiting.");
    return;
}

Console.WriteLine("Watchdog started. Monitoring service...");

while (true)
{
    try
    {
        using var service = new ServiceController(ServiceName);
        if (service.Status != ServiceControllerStatus.Running)
        {
            Console.WriteLine($"{ServiceName} is not running. Attempting to start...");
            service.Start();
            service.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(10));
            Console.WriteLine($"{ServiceName} started successfully.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error monitoring service: {ex.Message}");
    }

    Thread.Sleep(TimeSpan.FromSeconds(5));
}
