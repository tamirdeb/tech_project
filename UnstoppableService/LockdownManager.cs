namespace UnstoppableService;

public class LockdownManager
{
    private const string LockdownFile = "lockdown.dat";
    private DateTime _lockdownEndTime = DateTime.MinValue;

    public bool IsLockdownActive => DateTime.UtcNow < _lockdownEndTime;

    public LockdownManager()
    {
        LoadLockdownState();
    }

    public void StartLockdown(TimeSpan duration)
    {
        if (IsLockdownActive)
        {
            return;
        }

        _lockdownEndTime = DateTime.UtcNow.Add(duration);
        SaveLockdownState();
    }

    private void LoadLockdownState()
    {
        if (File.Exists(LockdownFile))
        {
            var content = File.ReadAllText(LockdownFile);
            if (long.TryParse(content, out var ticks))
            {
                _lockdownEndTime = new DateTime(ticks, DateTimeKind.Utc);
            }
        }
    }

    private void SaveLockdownState()
    {
        File.WriteAllText(LockdownFile, _lockdownEndTime.Ticks.ToString());
    }
}
