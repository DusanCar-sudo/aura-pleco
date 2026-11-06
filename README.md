# 🐟 Aura Pleco

> **The Background "Cleaner Fish" Daemon for Linux & Windows Workstations.**  
> *Keeping memory, temporary storage, and local AI model caches crystal clear, lean, and snappy.*

[![License](https://img.shields.io/badge/License-Apache_2.0-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/Platform-Linux%20%7C%20Windows-brightgreen.svg)]()
[![Footprint](https://img.shields.io/badge/RAM_Footprint-~3MB-success.svg)]()

---

```
================================================================================
        🐟 AURA PLECO — THE BACKGROUND FISH TANK CLEANER
================================================================================
  Tank Water:  [CRYSTAL CLEAR]
  Fish Mood:     ><(((º>  . .  ~ ~ ~
--------------------------------------------------------------------------------
  Memory Load: [=================>                  ] 46%
  RAM State:   7,520 MB Used / 16,384 MB Total (8,864 MB Available)
  Mode:        Sentinel Active (Compaction, Working Set Trim, Model Cache Scrub)
--------------------------------------------------------------------------------
  Total RAM Liberated:   4.85 GB across 12 nibbles
  Total Temp Junk Freed: 312.4 MB
--------------------------------------------------------------------------------
  Recent Tank Log:
    * 14:02:15 - Clean #12: +1,240.5 MB freed
    * 14:00:10 - Gravel swept: 84.2 MB temp junk removed
    * 13:58:30 - Model cache: 14 stale AI artifacts purged
================================================================================
```

---

## 🐠 What is an "Aura Pleco"? (The Cleaner Fish Metaphor)

In aquarium hobbyism, the **Pleco** (*Hypostomus plecostomus*, or suckermouth armored catfish) is the quintessential "cleaner fish". 

When you set up a fish tank, algae slowly blooms on the glass, uneaten flakes sink into the gravel, and over time the water turns cloudy and murky. Rather than constantly tearing down the tank and scrubbing it by hand, aquarists introduce a Pleco. The pleco attaches to the glass, swims peacefully along the bottom, gently eats the algae, vacuums the gravel, and keeps the tank water crystal clear 24/7 without disturbing any of the other fish.

**Aura Pleco brings that exact principle to modern developer workstations.**

Whether you are running Linux (Aura OS, Arch, Ubuntu) or Windows 11, operating systems accumulate background sludge after hours of intense work:
- Inactive apps, Electron utilities, and browsers hoard physical RAM.
- Windows file caches clog the unreleased Standby List.
- **Local AI & LLM development** (PyTorch, Ollama, Hugging Face, llama.cpp, vLLM) leaves gigabytes of stale model weights, abandoned partial download blobs (`*.partial`, `tmp*`), compile caches (`torchinductor_*`), and bloated process memory allocations.
- Ephemeral test runs and temp directories fill up storage.

**Aura Pleco is your system's cleaner fish.** It lives quietly in the background, using ~3 MB of RAM and 0% CPU. When your system "water" gets murky (RAM headroom drops low or cache bloat accumulates), it gently nibbles away the debris, compacts memory, trims idle working sets, and restores your system to a snappy, crystal-clear state.

---

## ✨ Features

- **RAM Headroom Sentinel:** Checks physical memory headroom every 20 seconds. If available RAM drops below threshold (e.g. `< 2,500 MB` or `> 80% load`), it automatically compacts pages, drops inactive page caches, and flushes Standby memory.
- **Local AI Model Cache Scrubber:**
  - Cleans stale Hugging Face cache files (`~/.cache/huggingface` / `%USERPROFILE%\.cache\huggingface`).
  - Removes incomplete/aborted model weights (`tmp*`, `.lock` files).
  - Scrubs PyTorch Inductor compile caches (`torchinductor_*`) and Triton kernel caches.
  - Removes aborted Ollama model pulls (`*.partial` blobs).
- **Process Working Set Trimmer:** Safely signals the OS memory manager (`EmptyWorkingSet` on Windows, page compaction on Linux) to release unreferenced physical memory pages from background Python, Ollama, browser, and IDE processes without terminating them.
- **Tmpfs & Gravel Cleaner:** Periodically scrubs stale test directories (`aura-corpus-*`, `puppeteer_dev_chrome_profile-*`, `playwright-*`) older than 2 hours.
- **Timer Resolution Optimizer (Windows):** Locks Windows kernel timer resolution to 1.0 ms (`NtSetTimerResolution`), eliminating micro-stutters and boosting desktop responsiveness.
- **Live Interactive Aquarium (`--stream`):** Launch an ASCII fish tank dashboard right in your terminal to monitor memory health, cache metrics, and the pleco's activity in real time.
- **One-Shot & CLI Automation:** Run on-demand sweeps via `--run-now`, `--heal`, `--clean-model-cache`, or `--status`.

---

## 📦 Packages in this Repository

This repository contains two ready-to-use packages tailored for each operating system:

```
aura-pleco/
├── LICENSE                 # Apache License 2.0
├── README.md               # Main documentation & architecture guide
└── packages/
    ├── linux/              # Linux & Aura OS Package
    │   ├── aura-pleco      # Background Bash daemon & CLI tool
    │   ├── aura-pleco.service # systemd user unit
    │   ├── install.sh      # 1-click user installer
    │   ├── uninstall.sh    # 1-click clean uninstaller
    │   └── README.md       # Linux documentation
    │
    └── windows/            # Windows 10/11 Package
        ├── Pleco.cs        # Pure C# standalone engine (Win32/NT native)
        ├── Pleco.ps1       # Pure PowerShell edition
        ├── PlecoConfig.json# Configurable thresholds & flags
        ├── build.bat       # Native compile via built-in csc.exe (zero external deps)
        ├── install.bat     # 1-click Windows Task Scheduler installer
        ├── uninstall.bat   # 1-click clean uninstaller
        └── README.md       # Windows documentation
```

---

## 🚀 Quick Start

### 🐧 Linux / Aura OS

#### 1-Click Install (User Service)
```bash
git clone https://github.com/DusanCar-sudo/aura-pleco.git
cd aura-pleco/packages/linux
./install.sh
```

#### Commands
```bash
# Check tank health & model cache sizes
aura-pleco --status

# Launch the live ASCII aquarium
aura-pleco --stream

# Trigger immediate RAM heal & compaction
aura-pleco --heal

# Clean stale HuggingFace/Torch/Ollama model caches
aura-pleco --clean-model-cache

# Run single full pass
aura-pleco --run-now

# Check background daemon logs
journalctl --user -u aura-pleco -f
```

---

### 🪟 Windows 10 / 11

#### 1-Click Install (No Visual Studio or SDK required!)
Uses the built-in Windows C# compiler (`csc.exe` shipped with .NET Framework):
1. Open PowerShell or Command Prompt as **Administrator**.
2. Run:
```cmd
cd packages\windows
install.bat
```
Pleco compiles in ~2 seconds, registers a background task in Windows Task Scheduler, and starts swimming immediately on boot.

#### Commands
```cmd
:: Launch interactive live ASCII aquarium
pleco.exe --stream

:: Check tank health & RAM headroom
pleco.exe --status

:: Clean stale local AI model caches
pleco.exe --clean-model-cache

:: Run a single cleaning cycle
pleco.exe --once
```

*(Or use the PowerShell edition: `.\Pleco.ps1 -Stream`)*

---

## ⚙️ Configuration

Both packages support customization of intervals, RAM thresholds, and cleaning features.

### Windows (`PlecoConfig.json`)
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

### Linux (Environment Variables / `.service`)
- `PLECO_THRESHOLD_MB`: RAM headroom in MB to trigger heal (default: `2500`)
- `PLECO_INTERVAL`: Wakeup interval in seconds (default: `20`)
- `PLECO_SWEEP_TICKS`: Ticks between temp sweeps (default: `15`, ~5 min)
- `PLECO_MODEL_TICKS`: Ticks between model cache sweeps (default: `90`, ~30 min)
- `PLECO_CACHE_DIR`: HuggingFace cache location (default: `~/.cache/huggingface`)

---

## 🛡 Security & Safety Principles

1. **Zero Data Loss:** Trimming working sets does **not** terminate processes or discard active application data; it merely prompts the OS memory manager to release unreferenced physical memory pages.
2. **Safe Model Cache Pruning:** Model cache cleanups only remove incomplete/temporary files (`tmp*`, broken lockfiles, partial downloads) and weights that haven't been accessed for more than 30 days. Active models and completed checkpoints are never disturbed.
3. **Thrashing Prevention:** Hard safety cooldowns (75–90s) prevent back-to-back cleanups during continuous heavy compilation or batch model loading.
4. **Zero Bloat:** Zero third-party runtime dependencies on both Linux (pure Bash + coreutils) and Windows (native Win32/NT P/Invoke compiled by the OS).

---

## 📄 License

Licensed under the **Apache License, Version 2.0**. See [LICENSE](LICENSE) for details.
