while ($true) {
    # This command is a more robust way to clear the clipboard,
    # including the clipboard history.
    cmd.exe /c "echo off | clip"
    Start-Sleep -Seconds 30
}