# Strict Enforcement Tool

A strict self-regulation enforcement tool built as a .NET 8 Console Application.

## Features
- **Persistence**: Automatically adds itself to Windows Startup (`HKCU\...\Run`).
- **Last Chance Protocol**: Starts with a 30-second countdown. If the correct override code is not entered, it engages the monitoring loop.
- **Monitoring**: Captures screenshots every 3 seconds, processes them (Grayscale -> Threshold), and uses Tesseract OCR to scan for banned keywords.
- **Punishment**: If a banned keyword is found, the computer is immediately shut down.

## Prerequisites
1. **.NET 8 SDK**: Ensure you have the .NET 8 runtime or SDK installed.
2. **Windows OS**: This tool relies on Windows API (P/Invoke) and Registry access.
3. **Tessdata**: You must download the Tesseract language data.

## Setup Instructions

1. **Download Tesseract Data**:
   - Download the `eng.traineddata` file from the [tessdata repository](https://github.com/tesseract-ocr/tessdata).
   - Create a folder named `tessdata` in the same directory as the executable.
   - Place `eng.traineddata` inside the `tessdata` folder.

2. **Build**:
   ```bash
   dotnet build -c Release
   ```

3. **Run**:
   Navigate to the output directory (e.g., `bin/Release/net8.0/win-x64/`) and run the executable.

   *Note: On first run, it will add itself to the registry.*

## Configuration
- **Override Code**: The default override code is `password`. (Hash is hardcoded in `Program.cs`).
- **Banned Keywords**: Edit the `BannedKeywords` list in `Program.cs` to add/remove words.

## Disclaimer
This tool is designed for strict self-regulation. **Use with caution.** It will shut down your computer immediately if triggered, potentially causing data loss in open applications.
