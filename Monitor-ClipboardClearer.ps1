# Get the directory of the current script to ensure paths are correct
$scriptPath = $PSScriptRoot

# The full path to the script that should be running
$targetScriptPath = Join-Path -Path $scriptPath -ChildPath "Clear-Clipboard.ps1"

while ($true) {
    # Check if a powershell process is running our target script
    # We look for a command line that contains the script's path
    $process = Get-CimInstance Win32_Process | Where-Object {
        $_.Name -eq 'powershell.exe' -and $_.CommandLine -like "*$targetScriptPath*"
    }

    # If no such process is found, start the script
    if (-not $process) {
        $processArgs = @{
            FilePath     = "powershell.exe"
            ArgumentList = "-WindowStyle Hidden -ExecutionPolicy Bypass -File `"$targetScriptPath`""
            WindowStyle  = "Hidden"
        }
        Start-Process @processArgs
    }

    # Wait for 60 seconds before the next check
    Start-Sleep -Seconds 60
}