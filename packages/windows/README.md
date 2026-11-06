# 🐟 Pleco for Windows — The Background Tank Cleaner Fish

> *"Just like that fish you buy for your fish tank that stays quietly in the background eating algae and keeping the glass crystal clear — Pleco keeps your Windows memory, temporary storage, and local AI model caches clean, lean, and snappy."*

---

```
================================================================================
        🐟 PLECO FOR WINDOWS — THE BACKGROUND FISH TANK CLEANER               
================================================================================
  Tank Water:  [CRYSTAL CLEAR]
  Fish Mood:     ><(((º>  . .  ~ ~ ~
--------------------------------------------------------------------------------
  Memory Load: [=================>                  ] 46%
  RAM State:   7,520 MB Used / 16,384 MB Total (8,864 MB Available)
  Privilege:   Administrator (Full Standby Purge Active)
--------------------------------------------------------------------------------
  Total RAM Liberated:   4.85 GB across 12 nibbles
  Total Temp Junk Freed: 312.4 MB
--------------------------------------------------------------------------------
  Recent Tank Log:
    * 14:02:15 - Clean #12: +1,240.5 MB freed (42 processes)
    * 14:00:10 - Gravel swept: 84.2 MB temp junk removed
    * 13:58:30 - Model cache: 14 items freed
================================================================================
```

---

## 💡 The Problem with Windows After Hours of Use & AI Iteration

Windows workstations running heavy developer workflows and local AI inference accumulate memory bloat and background sludge:
1. **Standby List Bloat:** Windows caches previously opened files into the Standby list. Over time, this Standby memory refuses to release promptly, causing micro-stutters, gaming frame drops, and laggy IDE/browser multitasking.
2. **AI Model Runtimes & Working Set Retention:** Local LLMs and inference frameworks (Ollama, LM Studio, Python/PyTorch, Electron apps, Chrome) hold massive unreferenced physical working sets long after requests complete.
3. **Model Cache Debris:** `.cache\huggingface\hub` accumulates abandoned temporary download chunks (`tmp*`) and orphaned locks; `%TEMP%` fills with PyTorch inductor compile directories (`torchinductor_*`), and aborted Ollama model pulls leave dead partial blobs.
4. **Coarse Timer Resolution:** Windows defaults to a sluggish 15.6ms timer tick, making context switching and UI pacing feel less snappy.

---

## 🌊 How Pleco Works

Pleco runs silently in the background with negligible footprint (~5 MB RAM, 0% CPU):

1. **Water Murkiness Sentinel (Every 20 seconds):**
   - Monitors physical memory headroom via Win32 `GlobalMemoryStatusEx`.
   - When Available RAM falls below threshold (e.g. `< 2,500 MB` or `> 80% load`), Pleco begins a gentle nibble:
     - **Process Working Set Trimming:** Calls native `EmptyWorkingSet` on bloated background processes (especially targeting Python, Ollama, browsers, and background tools).
     - **Standby List Purge:** Uses NT native `NtSetSystemInformation` (`SystemMemoryListInformation`) to flush stale standby pages without touching active working state.
     - **Safety Cooldown:** Enforces a 75-second cooldown between heavy cleanups to eliminate thrashing.
2. **AI Model Cache Scrubber (Every 30 minutes or on demand):**
   - Cleans orphaned HuggingFace `tmp*` files, abandoned lockfiles, PyTorch Inductor caches (`%TEMP%\torchinductor_*`), and aborted Ollama model pulls (`*.partial`).
3. **Gravel & Algae Cleaner (Every 5 minutes):**
   - Sweeps `%TEMP%` and `%LOCALAPPDATA%\Temp` for abandoned ephemeral directories and logs older than 2 hours.
4. **Snappiness Enhancer:**
   - Locks kernel timer resolution to **1.0 ms** (`NtSetTimerResolution`), delivering instantaneous mouse/keyboard response and eliminating UI frame drops.

---

## 🚀 Quick Start (Zero Dependencies)

### Option 1: Double-Click Install (Recommended)
1. Run `install.bat` (Run as Administrator for full Standby Purge capabilities).
2. Pleco compiles natively using Windows' built-in C# compiler (`csc.exe`) in 2 seconds and registers a background Scheduled Task `PlecoTankCleaner`.
3. Pleco starts immediately and runs quietly on every boot.

### Option 2: Live Aquarium Dashboard (Interactive Stream)
Want to watch the cleaner fish at work? Run:
```cmd
pleco.exe --stream
```
*(Or in PowerShell)*
```powershell
.\Pleco.ps1 -Stream
```

### Option 3: Single Quick Pass
```cmd
pleco.exe --once
```

### Option 4: Clean Model Caches
```cmd
pleco.exe --clean-model-cache
```

---

## 🛠 Project Structure

| File | Purpose |
| :--- | :--- |
| **`Pleco.cs`** | Pure C# standalone engine with Win32/NT P/Invoke. Zero third-party dependencies. |
| **`Pleco.ps1`** | Pure PowerShell edition with identical features and embedded Win32 P/Invoke. |
| **`PlecoConfig.json`** | Customizable thresholds (Interval, RAM trigger, cooldown, temp file age, model cache options). |
| **`build.bat`** | Compiles `Pleco.cs` into `pleco.exe` using Windows' built-in .NET `csc.exe`. |
| **`install.bat`** | 1-click setup into Windows Task Scheduler with automatic restart on boot. |
| **`uninstall.bat`** | 1-click clean removal. |

---

## ⚙️ Configuration (`PlecoConfig.json`)

```json
{
  "checkIntervalSeconds": 20,
  "memoryThresholdPercent": 80,
  "minAvailableRamMb": 2500,
  "cooldownSeconds": 75,
  "trimWorkingSets": true,
  "purgeStandbyList": true,
  "optimizeTimerResolution": true,
  "cleanTempFiles": true,
  "tempFileMaxAgeHours": 2,
  "cleanOrphanProcesses": true,
  "cleanModelCache": true,
  "modelCacheMaxAgeDays": 30,
  "cleanTorchInductorCache": true,
  "logToFile": true,
  "logFilePath": "pleco.log"
}
```

---

## 🛡 Security & Permissions

- **Non-Admin Mode:** Trims user-accessible working sets and cleans user `%TEMP%`. Safe, effective, and requires no special rights.
- **Admin Mode:** Grants `SeIncreaseQuotaPrivilege` and `SeProfileSingleProcessPrivilege` to allow purging Windows Standby List and flushing system file cache.
- **Never Kills Active Work:** Trimming a working set only releases unreferenced physical pages back to the OS memory manager — it never terminates processes or loses state.
