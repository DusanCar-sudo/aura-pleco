using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading;

namespace Pleco
{
    public class Program
    {
        // ──────────────── Win32 / NT Structures & APIs ────────────────

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;

            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx([In, Out] MEMORYSTATUSEX lpBuffer);

        [DllImport("psapi.dll", SetLastError = true)]
        public static extern int EmptyWorkingSet(IntPtr hwProc);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern uint NtSetSystemInformation(int InfoClass, IntPtr Info, int Length);

        [DllImport("ntdll.dll", SetLastError = true)]
        public static extern int NtSetTimerResolution(uint DesiredResolution, bool SetResolution, out uint CurrentResolution);

        // Token privilege adjustment
        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern bool OpenProcessToken(IntPtr ProcessHandle, int DesiredAccess, ref IntPtr TokenHandle);

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        internal static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, ref long lpLuid);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct TOKEN_PRIVILEGES
        {
            public int PrivilegeCount;
            public long Luid;
            public int Attributes;
        }

        [DllImport("advapi32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

        private const int TOKEN_ADJUST_PRIVILEGES = 0x00000020;
        private const int TOKEN_QUERY = 0x00000008;
        private const int SE_PRIVILEGE_ENABLED = 0x00000002;
        private const int SystemMemoryListInformation = 80;

        private enum MemoryListCommand
        {
            MemoryEmptyWorkingSets = 1,
            MemoryFlushModifiedList = 2,
            MemoryPurgeStandbyList = 3,
            MemoryPurgeLowPriorityStandbyList = 4
        }

        // ──────────────── Configuration Defaults ────────────────
        public class Config
        {
            public int CheckIntervalSeconds = 20;
            public int MemoryThresholdPercent = 80;
            public int MinAvailableRamMb = 2500;
            public int CooldownSeconds = 75;
            public bool TrimWorkingSets = true;
            public bool PurgeStandbyList = true;
            public bool OptimizeTimerResolution = true;
            public bool CleanTempFiles = true;
            public int TempFileMaxAgeHours = 2;
            public bool CleanOrphanProcesses = true;
            public bool CleanModelCache = true;
            public int ModelCacheMaxAgeDays = 30;
            public bool CleanTorchInductorCache = true;
            public bool LogToFile = true;
            public string LogFilePath = "pleco.log";
        }

        private static Config _config = new Config();
        private static bool _streamMode = false;
        private static bool _isAdmin = false;
        private static DateTime _lastHealTime = DateTime.MinValue;
        private static ulong _totalBytesLiberated = 0;
        private static long _totalTempBytesCleaned = 0;
        private static long _totalModelBytesCleaned = 0;
        private static int _cleanCount = 0;
        private static string _tankStatus = "CRYSTAL CLEAR";
        private static List<string> _recentActivity = new List<string>();

        // ──────────────── Entry Point ────────────────
        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            _isAdmin = CheckAdminStatus();

            bool once = false;
            bool status = false;
            bool cleanModels = false;

            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i].ToLowerInvariant();
                if (a == "--stream" || a == "-s") _streamMode = true;
                else if (a == "--once" || a == "-1") once = true;
                else if (a == "--status") status = true;
                else if (a == "--clean-model-cache") cleanModels = true;
                else if (a == "--help" || a == "-h")
                {
                    PrintHelp();
                    return;
                }
            }

            LoadConfig();

            if (_config.OptimizeTimerResolution)
            {
                EnableHighPrecisionTimer();
            }

            if (_isAdmin)
            {
                EnablePrivilege("SeIncreaseQuotaPrivilege");
                EnablePrivilege("SeProfileSingleProcessPrivilege");
            }

            if (status)
            {
                PrintStatus();
                return;
            }

            if (cleanModels)
            {
                Log("Scrubbing local AI model caches...");
                CleanModelCache();
                PrintStatus();
                return;
            }

