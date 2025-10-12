# Resilient Clipboard Auto-Clearer

This project contains a PowerShell script to automatically clear your clipboard every 30 seconds, enhancing your privacy and security. It now includes a monitor script to ensure the clipboard-clearing service is always running.

## How It Works

The solution consists of two main scripts:

1.  `Clear-Clipboard.ps1`: This is the core script that runs in a continuous loop, clearing the system clipboard every 30 seconds.
2.  `Monitor-ClipboardClearer.ps1`: This script acts as a watchdog. It runs in the background and checks every 60 seconds to see if the `Clear-Clipboard.ps1` script is active. If it has stopped for any reason, the monitor will automatically restart it.

A helper batch file, `Start-Monitor.bat`, is provided to launch the whole system silently.

## Files

-   `Clear-Clipboard.ps1`: The main PowerShell script that clears the clipboard.
-   `Monitor-ClipboardClearer.ps1`: The watchdog script that ensures the clipboard clearer is always running.
-   `Start-Monitor.bat`: The single batch file used to start the monitoring service in a hidden window.

## How to Use

### Manual Start

To start the clipboard clearer system, simply double-click the `Start-Monitor.bat` file. This will launch the monitor script in the background, which in turn will start the clipboard clearer. You won't see any windows, but the scripts will be running.

### Automatic Start on Windows Login

To have the script run automatically every time you log in to Windows, you should place a shortcut to the `Start-Monitor.bat` file in the Windows Startup folder.

1.  Right-click on the `Start-Monitor.bat` file and select **Create shortcut**.
2.  Press the **Windows Key + R** to open the Run dialog.
3.  Type `shell:startup` and press **Enter**. This will open the Startup folder.
4.  Move the newly created shortcut file into the Startup folder.

Now, the monitor and the clipboard clearer will automatically start in the background every time you log into your Windows account.

## How to Stop the Scripts

To completely stop the system, you need to stop both the monitor and the clipboard clearer processes.

1.  Press **Ctrl + Shift + Esc** to open the Task Manager.
2.  Go to the **Details** tab.
3.  You will need to end two `powershell.exe` processes. To identify them, you may need to add the "Command line" column (right-click the header -> Select columns).
    -   Find the `powershell.exe` process with a command line containing `Monitor-ClipboardClearer.ps1` and end it.
    -   Find the `powershell.exe` process with a command line containing `Clear-Clipboard.ps1` and end it.

Ending the monitor first will prevent it from restarting the clearer script.