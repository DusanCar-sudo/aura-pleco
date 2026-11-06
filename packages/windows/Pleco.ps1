<#
.SYNOPSIS
    Pleco for Windows — The Background Tank Cleaner Fish (PowerShell Edition)
    Monitors RAM headroom, trims process working sets, purges Standby List, sweeps %TEMP% junk,
    scrubs stale AI model caches (HuggingFace, PyTorch, Ollama), and maintains Windows desktop
    snappiness like a cleaner fish in an aquarium.

.PARAMETER Stream
    Launches the live interactive ASCII aquarium console dashboard.

.PARAMETER Once
    Runs a single cleaning cycle (RAM, temp, model caches) and exits.

.PARAMETER Status
    Shows current tank health, memory headroom, and cache statistics.

.PARAMETER CleanModelCache
    Immediately scrubs stale HuggingFace cache files, partial Ollama blobs, and PyTorch inductor temps.

.EXAMPLE
    .\Pleco.ps1 -Stream
    .\Pleco.ps1 -Once
    .\Pleco.ps1 -CleanModelCache
#>

[CmdletBinding()]
param (
    [switch]$Stream,
    [switch]$Once,
    [switch]$Status,
    [switch]$CleanModelCache,
    [int]$MinAvailableMb = 2500,
    [int]$MemoryThresholdPercent = 80,
    [int]$IntervalSec = 20,
    [int]$CooldownSec = 75
)

$OutputEncoding = [System.Text.Encoding]::UTF8

# ────────────────── Native Win32 / NT Definition ──────────────────
$NativeCode = @"
using System;
using System.Runtime.InteropServices;