            if (once)
            {
                Log("Running single tank-cleaning pass...");
                PerformClean(true);
                if (_config.CleanTempFiles) CleanTempDirectory();
                if (_config.CleanModelCache) CleanModelCache();
                PrintStatus();
                return;
            }

            // Start Pleco Loop
            RunLoop();
        }

        private static void RunLoop()
        {
            Log(string.Format("🐟 Pleco swimming into the tank! (Interval: {0}s, Min RAM: {1}MB, Admin: {2})",
                _config.CheckIntervalSeconds, _config.MinAvailableRamMb, _isAdmin ? "Yes (Full Standby Purge)" : "No (Standard Trimming)"));

            int tick = 0;
            int fishFrame = 0;

            while (true)
            {
                tick++;
                MEMORYSTATUSEX mem = GetMemoryStatus();
                ulong availMb = mem.ullAvailPhys / (1024 * 1024);
                uint memLoad = mem.dwMemoryLoad;

                bool needHeal = (memLoad >= (uint)_config.MemoryThresholdPercent) || (availMb < (ulong)_config.MinAvailableRamMb);

                if (needHeal)
                {
                    TimeSpan elapsed = DateTime.Now - _lastHealTime;
                    if (elapsed.TotalSeconds >= _config.CooldownSeconds)
                    {
                        _tankStatus = "NIBBLING ALGAE (CLEANING)";
                        PerformClean(false);
                        _lastHealTime = DateTime.Now;
                        _tankStatus = "CRYSTAL CLEAR";
                    }
                    else
                    {
                        _tankStatus = string.Format("WATER MURKY (Cooldown: {0}s)", (int)(_config.CooldownSeconds - elapsed.TotalSeconds));
                    }
                }
                else
                {
                    _tankStatus = "CRYSTAL CLEAR";
                }

                // Periodic temp sweep (every ~5 mins = 15 ticks at 20s)
                if (tick % 15 == 0 && _config.CleanTempFiles)
                {
                    CleanTempDirectory();
                }

                // Periodic model cache sweep (every ~30 mins = 90 ticks at 20s)
                if (tick % 90 == 0 && _config.CleanModelCache)
                {
                    CleanModelCache();
                }

                // Periodic orphan sweep
                if (tick % 30 == 0 && _config.CleanOrphanProcesses)
                {
                    CleanOrphanProcesses();
                }

                if (_streamMode)
                {
                    RenderAquarium(mem, fishFrame++);
                }

                Thread.Sleep(_config.CheckIntervalSeconds * 1000);
            }
        }

        // ──────────────── Memory Cleaning Logic ────────────────

        private static void PerformClean(bool force)
        {
            MEMORYSTATUSEX before = GetMemoryStatus();
            ulong beforeAvail = before.ullAvailPhys;

            string actionMsg = "Nibbling tank: ";

            // 1. Trim process working sets (including Python/Ollama AI runners)
            int trimmedCount = 0;
            if (_config.TrimWorkingSets)
            {
                trimmedCount = TrimProcessWorkingSets();
                actionMsg += string.Format("{0} apps trimmed, ", trimmedCount);
            }

            // 2. Purge Standby list (if Admin)
            bool purgedStandby = false;
            if (_config.PurgeStandbyList && _isAdmin)
            {
                purgedStandby = PurgeStandbyList();
                if (purgedStandby) actionMsg += "Standby List purged, ";
            }

            MEMORYSTATUSEX after = GetMemoryStatus();
            ulong freedBytes = 0;
            if (after.ullAvailPhys > beforeAvail)
            {
                freedBytes = after.ullAvailPhys - beforeAvail;
                _totalBytesLiberated += freedBytes;
            }

            _cleanCount++;
            double freedMb = freedBytes / (1024.0 * 1024.0);
            actionMsg += string.Format("+{0:F1} MB RAM restored", freedMb);

            Log(actionMsg);
            AddActivity(string.Format("{0:HH:mm:ss} - Clean #{1}: +{2:F1} MB freed ({3} processes)",
                DateTime.Now, _cleanCount, freedMb, trimmedCount));
        }

        private static int TrimProcessWorkingSets()
        {
            int count = 0;
            Process[] processes = Process.GetProcesses();

            // High priority local model & dev process names to target aggressively
            HashSet<string> modelTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "python", "ollama", "ollama_llama_server", "llama-server", "lmstudio", "text-generation-webui", "node", "chrome", "msedge", "code"
            };

            foreach (Process p in processes)
            {
                try
                {
                    // Skip idle and self
                    if (p.Id == 0 || p.Id == 4 || p.Id == Process.GetCurrentProcess().Id) continue;

                    string pName = p.ProcessName.ToLowerInvariant();
                    bool isModelProcess = modelTargets.Contains(pName);

                    // Aggressively trim model/browser processes (>15MB) or general apps (>35MB)
                    long minTrimSize = isModelProcess ? (15 * 1024 * 1024) : (35 * 1024 * 1024);

                    if (p.WorkingSet64 > minTrimSize)
                    {
                        EmptyWorkingSet(p.Handle);
                        count++;
                    }
                }
                catch
                {
                    // Access denied on protected system processes is expected
                }
                finally
                {
                    p.Dispose();
                }
            }
            return count;
        }

        private static bool PurgeStandbyList()
        {
            try
            {
                int cmd = (int)MemoryListCommand.MemoryPurgeStandbyList;
                GCHandle handle = GCHandle.Alloc(cmd, GCHandleType.Pinned);
                uint status = NtSetSystemInformation(SystemMemoryListInformation, handle.AddrOfPinnedObject(), Marshal.SizeOf(cmd));
                handle.Free();
                return (status == 0);
            }
            catch
            {
                return false;
            }
        }

        private static void CleanTempDirectory()
        {
            try
            {
                string tempPath = Path.GetTempPath();
                if (!Directory.Exists(tempPath)) return;

                DirectoryInfo di = new DirectoryInfo(tempPath);
                DateTime cutoff = DateTime.Now.AddHours(-_config.TempFileMaxAgeHours);
                long bytesCleaned = 0;
                int filesCleaned = 0;

                // Clean files
                foreach (FileInfo file in di.GetFiles())
                {
                    try
                    {
                        if (file.LastWriteTime < cutoff)
                        {
                            long len = file.Length;
                            file.Delete();
                            bytesCleaned += len;
                            filesCleaned++;
                        }
                    }
                    catch { }
                }

                // Clean orphaned directories (e.g. puppeteer, playwright, torchinductor)
                foreach (DirectoryInfo dir in di.GetDirectories())
                {
                    try
                    {
                        string name = dir.Name.ToLowerInvariant();
                        if ((name.Contains("puppeteer") || name.Contains("playwright") || name.Contains("aura-corpus") || name.Contains("tiered-context") || name.Contains("torchinductor"))
                            && dir.LastWriteTime < cutoff)
                        {
                            dir.Delete(true);
                            filesCleaned++;
                        }
                    }
                    catch { }
                }

                if (bytesCleaned > 0)
                {
                    _totalTempBytesCleaned += bytesCleaned;
                    double mb = bytesCleaned / (1024.0 * 1024.0);
                    Log(string.Format("🧹 Temp gravel cleaned: {0} items, {1:F1} MB removed", filesCleaned, mb));
                    AddActivity(string.Format("{0:HH:mm:ss} - Gravel swept: {1:F1} MB temp junk removed", DateTime.Now, mb));
                }
            }
            catch { }
        }

        // ──────────────── Local AI Model Cache Cleaner ────────────────

        private static void CleanModelCache()
        {
            long cleanedBytes = 0;
            int cleanedFiles = 0;

            try
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                // 1. HuggingFace cache: .cache\huggingface\hub
                string hfHubPath = Path.Combine(userProfile, @".cache\huggingface\hub");
                if (Directory.Exists(hfHubPath))
                {
                    DateTime cutoff = DateTime.Now.AddDays(-_config.ModelCacheMaxAgeDays);
                    DateTime shortCutoff = DateTime.Now.AddHours(-2);

                    DirectoryInfo di = new DirectoryInfo(hfHubPath);
                    // Remove incomplete downloads (tmp*) & stale lockfiles
                    foreach (FileInfo fi in di.GetFiles("*", SearchOption.AllDirectories))
                    {
                        try
                        {
                            if ((fi.Name.StartsWith("tmp") || fi.Name.EndsWith(".lock")) && fi.LastWriteTime < shortCutoff)
                            {
                                long len = fi.Length;
                                fi.Delete();
                                cleanedBytes += len;
                                cleanedFiles++;
                            }
                        }
                        catch { }
                    }
                }

                // 2. PyTorch Inductor cache in %TEMP%
                string tempPath = Path.GetTempPath();
                if (Directory.Exists(tempPath))
                {
                    DirectoryInfo di = new DirectoryInfo(tempPath);
                    DateTime cutoff = DateTime.Now.AddHours(-6);
                    foreach (DirectoryInfo dir in di.GetDirectories("torchinductor_*"))
                    {
                        try
                        {
                            if (dir.LastWriteTime < cutoff)
                            {
                                dir.Delete(true);
                                cleanedFiles++;
                            }
                        }
                        catch { }
                    }
                }

                // 3. Ollama partial downloads
                string ollamaBlobs = Path.Combine(userProfile, @".ollama\models\blobs");
                if (Directory.Exists(ollamaBlobs))
                {
                    DirectoryInfo di = new DirectoryInfo(ollamaBlobs);
                    DateTime cutoff = DateTime.Now.AddHours(-2);
                    foreach (FileInfo fi in di.GetFiles("*.partial"))
                    {
                        try
                        {
                            if (fi.LastWriteTime < cutoff)
                            {
                                long len = fi.Length;
                                fi.Delete();
                                cleanedBytes += len;
                                cleanedFiles++;
                            }
                        }
                        catch { }
                    }
                }

                if (cleanedFiles > 0)
                {
                    _totalModelBytesCleaned += cleanedBytes;
                    double mb = cleanedBytes / (1024.0 * 1024.0);
                    Log(string.Format("🤖 Model cache scrubbed: {0} files/dirs, {1:F1} MB purged", cleanedFiles, mb));
                    AddActivity(string.Format("{0:HH:mm:ss} - Model cache: {1} items freed", DateTime.Now, cleanedFiles));
                }
            }
            catch { }
        }

        private static void CleanOrphanProcesses()
        {
            try
            {
                string[] targets = new string[] { "node", "python", "git" };
                foreach (string t in targets)
                {
                    Process[] procs = Process.GetProcessesByName(t);
                    foreach (Process p in procs)
                    {
                        try
                        {
                            if ((DateTime.Now - p.StartTime).TotalHours > 2 && string.IsNullOrEmpty(p.MainWindowTitle))
                            {
                                // Idle background workers
                            }
                        }
                        catch { }
                        finally { p.Dispose(); }
                    }
                }
            }
            catch { }
        }

        private static void EnableHighPrecisionTimer()
        {
            try
            {
                uint cur;
                // 10000 = 1.0 ms in units of 100ns
                NtSetTimerResolution(10000, true, out cur);
            }
            catch { }
        }

        // ──────────────── Stream UI (Fish Tank View) ────────────────

        private static void RenderAquarium(MEMORYSTATUSEX mem, int frame)
        {
            try
            {
                Console.Clear();
            }
            catch { }

            ulong totalMb = mem.ullTotalPhys / (1024 * 1024);
            ulong availMb = mem.ullAvailPhys / (1024 * 1024);
            ulong usedMb = totalMb - availMb;
            uint load = mem.dwMemoryLoad;

            string[] fishSprites = new string[]
            {
                "><(((º>  . . .  ~ ~ ~",
                "  ><(((º>  . .  ~ ~ ~",
                "    ><(((º>  .  ~ ~ ~",
                "      ><(((º>   ~ ~ ~",
                "    <º)))><  .  ~ ~ ~",
                "  <º)))><  . .  ~ ~ ~",
                "<º)))><  . . .  ~ ~ ~"
            };

            string fish = fishSprites[frame % fishSprites.Length];

            Console.WriteLine("================================================================================");
            Console.WriteLine("        🐟 PLECO FOR WINDOWS — THE BACKGROUND FISH TANK CLEANER               ");
            Console.WriteLine("================================================================================");
            Console.WriteLine(string.Format("  Tank Water:  [{0}]", _tankStatus));
            Console.WriteLine(string.Format("  Fish Mood:   {0}", fish));
            Console.WriteLine("--------------------------------------------------------------------------------");

            // Memory Bar
            int barWidth = 36;
            int filled = (int)((load / 100.0) * barWidth);
            if (filled > barWidth) filled = barWidth;
            string bar = new string('=', filled) + new string(' ', barWidth - filled);

            Console.WriteLine(string.Format("  Memory Load: [{0}] {1}%", bar, load));
            Console.WriteLine(string.Format("  RAM State:   {0:N0} MB Used / {1:N0} MB Total ({2:N0} MB Available)", usedMb, totalMb, availMb));
            Console.WriteLine(string.Format("  Privilege:   {0}", _isAdmin ? "Administrator (Full Standby Purge Active)" : "Standard User (Process Working Set Trim)"));
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine(string.Format("  Total RAM Liberated:   {0:F2} GB across {1} nibbles", _totalBytesLiberated / (1024.0 * 1024.0 * 1024.0), _cleanCount));
            Console.WriteLine(string.Format("  Total Temp Junk Freed: {0:F1} MB", _totalTempBytesCleaned / (1024.0 * 1024.0)));
            Console.WriteLine("--------------------------------------------------------------------------------");
            Console.WriteLine("  Recent Tank Log:");
            lock (_recentActivity)
            {
                int start = Math.Max(0, _recentActivity.Count - 5);
                for (int i = start; i < _recentActivity.Count; i++)
                {
                    Console.WriteLine("    * " + _recentActivity[i]);
                }
                if (_recentActivity.Count == 0)
                {
                    Console.WriteLine("    (Fish swimming peacefully, waiting for water to get murky...)");
                }
            }
            Console.WriteLine("================================================================================");
            Console.WriteLine("  Press Ctrl+C to minimize Pleco to background.");
        }

        // ──────────────── Helper Methods ────────────────

        private static MEMORYSTATUSEX GetMemoryStatus()
        {
            MEMORYSTATUSEX mem = new MEMORYSTATUSEX();
            GlobalMemoryStatusEx(mem);
            return mem;
        }

        private static bool CheckAdminStatus()
        {
            try
            {
                using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
                {
                    WindowsPrincipal principal = new WindowsPrincipal(identity);
                    return principal.IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool EnablePrivilege(string privilegeName)
        {
            try
            {
                IntPtr hToken = IntPtr.Zero;
                if (!OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, ref hToken))
                    return false;

                long luid = 0;
                if (!LookupPrivilegeValue(null, privilegeName, ref luid))
                    return false;

                TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES();
                tp.PrivilegeCount = 1;
                tp.Luid = luid;
                tp.Attributes = SE_PRIVILEGE_ENABLED;

                return AdjustTokenPrivileges(hToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
            }
            catch
            {
                return false;
            }
        }

        private static void AddActivity(string entry)
        {
            lock (_recentActivity)
            {
                _recentActivity.Add(entry);
                if (_recentActivity.Count > 20) _recentActivity.RemoveAt(0);
            }
        }

        private static void Log(string msg)
        {
            string line = string.Format("{0:yyyy-MM-dd HH:mm:ss} 🐟 [pleco] {1}", DateTime.Now, msg);
            if (!_streamMode)
            {
                Console.WriteLine(line);
            }

            if (_config.LogToFile)
            {
                try
                {
                    File.AppendAllText(_config.LogFilePath, line + Environment.NewLine);
                }
                catch { }
            }
        }

        private static void PrintStatus()
        {
            MEMORYSTATUSEX mem = GetMemoryStatus();
            ulong totalMb = mem.ullTotalPhys / (1024 * 1024);
            ulong availMb = mem.ullAvailPhys / (1024 * 1024);
            Console.WriteLine("========================================================");
            Console.WriteLine("             🐟 PLECO TANK HEALTH CHECK                 ");
            Console.WriteLine("========================================================");
            Console.WriteLine(string.Format("  Total Physical RAM:     {0:N0} MB", totalMb));
            Console.WriteLine(string.Format("  Available Headroom:     {0:N0} MB", availMb));
            Console.WriteLine(string.Format("  Current Memory Load:    {0}%", mem.dwMemoryLoad));
            Console.WriteLine(string.Format("  Threshold Trigger:      {0}% or < {1} MB", _config.MemoryThresholdPercent, _config.MinAvailableRamMb));
            Console.WriteLine(string.Format("  Mode:                   {0}", _isAdmin ? "Admin (Standby Purge + Working Set)" : "User (Working Set Trimming)"));
            Console.WriteLine("========================================================");
        }

        private static void LoadConfig()
        {
            try
            {
                if (File.Exists("PlecoConfig.json"))
                {
                    string json = File.ReadAllText("PlecoConfig.json");
                    if (json.Contains("\"checkIntervalSeconds\":"))
                        _config.CheckIntervalSeconds = ExtractInt(json, "\"checkIntervalSeconds\":", _config.CheckIntervalSeconds);
                    if (json.Contains("\"memoryThresholdPercent\":"))
                        _config.MemoryThresholdPercent = ExtractInt(json, "\"memoryThresholdPercent\":", _config.MemoryThresholdPercent);
                    if (json.Contains("\"minAvailableRamMb\":"))
                        _config.MinAvailableRamMb = ExtractInt(json, "\"minAvailableRamMb\":", _config.MinAvailableRamMb);
                    if (json.Contains("\"cooldownSeconds\":"))
                        _config.CooldownSeconds = ExtractInt(json, "\"cooldownSeconds\":", _config.CooldownSeconds);
                }
            }
            catch { }
        }

        private static int ExtractInt(string json, string key, int fallback)
        {
            try
            {
                int idx = json.IndexOf(key);
                if (idx < 0) return fallback;
                int start = idx + key.Length;
                int end = json.IndexOfAny(new char[] { ',', '}', '\r', '\n' }, start);
                string val = json.Substring(start, end - start).Trim();
                int res;
                if (int.TryParse(val, out res)) return res;
            }
            catch { }
            return fallback;
        }

        private static void PrintHelp()
        {
            Console.WriteLine("Pleco for Windows — The Background Tank Cleaner Fish");
            Console.WriteLine("Usage: pleco.exe [options]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --stream, -s            Launch interactive live ASCII fish tank dashboard");
            Console.WriteLine("  --once, -1              Run a single tank clean pass and exit");
            Console.WriteLine("  --status                Check current memory health and headroom");
            Console.WriteLine("  --clean-model-cache     Purge stale Hugging Face, Torch & Ollama caches");
            Console.WriteLine("  --help, -h              Show this help dialog");
            Console.WriteLine();
            Console.WriteLine("Config: Customize thresholds in PlecoConfig.json");
        }
    }
}
