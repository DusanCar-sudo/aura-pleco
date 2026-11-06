# 🐟 Aura Pleco for Linux / Aura OS

> *"Just like that fish you buy for your fish tank that stays quietly in the background eating algae and keeping the glass crystal clear — Aura Pleco keeps your Linux memory, temporary storage, and local AI model caches clean, lean, and snappy."*

---

### Features

- **RAM Headroom Sentinel**: Checks `/proc/meminfo` every 20s. When available RAM dips below threshold (default `2,500 MB`), triggers `/usr/local/bin/aura-ram-heal`, `/usr/local/bin/aura-os-ram-liberate`, or kernel page compaction + cache flush.
- **AI & Local Model Cache Scrubber**: Cleans stale Hugging Face cache files (`~/.cache/huggingface`), incomplete downloads (`tmp*`, partial locks), PyTorch inductor caches (`/tmp/torchinductor_*`), Triton caches (`~/.triton/cache`), and aborted Ollama model pulls.
- **AI Process Memory Trimming**: Clears unmapped working set pages and clears memory leaks across long-running Python, Ollama, and llama-server model runner processes.
- **Tmpfs Algae Scrubber**: Cleans stale test dirs (`/tmp/aura-corpus-*`, `/tmp/tiered-context-test-*`, orphaned browser profiles) older than 2 hours every 5 minutes.
- **Interactive Aquarium Dashboard**: Live ASCII fish tank dashboard in your terminal via `aura-pleco --stream`.
- **Zero Overhead**: Consumes only ~2.6 MB of RAM and runs at low priority.

---

### Installation

```bash
cd packages/linux
./install.sh
```

This installs `aura-pleco` to `~/.local/bin/aura-pleco` and sets up the `aura-pleco.service` systemd user service.

---

### Usage & Commands

```bash
# Check current tank status and model cache sizes
aura-pleco --status

# Launch live interactive ASCII fish tank in terminal
aura-pleco --stream

# Real-time AI model memory logger & profiler
aura-pleco-memlog --snapshot                 # Instant breakdown of model RSS & system memory
aura-pleco-memlog --watch                    # Live terminal dashboard updating every second
aura-pleco-memlog --log memory-audit.csv     # Continuous background/foreground CSV logging
aura-pleco-memlog --test                     # Run full inference memory test with local model

# Trigger immediate RAM heal and compaction
aura-pleco --heal

# Clean stale local model caches
aura-pleco --clean-model-cache

# Run single complete tank sweep and exit
aura-pleco --run-now

# Check background daemon status & logs
systemctl --user status aura-pleco
journalctl --user -u aura-pleco -f
```

---

### Uninstall

```bash
cd packages/linux
./uninstall.sh
```