public class Win32Mem {
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class MEMORYSTATUSEX {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;

        public MEMORYSTATUSEX() {
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

    public static MEMORYSTATUSEX GetStatus() {
        MEMORYSTATUSEX mem = new MEMORYSTATUSEX();
        GlobalMemoryStatusEx(mem);
        return mem;
    }

    public static bool PurgeStandby() {
        try {
            int cmd = 3; // MemoryPurgeStandbyList
            GCHandle handle = GCHandle.Alloc(cmd, GCHandleType.Pinned);
            uint status = NtSetSystemInformation(80, handle.AddrOfPinnedObject(), Marshal.SizeOf(cmd));
            handle.Free();
            return (status == 0);
        } catch {
            return false;
        }
    }

    public static void SetHighPrecisionTimer() {
        try {
            uint cur;
            NtSetTimerResolution(10000, true, out cur); // 1.0ms
        } catch {}
    }
}
"@

if (-not ([System.Management.Automation.PSTypeName]'Win32Mem').Type) {
    Add-Type -TypeDefinition $NativeCode -ErrorAction SilentlyContinue
}

# Check Admin Privileges
$IsAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

# Enable 1.0ms timer resolution for high snappiness
[Win32Mem]::SetHighPrecisionTimer()

function Get-MemStatus {
    $m = [Win32Mem]::GetStatus()
    $totalMb = [math]::Round($m.ullTotalPhys / 1MB)
    $availMb = [math]::Round($m.ullAvailPhys / 1MB)
    $usedMb = $totalMb - $availMb
    [PSCustomObject]@{
        TotalMB  = $totalMb
        AvailMB  = $availMb
        UsedMB   = $usedMb
        LoadPct  = $m.dwMemoryLoad
    }
}

function Invoke-PlecoNibble {
    $before = Get-MemStatus
    $trimmed = 0

    $modelTargets = @("python", "ollama", "ollama_llama_server", "llama-server", "lmstudio", "node", "chrome", "msedge")

    # 1. Trim process working sets (aggressively on model runners > 15MB, other apps > 35MB)
    Get-Process -ErrorAction SilentlyContinue | Where-Object {
        $_.Id -gt 4 -and $_.Id -ne $PID
    } | ForEach-Object {
        try {
            $isModel = $modelTargets -contains $_.ProcessName.ToLower()
            $threshold = if ($isModel) { 15MB } else { 35MB }

            if ($_.WorkingSet64 -gt $threshold) {
                [Win32Mem]::EmptyWorkingSet($_.Handle) | Out-Null
                $trimmed++
            }
        } catch {}
    }

    # 2. Purge Standby List if Admin
    $standbyPurged = $false
    if ($IsAdmin) {
        $standbyPurged = [Win32Mem]::PurgeStandby()
    }

    $after = Get-MemStatus
    $freedMb = [math]::Max(0, ($after.AvailMB - $before.AvailMB))

    $msg = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') 🐟 [pleco] Nibbled: $trimmed apps trimmed"
    if ($standbyPurged) { $msg += ", Standby list purged" }
    $msg += " (+$freedMb MB RAM restored)"

    Write-Host $msg -ForegroundColor Green
    return [PSCustomObject]@{
        FreedMB = $freedMb
        Trimmed = $trimmed
    }
}

function Clean-TempGravel {
    try {
        $tempPath = [System.IO.Path]::GetTempPath()
        $cutoff = (Get-Date).AddHours(-2)
        $cleanedBytes = 0

        Get-ChildItem -Path $tempPath -File -ErrorAction SilentlyContinue | Where-Object { $_.LastWriteTime -lt $cutoff } | ForEach-Object {
            try {
                $cleanedBytes += $_.Length
                Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
            } catch {}
        }

        Get-ChildItem -Path $tempPath -Directory -ErrorAction SilentlyContinue | Where-Object {
            ($_.Name -match "puppeteer|playwright|aura-corpus|tiered-context|torchinductor") -and ($_.LastWriteTime -lt $cutoff)
        } | ForEach-Object {
            try {
                Remove-Item $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
            } catch {}
        }

        if ($cleanedBytes -gt 0) {
            $cleanedMb = [math]::Round($cleanedBytes / 1MB, 1)
            Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') 🧹 [pleco] Swept $cleanedMb MB gravel from %TEMP%" -ForegroundColor Cyan
        }
    } catch {}
}

function Clean-ModelCache {
    try {
        $userProfile = [Environment]::GetFolderPath("UserProfile")
        $cleanedFiles = 0

        # 1. HuggingFace cache: .cache\huggingface\hub
        $hfHubPath = Join-Path $userProfile ".cache\huggingface\hub"
        if (Test-Path $hfHubPath) {
            $shortCutoff = (Get-Date).AddHours(-2)
            Get-ChildItem -Path $hfHubPath -Recurse -File -ErrorAction SilentlyContinue | Where-Object {
                ($_.Name -like "tmp*" -or $_.Name -like "*.lock") -and ($_.LastWriteTime -lt $shortCutoff)
            } | ForEach-Object {
                try {
                    Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
                    $cleanedFiles++
                } catch {}
            }
        }

        # 2. Ollama partial blobs
        $ollamaBlobs = Join-Path $userProfile ".ollama\models\blobs"
        if (Test-Path $ollamaBlobs) {
            $shortCutoff = (Get-Date).AddHours(-2)
            Get-ChildItem -Path $ollamaBlobs -Filter "*.partial" -ErrorAction SilentlyContinue | Where-Object {
                $_.LastWriteTime -lt $shortCutoff
            } | ForEach-Object {
                try {
                    Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue
                    $cleanedFiles++
                } catch {}
            }
        }

        if ($cleanedFiles -gt 0) {
            Write-Host "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss') 🤖 [pleco] Cleaned $cleanedFiles stale AI model artifacts" -ForegroundColor Cyan
        }
    } catch {}
}

# ────────────────── CLI Dispatch ──────────────────

if ($Status) {
    $m = Get-MemStatus
    Write-Host "========================================================" -ForegroundColor Cyan
    Write-Host "             🐟 PLECO TANK HEALTH CHECK                 " -ForegroundColor White
    Write-Host "========================================================" -ForegroundColor Cyan
    Write-Host "  Total Physical RAM:     $($m.TotalMB) MB"
    Write-Host "  Available Headroom:     $($m.AvailMB) MB"
    Write-Host "  Current Memory Load:    $($m.LoadPct)%"
    Write-Host "  Trigger Threshold:      $MemoryThresholdPercent% or < $MinAvailableMb MB"
    Write-Host "  Admin Mode:             $(if ($IsAdmin) {'Yes (Full Standby Purge)'} else {'No (Process Trimming Only)'})"
    Write-Host "========================================================" -ForegroundColor Cyan
    exit 0
}

if ($CleanModelCache) {
    Write-Host "🤖 Scrubbing local AI model caches..." -ForegroundColor Cyan
    Clean-ModelCache
    exit 0
}

if ($Once) {
    Write-Host "🐟 Pleco running single tank-cleaning pass..." -ForegroundColor Cyan
    Invoke-PlecoNibble | Out-Null
    Clean-TempGravel
    Clean-ModelCache
    exit 0
}

# ────────────────── Main Background Loop / Stream ──────────────────

Write-Host "🐟 Pleco swimming into the Windows tank! (Threshold: <$MinAvailableMb MB, Interval: ${IntervalSec}s)" -ForegroundColor Cyan
$lastHeal = [DateTime]::MinValue
$tick = 0
$fishSprites = @(
    "><(((º>  . . .  ~ ~ ~",
    "  ><(((º>  . .  ~ ~ ~",
    "    ><(((º>  .  ~ ~ ~",
    "      ><(((º>   ~ ~ ~",
    "    <º)))><  .  ~ ~ ~",
    "  <º)))><  . .  ~ ~ ~",
    "<º)))><  . . .  ~ ~ ~"
)

while ($true) {
    $tick++
    $m = Get-MemStatus
    $needHeal = ($m.LoadPct -ge $MemoryThresholdPercent) -or ($m.AvailMB -lt $MinAvailableMb)
    $tankStatus = "CRYSTAL CLEAR"

    if ($needHeal) {
        $elapsed = ((Get-Date) - $lastHeal).TotalSeconds
        if ($elapsed -ge $CooldownSec) {
            $tankStatus = "NIBBLING ALGAE (CLEANING)"
            Invoke-PlecoNibble | Out-Null
            $lastHeal = Get-Date
            $tankStatus = "CRYSTAL CLEAR"
        } else {
            $rem = [math]::Round($CooldownSec - $elapsed)
            $tankStatus = "WATER MURKY (Cooldown: ${rem}s)"
        }
    }

    # Periodic temp sweep (~5 mins = 15 ticks)
    if ($tick % 15 -eq 0) {
        Clean-TempGravel
    }

    # Periodic model cache sweep (~30 mins = 90 ticks)
    if ($tick % 90 -eq 0) {
        Clean-ModelCache
    }

    if ($Stream) {
        Clear-Host
        $fish = $fishSprites[$tick % $fishSprites.Length]
        $barWidth = 36
        $filled = [math]::Min($barWidth, [int](($m.LoadPct / 100.0) * $barWidth))
        $bar = ("=" * $filled) + (" " * ($barWidth - $filled))

        Write-Host "================================================================================" -ForegroundColor Cyan
        Write-Host "        🐟 PLECO FOR WINDOWS — THE BACKGROUND FISH TANK CLEANER               " -ForegroundColor White
        Write-Host "================================================================================" -ForegroundColor Cyan
        Write-Host "  Tank Water:  [$tankStatus]" -ForegroundColor $(if ($tankStatus -like "*MURKY*") {'Yellow'} else {'Green'})
        Write-Host "  Fish Mood:   $fish" -ForegroundColor Cyan
        Write-Host "--------------------------------------------------------------------------------"
        Write-Host "  Memory Load: [$bar] $($m.LoadPct)%"
        Write-Host "  RAM State:   $($m.UsedMB) MB Used / $($m.TotalMB) MB Total ($($m.AvailMB) MB Available)"
        Write-Host "  Privilege:   $(if ($IsAdmin) {'Administrator (Standby Purge Active)'} else {'Standard User (Working Set Trim)'})"
        Write-Host "================================================================================" -ForegroundColor Cyan
        Write-Host "  Press Ctrl+C to stop."
    }

    Start-Sleep -Seconds $IntervalSec
}
