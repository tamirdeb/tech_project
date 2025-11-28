using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows.Automation;

namespace EnforcementTool
{
    // קונטקסט להאזנה לכיבוי חוקי (Windows Restart/Update)
    public class HiddenContext : ApplicationContext
    {
        public HiddenContext()
        {
            // זיהוי כיבוי מסודר של מערכת ההפעלה
            SystemEvents.SessionEnding += (s, e) =>
            {
                Program.IsSystemRestarting = true;
            };
        }
    }

    // =============================================================
    // TESTABLE LOGIC CORE
    // =============================================================
    public static class EnforcementLogic
    {
        public static readonly List<string> BannedKeywords = new()
        {
            // ---Banned Keywords List---
            "x.com", "twitter", "reddit", "vk.com", "catch-chat",
            "hentai",

            // ---(Messaging) ---
            "telegram", "discord", "beeper", "franz", "ferdi", "ferdium",
            "rambox", "station", "wavebox", "biscuit", "mailbird", "stack next se",
            "all-in-one messenger", "mangoapps", "island.io", "webcatalog",

            // --- Unsupported Browsers & Networks ---
            "tor browser", ".onion", "mullvad", "epic privacy", "iron", "k-meleon",
            "supermium", "librewolf", "mypal", "r3dfox", "icedragon", "links", "otter",
            "qupzilla", "greenbrowser", "browzar", "beaker", "phyrox", "lt browser",
            "ghost browser", "sielo", "aol desktop", "slimbrowser", "slimboat", "iridium",
            "ungoogled", "waterfox", "pale moon", "midori", "avast secure", "basilisk",
            "coc coc", "dragon", "falkon", "floorp", "icecat", "konqueror", "lunascape",
            "maxthon", "netsurf", "puffin", "qutebrowser", "salamweb", "seamonkey",
            "sleipnir", "srware", "thorium", "torch", "ulaa", "ur browser", "zen browser",

            // --- 4. כלי פיתוח שמשמשים כדפדפן ---
            "polypane", "responsively", "floutwork", "blisk", "sizzy",
            "manageyum", "huler", "here.io",

            // ---(Android/iOS) ---
            "bluestacks", "gameloop", "ldplayer", "memu", "nox", "genymotion", "smartgaga",
            "primeos", "phoenixos", "droid4x", "andyroid", "anbox", "ankulua", "apowermirror",
            "browserstack", "corellium", "ipadian", "lambdatest", "leidian", "lonelyscreen",
            "remix os", "shashlik", "tiantian", "waydroid", "windroy", "xeplayer", "xiaoyao",
            "youwave", "leapdroid", "koplayer", "mumu",

            // -- VM's --
            "virtualbox", "vmware", "qemu", "parallels", "hyper-v", "bochs", "pcem",

            // --- 7. טורנטים ו-P2P ---
            "bittorrent", "emule", "frostwire", "tixati",
            "soulseek", "kazaa", "dc++", "fopnu", "retroshare", "tribler", "gnucleus",
            "imesh", "overnet", "shareaza", "edonkey", "gtk-gnutella", "imule", "mldonkey",
            "perfect dark",

            // --- 8. כלי פריצה והורדות ---
            "tails os", "whonix", "kodachi", "parrot", "qubes", "securonis", "septor",
            "softonic", "uptodown", "f-droid", "filehippo", "filehorse", "filepuma",
            "lo4d", "ninite", "snapfiles", "techspot", "downloadcrew", "portableapps", "portapps"
        };

        public static bool ContainsBannedKeyword(string t)
        {
            if (string.IsNullOrEmpty(t)) return false;
            t = t.ToLowerInvariant();
            foreach (var k in BannedKeywords)
                if (t.Contains(k)) return true;
            return false;
        }

