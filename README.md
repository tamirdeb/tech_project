# Clipboard Auto-Clearer

This project contains a PowerShell script to automatically clear your clipboard every 30 seconds, enhancing your privacy and security. A helper batch file is included to run the script silently in the background.

## Files

- `Clear-Clipboard.ps1`: The main PowerShell script that clears the clipboard.
- `Start-Clipboard-Clearer.bat`: A batch file to start the PowerShell script in a hidden window.

## How to Use

### Manual Start

To start the clipboard clearer manually, simply double-click the `Start-Clipboard-Clearer.bat` file. This will launch the PowerShell script in the background. You won't see any window, but the script will be running.

### Automatic Start on Windows Login

To have the script run automatically every time you log in to Windows, you can place a shortcut to the `Start-Clipboard-Clearer.bat` file in the Windows Startup folder.

1.  Right-click on the `Start-Clipboard-Clearer.bat` file and select **Create shortcut**.
2.  Press the **Windows Key + R** to open the Run dialog.
3.  Type `shell:startup` and press **Enter**. This will open the Startup folder.
4.  Move the newly created shortcut file into the Startup folder.

Now, the script will automatically start in the background every time you log into your Windows account.

## How to Stop the Script

You can stop the script at any time by using the Task Manager:

1.  Press **Ctrl + Shift + Esc** to open the Task Manager.
2.  Go to the **Details** tab.
3.  Find and select the `powershell.exe` process. *Note: If you have other PowerShell scripts running, you might need to identify the correct one. You can check the command line column to see which one is running the `Clear-Clipboard.ps1` script.*
4.  Click the **End task** button.