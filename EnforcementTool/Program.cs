using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Tesseract;

namespace EnforcementTool
{
    class Program
    {
        // --- P/Invoke Definitions ---
        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        static extern int GetSystemMetrics(int nIndex);

        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        const int SM_XVIRTUALSCREEN = 76;
        const int SM_YVIRTUALSCREEN = 77;
        const int SM_CXVIRTUALSCREEN = 78;
        const int SM_CYVIRTUALSCREEN = 79;

        // --- Configuration ---
        private static readonly string AppName = "EnforcementTool";
        // SHA256 Hash for "password"
        private static readonly string OverrideHash = "5e884898da28047151d0e56f8dc6292773603d0d6aabbdd62a11ef721d1542d8";
        // Banned keywords list
        private static readonly List<string> BannedKeywords = new()
        {
            "facebook", "twitter", "reddit", "instagram", "tiktok", "youtube", "game", "steam", "discord"
        };
        private static readonly int ScreenshotIntervalMs = 3000;

        static async Task Main(string[] args)
        {
            // 1. Persistence
            EnsurePersistence();

            // Ensure console is visible
            var handle = GetConsoleWindow();
            ShowWindow(handle, SW_SHOW);

            // 2. Last Chance Countdown
            bool aborted = await RunStartupSequence();

            if (aborted)
            {
                Console.WriteLine("Override accepted. Exiting...");
                await Task.Delay(2000);
                return;
            }

            // 3. Engage Monitoring
            Console.WriteLine("Protocol Engaged.");
            await Task.Delay(1000); // Brief pause
            ShowWindow(handle, SW_HIDE);

            await RunMonitoringLoop();
        }

        // --- Persistence Logic ---
        private static void EnsurePersistence()
        {
            try
            {
                string? exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath)) return;

                // Open Run key for the current user
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    if (key.GetValue(AppName) == null)
                    {
                        key.SetValue(AppName, exePath);
                        Console.WriteLine("Persistence enabled: Added to Registry.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Failed to ensure persistence. {ex.Message}");
            }
        }

        // --- Startup Logic ---
        private static async Task<bool> RunStartupSequence()
        {
            Console.Clear();
            Console.WriteLine("==========================================");
            Console.WriteLine("   STRICT ENFORCEMENT PROTOCOL INITIATING");
            Console.WriteLine("==========================================");
            Console.WriteLine($"You have 30 seconds to abort.");
            Console.WriteLine("Enter Override Code:");

            using var cts = new CancellationTokenSource();

            // Timer task
            var timerTask = Task.Delay(TimeSpan.FromSeconds(30), cts.Token);

            // Input task
            var inputTask = ReadLineAsync(cts.Token);

            // Wait for either
            var completedTask = await Task.WhenAny(timerTask, inputTask);

            if (completedTask == timerTask)
            {
                Console.WriteLine("\nTime expired.");
                return false; // Proceed to engage
            }
            else
            {
                // Input received
                string input = await inputTask;
                if (VerifyHash(input, OverrideHash))
                {
                    cts.Cancel();
                    return true; // Abort
                }
                else
                {
                    Console.WriteLine("\nIncorrect code.");
                    return false; // Proceed to engage
                }
            }
        }

        private static async Task<string> ReadLineAsync(CancellationToken token)
        {
            return await Task.Run(() =>
            {
                // Simple blocking read, wrapped in Task.
                // Checking KeyAvailable loop is better for cancellation but ReadLine is robust for input.
                // If cancelled, this thread may remain blocked until enter is pressed,
                // but since the app proceeds to hide window or exit, it's acceptable.
                try
                {
                    if (token.IsCancellationRequested) return string.Empty;
                    return Console.ReadLine() ?? string.Empty;
                }
                catch
                {
                    return string.Empty;
                }
            }, token);
        }

        private static bool VerifyHash(string input, string hash)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            var builder = new StringBuilder();
            foreach (var b in bytes) builder.Append(b.ToString("x2"));
            return builder.ToString().Equals(hash, StringComparison.OrdinalIgnoreCase);
        }

        // --- Monitoring Logic ---
        private static async Task RunMonitoringLoop()
        {
            string tessDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tessdata");

            // Ensure tessdata exists or log error
            if (!Directory.Exists(tessDataPath))
            {
                 // In a hidden console app, we can't easily show error.
                 // We'll just attempt to run and catch.
                 // Ideally, user ensures tessdata is present.
            }

            try
            {
                // Initialize Tesseract Engine
                using var engine = new TesseractEngine(tessDataPath, "eng", EngineMode.Default);

                while (true)
                {
                    try
                    {
                        // 1. Capture
                        using var bitmap = CaptureScreen();
                        if (bitmap == null) continue;

                        // 2. Process (Grayscale -> Threshold)
                        ProcessImage(bitmap);

                        // 3. OCR
                        using var page = engine.Process(bitmap);
                        string text = page.GetText();

                        // 4. Check
                        if (ContainsBannedKeyword(text))
                        {
                            Punish();
                        }
                    }
                    catch (Exception)
                    {
                        // Ignore transient errors (screen capture failure, etc.)
                    }

                    await Task.Delay(ScreenshotIntervalMs);
                }
            }
            catch (Exception ex)
            {
                // Fatal error (e.g. Tesseract not found)
                // Since window is hidden, write to a log file?
                File.WriteAllText("error_log.txt", ex.ToString());
            }
        }

        private static Bitmap? CaptureScreen()
        {
            int width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            int height = GetSystemMetrics(SM_CYVIRTUALSCREEN);
            int left = GetSystemMetrics(SM_XVIRTUALSCREEN);
            int top = GetSystemMetrics(SM_YVIRTUALSCREEN);

            if (width == 0 || height == 0)
            {
                // Fallback to primary screen
                width = GetSystemMetrics(0); // SM_CXSCREEN
                height = GetSystemMetrics(1); // SM_CYSCREEN
                left = 0;
                top = 0;
            }

            if (width <= 0 || height <= 0) return null;

            var bmp = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(left, top, 0, 0, bmp.Size);
            }
            return bmp;
        }

        private static void ProcessImage(Bitmap bmp)
        {
            // Direct unsafe memory manipulation for Grayscale + Threshold
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData data = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            unsafe
            {
                byte* ptr = (byte*)data.Scan0;
                int totalBytes = Math.Abs(data.Stride) * bmp.Height;

                for (int i = 0; i < totalBytes; i += 4)
                {
                    // BGRA format
                    byte b = ptr[i];
                    byte g = ptr[i + 1];
                    byte r = ptr[i + 2];

                    // Grayscale (Luma)
                    byte gray = (byte)(0.299 * r + 0.587 * g + 0.114 * b);

                    // Threshold (128)
                    byte thresholded = gray < 128 ? (byte)0 : (byte)255;

                    ptr[i] = thresholded;     // B
                    ptr[i + 1] = thresholded; // G
                    ptr[i + 2] = thresholded; // R
                }
            }

            bmp.UnlockBits(data);
        }

        private static bool ContainsBannedKeyword(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;

            // Normalize text
            string normalized = text.ToLowerInvariant();

            foreach (var kw in BannedKeywords)
            {
                if (normalized.Contains(kw)) return true;
            }
            return false;
        }

        private static void Punish()
        {
            try
            {
                Process.Start(new ProcessStartInfo("shutdown", "/s /f /t 0")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
            catch
            {
                // If punishment fails...
            }
            finally
            {
                Environment.Exit(0);
            }
        }
    }
}