        public static bool VerifyHash(string input, string hash)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            var builder = new StringBuilder();
            foreach (var b in bytes) builder.Append(b.ToString("x2"));
            return builder.ToString().Equals(hash, StringComparison.OrdinalIgnoreCase);
        }
    }

    class Program
    {
        // =============================================================
        // 1. IMPORTS & DEFINITIONS
        // =============================================================

        [DllImport("kernel32.dll")] static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")] static extern bool SetProcessDPIAware();
        [DllImport("user32.dll")] static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);
        [DllImport("shell32.dll")] static extern int SHQueryUserNotificationState(out int pquns);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)] private static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("user32.dll")] static extern int GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);
        [DllImport("user32.dll")] static extern bool TranslateMessage([In] ref MSG lpMsg);
        [DllImport("user32.dll")] static extern IntPtr DispatchMessage([In] ref MSG lpMsg);

        [DllImport("advapi32.dll", SetLastError = true)] static extern uint SetSecurityInfo(IntPtr handle, int ObjectType, int SecurityInfo, IntPtr psidOwner, IntPtr psidGroup, IntPtr pDacl, IntPtr pSacl);
        [DllImport("advapi32.dll", SetLastError = true)] static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(string StringSecurityDescriptor, uint StringSDRevision, out IntPtr SecurityDescriptor, out uint SecurityDescriptorSize);
        [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr LocalFree(IntPtr hMem);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetConsoleCtrlHandler(ConsoleCtrlDelegate HandlerRoutine, bool Add);
        [DllImport("user32.dll")] static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        public struct MSG { public IntPtr hwnd; public uint message; public IntPtr wParam; public IntPtr lParam; public uint time; public System.Drawing.Point pt; }
        public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate bool ConsoleCtrlDelegate(int CtrlType);

        // Constants
        const int WH_KEYBOARD_LL = 13;
        const int WM_KEYDOWN = 0x0100;
        const int SW_HIDE = 0;
        const int SW_SHOW = 5;
        const int QUNS_RUNNING_D3D_FULL_SCREEN = 3;
        const int QUNS_BUSY = 2;
        const int QUNS_NOT_PRESENT = 1;
        const int QUNS_PRESENTATION_MODE = 4;
        const int QUNS_ACCEPTS_NOTIFICATIONS = 5;

        // =============================================================
        // 2. CONFIG & STATE
        // =============================================================

        private static readonly bool SafeMode = false;
        private static readonly string AppName = "EnforcementTool";
        private static readonly string WatchdogExeName = "SystemIntegrityGuard.exe";
        public static readonly string OverrideHash ="a665a45920422f9d417e4867efdc4fb8a04a1f3fff1fa07e998e86f7f7a27ae3"; // 123

        private static readonly List<string> RestrictedWindowTitles = new() { "Task Scheduler", "Registry Editor", "Services", "Telegram", "x.com", "System Configuration", "Process Hacker", "Process Explorer", "System Explorer", "Resource Monitor" };
        private static readonly string LogFileName = "system.log";

        private static int _violationCount = 0;
        private const int Threshold_KillApp = 5;
        private const int Threshold_Shutdown = 10;
        private static DateTime _lastViolationTime = DateTime.Now;
        private const int CoolingOffMinutes = 10;

        public static bool IsSystemRestarting = false;
        private static bool _isGaming = false;
        private static bool _isShuttingDown = false;
        private static FileStream? _globalFileLock = null;
        private static bool _isPunishing = false;
        private static Mutex? _appMutex;

        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private static StringBuilder _inputBuffer = new StringBuilder();
        private static ConsoleCtrlDelegate? _consoleHandler;

        // =============================================================
        // 3. MAIN
        // =============================================================

        [STAThread]
        static void Main(string[] args)
        {
            // 1. זיהוי תפקיד חכם (מונע בלבול זהויות)
            string currentProcName = Process.GetCurrentProcess().ProcessName;
            // אם שם הקובץ מכיל את שם השומר, הוא בהכרח שומר, גם אם לא קיבל ארגומנטים!
            bool isWatchdog = (args.Length > 0 && args[0] == "--watchdog") ||
                              currentProcName.IndexOf("SystemIntegrityGuard", StringComparison.OrdinalIgnoreCase) >= 0;

            // 2. מניעת כפילויות (Singleton)
            bool createdNew;
            string mutexName = isWatchdog ? "Global\\Watchdog_Mutex_V23" : "Global\\Master_Mutex_V23";

            _appMutex = new Mutex(true, mutexName, out createdNew);
            if (!createdNew) return;

            // 3. הגדרת טיפול בכיבוי (מונע BSOD)
            _consoleHandler = new ConsoleCtrlDelegate(ConsoleCtrlCheck);
            SetConsoleCtrlHandler(_consoleHandler, true);

            // 4. הפעלת הגנות בסיס
            LoadState();
            LockExecutable();
            ProcessProtector.ProtectCurrentProcess();

            // 5. פיצול לוגיקה
            if (isWatchdog)
            {
                // השומר מקבל את הנתיב לראשי
                string masterPath = args.Length > 1 ? args[1] : Path.Combine(Path.GetDirectoryName(Environment.ProcessPath)!, "EnforcementTool.exe");
                RunWatchdogLogic(masterPath);
            }
            else
            {
                // הראשי מתחיל בנוהל אתחול קפדני
                RunMasterLogic();
            }

            if (_hookID != IntPtr.Zero) UnhookWindowsHookEx(_hookID);
        }

        // =============================================================
        // 4. LOGIC
        // =============================================================

        private static void BootstrapWatchdog()
        {
            // שם המיוטקס של השומר (אותו שם שמוגדר ב-Main)
            string watchdogMutexName = "Global\\Watchdog_Mutex_V23"; // ודא שזה תואם למה שב-Main!

            // לולאה שלא נגמרת עד שהשומר חי
            while (!IsSystemRestarting && !_isShuttingDown)
            {
                // בדיקה 1: האם המיוטקס של השומר תפוס? (זה אומר שיש תהליך חי שמחזיק אותו)
                bool watchdogRunning = false;
                try
                {
                    // מנסים לפתוח את המיוטקס. אם הוא קיים, זה אומר שהשומר חי.
                    using (Mutex m = Mutex.OpenExisting(watchdogMutexName))
                    {
                        watchdogRunning = true;
                    }
                }
                catch (WaitHandleCannotBeOpenedException)
                {
                    // המיוטקס לא קיים -> השומר לא רץ
                    watchdogRunning = false;
                }
                catch (UnauthorizedAccessException)
                {
                    // אם אין גישה, זה סימן טוב (הוא קיים ונעול)
                    watchdogRunning = true;
                }

                if (watchdogRunning)
                {
                    LogSystemEvent("STARTUP", "Watchdog Mutex detected. System stable.");
                    break; // הצלחה!
                }

                // אם הגענו לפה - השומר לא רץ. מנסים להפעיל אותו.
                EnsureSeparateWatchdogRunning();

                // מחכים שנייה ונותנים לו לעלות
                Thread.Sleep(1000);
            }
        }

        private static bool ConsoleCtrlCheck(int ctrlType)
        {
            if (ctrlType == 5 || ctrlType == 6)
            {
                IsSystemRestarting = true;
                return true;
            }
            return false;
        }

        private static void CheckForKillSwitch()
        {
            if (_isShuttingDown) return;
            string stopFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "STOP_ME.txt");
            string stopFileDone = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "STOP_ME.done");

            if (File.Exists(stopFile) || File.Exists(stopFileDone))
            {
                try
                {
                    if (File.Exists(stopFile))
                    {
                        string content = File.ReadAllText(stopFile).Trim();
                        if (!EnforcementLogic.VerifyHash(content, OverrideHash)) return;
                        try { File.Move(stopFile, stopFileDone); } catch { }
                    }

                    _isShuttingDown = true;
                    LogSystemEvent("SHUTDOWN", "Kill switch confirmed.");

                    UnlockExecutable();
                    RemoveRedundancy();
                    KillAllInstances(); // ניקוי רג'יסטרי

                    // המתנה לתהליך השני
                    Thread.Sleep(4000);

                    try { if (File.Exists(stopFileDone)) File.Delete(stopFileDone); } catch { }

                    Environment.Exit(0);
                } catch { }
            }
        }

        private static void RunWatchdogLogic(string masterPath)
        {
            // הגדרות נתיבים
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string masterExeName = "EnforcementTool.exe";
            string fullMasterPath = Path.Combine(baseDir, masterExeName);

            // לוג לבקרה
            string logFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Startup_Debug.txt");
            void Log(string m) { try { File.AppendAllText(logFile, $"[{DateTime.Now:HH:mm:ss}] {m}\n"); } catch { } }

            try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High; } catch { }

            Log("--- WATCHDOG STARTED (Boot Sequence) ---");

            // =================================================================
            // שלב 1: שלב השיגור (Launcher Mode)
            // בשלב זה אין עונשים! המטרה היא רק לגרום לראשי לעלות.
            // =================================================================

            bool masterIsRunning = false;
            int launchAttempts = 0;

            // נותנים למערכת 30 שניות להתייצב ולהעלות את הראשי
            while (!masterIsRunning && launchAttempts < 10)
            {
                if (IsSystemRestarting || _isShuttingDown) return; // אם המחשב נכבה, צא בשקט

                Process[] masters = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(masterExeName));

                if (masters.Length > 0)
                {
                    masterIsRunning = true;
                    Log("Master detected running! Moving to Guard Mode.");
                }
                else
                {
                    launchAttempts++;
                    Log($"Master not found. Launch attempt {launchAttempts}/10...");

                    if (File.Exists(fullMasterPath))
                    {
                        try
                        {
                            var psi = new ProcessStartInfo(fullMasterPath);
                            psi.WorkingDirectory = baseDir; // חובה כדי למנוע קריסות
                            psi.UseShellExecute = true;
                            Process.Start(psi);
                        }
                        catch (Exception ex) { Log($"Launch error: {ex.Message}"); }
                    }

                    // המתנה של 3 שניות בין ניסיון לניסיון
                    Thread.Sleep(3000);
                }
            }

            // אם אחרי 30 שניות הראשי לא עלה - משהו דפוק במחשב, אבל נעבור למצב שמירה בכל זאת
            // כדי שאם הוא יעלה פתאום, נגן עליו.

            // =================================================================
            // שלב 2: שלב השומר (Guard Mode)
            // מעכשיו - כל נפילה גוררת ריסטרט.
            // =================================================================

            Log("--- GUARD MODE ACTIVATED ---");

            // השהייה אחרונה לפני דריכת הנשק
            Thread.Sleep(2000);

            while (true)
            {
                if (IsSystemRestarting || _isShuttingDown)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                CheckForKillSwitch();

                try
                {
                    // בדיקה האם הראשי חי
                    if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(masterExeName)).Length == 0)
                    {
                        // הראשי נעלם!

                        // בדיקה כפולה (למניעת טעות דגימה)
                        Thread.Sleep(1000);
                        if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(masterExeName)).Length == 0)
                        {
                            // וידוא אחרון שזה לא כיבוי מסודר של ווינדוס
                            if (!IsSystemRestarting && !_isShuttingDown)
                            {
                                Log("VIOLATION: Master process died. RESTARTING SYSTEM.");

                                // ביפ אזהרה
                                Task.Run(() => Console.Beep(2000, 500));

                                // מנגנון כיבוי משופר (עקשן)
                                bool shutdownCommandSent = false;
                                while (!shutdownCommandSent)
                                {
                                    try
                                    {
                                        // ניסיון ראשון: פקודת shutdown ישירה
                                        Process.Start(new ProcessStartInfo("shutdown", "/r /f /t 0") { CreateNoWindow = true, UseShellExecute = false });
                                        shutdownCommandSent = true;
                                    }
                                    catch (Exception ex)
                                    {
                                        try { File.AppendAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Shutdown_Debug.txt"), $"[{DateTime.Now}] Shutdown Error: {ex.Message}\n"); } catch { }
                                    }

                                    if (!shutdownCommandSent)
                                    {
                                        try
                                        {
                                            // ניסיון שני: דרך CMD
                                            Process.Start(new ProcessStartInfo("cmd", "/c shutdown /r /f /t 0") { CreateNoWindow = true, UseShellExecute = false });
                                            shutdownCommandSent = true;
                                        }
                                        catch { }
                                    }

                                    // אם עדיין לא הצלחנו, מנסים שוב עוד רגע
                                    if (!shutdownCommandSent) Thread.Sleep(1000);
                                }

                                // לא יוצאים מיד כדי לתת לפקודה זמן לרוץ, אבל בסוף יוצאים כדי שהמערכת תיסגר
                                Thread.Sleep(5000);
                                Environment.Exit(0);
                            }
                        }
                    }

                    // חידוש הגנות רג'יסטרי (כדי שהם יעלו בריסטרט הבא)
                    if (DateTime.Now.Second % 15 == 0) EnsureSystemRedundancy();
                }
                catch { }

                Thread.Sleep(1000);
            }
        }

        private static async Task RunMonitoringLoop()
        {
            int gcCounter = 0;

            // ניתן לשומר זמן לעלות לפני שמתחילים לאכוף עליו את המוות
            bool watchdogProtectionActive = false;
            DateTime startTime = DateTime.Now;

            while (true)
            {
                if (IsSystemRestarting) return;
                CheckForKillSwitch();

                if (!_isShuttingDown)
                {
                    // 1. בדיקה האם השומר (Watchdog) חי
                    Process[] watchdogs = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(WatchdogExeName));

                    // מפעילים הגנה רק אחרי 30 שניות של ריצה, כדי לתת לשומר זמן לעלות בהתחלה
                    if (!watchdogProtectionActive && (DateTime.Now - startTime).TotalSeconds > 30)
                    {
                        watchdogProtectionActive = true;
                    }

                    if (watchdogs.Length == 0)
                    {
                        // אם אנחנו בשלב ההגנה הפעילה והשומר איננו - סימן שמישהו הרג אותו!
                        if (watchdogProtectionActive)
                        {
                            LogSystemEvent("VIOLATION", "Watchdog was killed! RESTARTING.");
                            try
                            {
                                // הראשי נוקם את מותו של השומר
                                Process.Start(new ProcessStartInfo("shutdown", "/r /f /t 0") { CreateNoWindow = true, UseShellExecute = false });
                                Environment.Exit(0);
                            }
                            catch { }
                        }

                        // אם אנחנו עדיין בהתחלה, או שהריסטרט נכשל, ננסה להחיות אותו
                        EnsureSeparateWatchdogRunning();
                    }

                    CheckForRestrictedApps();
                    if (DateTime.Now.Second % 30 == 0) EnsureSystemRedundancy();
                }

                // ... המשך הקוד הרגיל שלך (בדיקת חלונות, אתרים וכו') ...
                if (_violationCount > 0 && (DateTime.Now - _lastViolationTime).TotalMinutes >= CoolingOffMinutes)
                {
                    _violationCount--;
                    _lastViolationTime = DateTime.Now;
                    SaveState();
                    LogSystemEvent("COOLING", $"Strike removed. Count: {_violationCount}");
                }

                if (IsGameMode()) { _isGaming = true; await Task.Delay(5000); continue; }
                _isGaming = false;

                try
                {
                    IntPtr hwnd = GetForegroundWindow();
                    if (hwnd != IntPtr.Zero)
                    {
                        string title = GetActiveWindowTitle();
                        if (EnforcementLogic.ContainsBannedKeyword(title)) Punish(title, "Window Title");
                        else
                        {
                            string url = GetUrlFromWindow(hwnd);
                            if (!string.IsNullOrEmpty(url) && EnforcementLogic.ContainsBannedKeyword(url)) Punish(url, "URL Scanner");
                        }
                    }
                }
                catch { }

                if (++gcCounter >= 50) { GC.Collect(); gcCounter = 0; }
                await Task.Delay(1000);
            }
        }

        private static void RunMasterLogic()
        {
            SetProcessDPIAware();

            // --- תיקון: הנעה מיידית של השומר ---
            // אל תחכה למיוטקס. פשוט תבדוק אם הוא רץ, ואם לא - תפעיל אותו מיד.
            EnsureSeparateWatchdogRunning();

            // וידוא רישום ברג'יסטרי מיד על ההתחלה
            EnsureSystemRedundancy();

            // צפצוף קצר כדי שתדע שהתוכנה עלתה
            Task.Run(() => { Console.Beep(500, 200); });

            LogSystemEvent("ENGAGED", $"Master Process Started. Violations: {_violationCount}");

            // הפעלת ה-Keylogger
            Thread keyloggerThread = new Thread(StartKeylogger);
            keyloggerThread.IsBackground = true;
            keyloggerThread.Start();

            // הפעלת לולאת הניטור הראשית
            Task.Run(() => RunMonitoringLoop());

            // השארת התהליך חי
            Application.Run(new HiddenContext());
        }

        // =============================================================
        // 5. HELPERS
        // =============================================================

        private static void EnsureSeparateWatchdogRunning()
        {
            try
            {
                // 1. חישוב נתיבים מדויקים
                string currentExe = Process.GetCurrentProcess().MainModule?.FileName ?? Environment.ProcessPath!;
                string currentDir = Path.GetDirectoryName(currentExe)!;
                string watchdogPath = Path.Combine(currentDir, WatchdogExeName);

                // 2. בדיקה: האם השומר כבר רץ?
                if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(WatchdogExeName)).Length > 0)
                {
                    return; // הוא כבר עובד, אין מה לעשות
                }

                // 3. אם הקובץ פיזית לא קיים - צור אותו
                if (!File.Exists(watchdogPath))
                {
                    try { File.Copy(currentExe, watchdogPath, true); } catch { }
                }

                // 4. הפעלה אגרסיבית
                if (File.Exists(watchdogPath))
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo(watchdogPath);
                    startInfo.Arguments = $"--watchdog \"{currentExe}\""; // מעביר לו את הנתיב לראשי
                    startInfo.WorkingDirectory = currentDir; // !!! קריטי לעלייה עם המחשב !!!
                    startInfo.UseShellExecute = true;
                    startInfo.CreateNoWindow = true;
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;

                    Process.Start(startInfo);
                }
            }
            catch { }
        }

        private static string GetUrlFromWindow(IntPtr hwnd) { try { StringBuilder sb = new StringBuilder(256); GetWindowText(hwnd, sb, 256); string t = sb.ToString().ToLower(); if (!t.Contains("chrome") && !t.Contains("edge") && !t.Contains("brave")) return ""; AutomationElement e = AutomationElement.FromHandle(hwnd); if (e == null) return ""; Condition c = new OrCondition(new PropertyCondition(AutomationElement.NameProperty, "Address and search bar"), new PropertyCondition(AutomationElement.NameProperty, "Search or enter address"), new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit)); AutomationElement edit = e.FindFirst(TreeScope.Descendants, c); if (edit != null) { if (edit.TryGetCurrentPattern(ValuePattern.Pattern, out object p)) return ((ValuePattern)p).Current.Value; else return edit.Current.Name; } } catch {} return ""; }
        private static void Punish(string text, string source) { if (_isPunishing || _isGaming || _isShuttingDown || IsSystemRestarting) return; _isPunishing = true; _violationCount++; _lastViolationTime = DateTime.Now; SaveState(); string log = text.Length > 50 ? text.Substring(0,50) : text; LogSystemEvent("VIOLATION", $"Strike {_violationCount}/{Threshold_Shutdown} | {source}: '{log}'"); Task.Run(() => { try { if (_violationCount >= Threshold_Shutdown && !SafeMode) { Process.Start(new ProcessStartInfo("shutdown", "/s /f /t 0") { CreateNoWindow = true, UseShellExecute = false }); Environment.Exit(0); } else if (_violationCount >= Threshold_KillApp && !SafeMode) { string k = KillActiveApplication(); Console.Beep(1000, 500); MessageBox.Show($"VIOLATION!\nApp Killed: {k}\nStrike: {_violationCount}/{Threshold_Shutdown}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification); } else { Console.Beep(800, 300); MessageBox.Show($"VIOLATION! Detected: '{log}'\nStrike: {_violationCount}/{Threshold_Shutdown}", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification); } } finally { Thread.Sleep(3000); _isPunishing = false; } }); }
        private static void SaveState() { try { using RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\EnforcementTool"); key.SetValue("ViolationCount", _violationCount); key.SetValue("LastViolationTime", _lastViolationTime.ToBinary()); } catch { } }
        private static void LoadState() { try { using RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\EnforcementTool"); if (key != null) { _violationCount = (int)key.GetValue("ViolationCount", 0); long timeBin = (long)key.GetValue("LastViolationTime", DateTime.Now.ToBinary()); _lastViolationTime = DateTime.FromBinary(timeBin); } } catch { } }
        private static string KillActiveApplication() { try { IntPtr hwnd = GetForegroundWindow(); if (hwnd == IntPtr.Zero) return "Unknown"; GetWindowThreadProcessId(hwnd, out uint pid); if (pid == Environment.ProcessId) return "Self"; Process p = Process.GetProcessById((int)pid); string name = p.ProcessName; p.Kill(); return name; } catch { return "Unknown"; } }
        private static string GetActiveWindowTitle() { try { IntPtr hwnd = GetForegroundWindow(); if (hwnd == IntPtr.Zero) return ""; StringBuilder sb = new StringBuilder(256); if (GetWindowText(hwnd, sb, 256) > 0) return sb.ToString(); } catch {} return ""; }
        public static class ProcessProtector { public static void ProtectCurrentProcess() { IntPtr p = IntPtr.Zero; try { string s = "D:P(D;;WP;;;WD)(A;;0x1FFFFF;;;WD)"; if (ConvertStringSecurityDescriptorToSecurityDescriptor(s, 1, out p, out uint _)) SetSecurityInfo(Process.GetCurrentProcess().Handle, 6, 4, IntPtr.Zero, IntPtr.Zero, p, IntPtr.Zero); } catch {} finally { if (p != IntPtr.Zero) LocalFree(p); } } }
        private static void LockExecutable() { try { string myPath = Environment.ProcessPath!; _globalFileLock = new FileStream(myPath, FileMode.Open, FileAccess.Read, FileShare.Read); } catch { } }
        private static void UnlockExecutable() { try { _globalFileLock?.Close(); _globalFileLock?.Dispose(); _globalFileLock = null; } catch { } }
        private static void StartKeylogger() { try { _hookID = SetHook(_proc); Application.Run(); UnhookWindowsHookEx(_hookID); } catch { } }
        private static IntPtr SetHook(LowLevelKeyboardProc proc) { using (Process curProcess = Process.GetCurrentProcess()) using (ProcessModule curModule = curProcess.MainModule!) return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0); }
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam) { if (_isGaming) return CallNextHookEx(_hookID, nCode, wParam, lParam); if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN) { Keys key = (Keys)Marshal.ReadInt32(lParam); string charToAdd = ""; if (key >= Keys.A && key <= Keys.Z) charToAdd = key.ToString(); else if (key >= Keys.D0 && key <= Keys.D9) charToAdd = key.ToString().Replace("D", ""); else if (key == Keys.Space) charToAdd = " "; else if (key == Keys.OemPeriod || key == Keys.Decimal) charToAdd = "."; if (!string.IsNullOrEmpty(charToAdd)) { _inputBuffer.Append(charToAdd.ToLower()); if (_inputBuffer.Length > 50) _inputBuffer.Remove(0, 1); foreach (var kw in EnforcementLogic.BannedKeywords) if (_inputBuffer.ToString().EndsWith(kw)) Punish(kw, "Keylogger"); } } return CallNextHookEx(_hookID, nCode, wParam, lParam); }
        private static bool IsGameMode() { if (SHQueryUserNotificationState(out int state) == 0) if (state == QUNS_RUNNING_D3D_FULL_SCREEN || state == QUNS_BUSY) return true; return false; }
        private static void CheckForRestrictedApps() { if (_isGaming || _isShuttingDown) return; IntPtr hwnd = GetForegroundWindow(); if (hwnd == IntPtr.Zero) return; StringBuilder sb = new StringBuilder(256); if (GetWindowText(hwnd, sb, 256) > 0) { string title = sb.ToString(); foreach (var r in RestrictedWindowTitles) if (title.Contains(r, StringComparison.OrdinalIgnoreCase)) { GetWindowThreadProcessId(hwnd, out uint pid); try { Process.GetProcessById((int)pid).Kill(); } catch { } } } }
        // VerifyHash moved to EnforcementLogic class
        private static void KillAllInstances() { try { using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\EnforcementTool", true)) { if (key != null) { try { key.DeleteValue("ViolationCount"); } catch {} } } } catch {} }
        private static void EnsureSystemRedundancy()
        {
            string currentExe = Process.GetCurrentProcess().MainModule?.FileName ?? Environment.ProcessPath!;
            string currentDir = Path.GetDirectoryName(currentExe)!;

            // 1. הגדרת נתיבים
            string watchdogExeName = "SystemIntegrityGuard.exe";
            string watchdogPath = Path.Combine(currentDir, watchdogExeName);

            // 2. וידוא שהקובץ הפיזי של השומר קיים
            if (!File.Exists(watchdogPath))
            {
                try { File.Copy(currentExe, watchdogPath, true); } catch { }
            }

            // 3. ניקוי רג'יסטרי ישן (מונע כפילויות ושומרים מזויפים)
            // אנו מסתמכים רק על Task Scheduler כי הוא אמין יותר לגישת אדמין בהפעלה
            try
            {
                using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true))
                {
                    if (key != null)
                    {
                        try { key.DeleteValue(AppName, false); } catch { }
                        try { key.DeleteValue(AppName + "_Monitor", false); } catch { }
                    }
                }
            }
            catch { }

            // 4. הגדרת Task Scheduler (הדרך הנכונה לעלות ב-Admin בלי הודעות)
            try
            {
                // משימה שרץ כל דקה (Keep-Alive)
                Process.Start(new ProcessStartInfo("schtasks", $"/create /sc minute /mo 1 /tn \"EnforcementProtocol_Minute\" /tr \"\\\"{currentExe}\\\"\" /rl highest /f") { CreateNoWindow = true, UseShellExecute = false });
            }
            catch { }
            try
            {
                // משימה שרצה בהתחברות (Logon)
                Process.Start(new ProcessStartInfo("schtasks", $"/create /sc onlogon /tn \"EnforcementProtocol_Logon\" /tr \"\\\"{currentExe}\\\"\" /rl highest /f") { CreateNoWindow = true, UseShellExecute = false });
            }
            catch { }
        }
        private static void RemoveRedundancy() {
            try { Process.Start(new ProcessStartInfo("schtasks", "/delete /tn \"EnforcementProtocol_Logon\" /f") { CreateNoWindow = true, UseShellExecute = false }); } catch { }
            try { Process.Start(new ProcessStartInfo("schtasks", "/delete /tn \"EnforcementProtocol_Minute\" /f") { CreateNoWindow = true, UseShellExecute = false }); } catch { }
            try { using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true)) { key?.DeleteValue(AppName, false); key?.DeleteValue(AppName + "_Monitor", false); } } catch { }
        }
        private static void LogSystemEvent(string type, string msg) { try { lock (LogFileName) { File.AppendAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogFileName), $"[{DateTime.Now}] [{type}] {msg}{Environment.NewLine}"); } } catch { } }
    }
}