#!/usr/bin/env bash
set -e

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BIN_DIR="$HOME/.local/bin"
SYSTEMD_USER_DIR="$HOME/.config/systemd/user"

echo "========================================================"
echo "    🐟 AURA PLECO (LINUX / AURA OS) INSTALLER"
echo "========================================================"

mkdir -p "$BIN_DIR" "$SYSTEMD_USER_DIR"

echo "[*] Installing aura-pleco executable to $BIN_DIR/aura-pleco..."
install -m 755 "$DIR/aura-pleco" "$BIN_DIR/aura-pleco"

echo "[*] Installing aura-pleco-memlog profiler to $BIN_DIR/aura-pleco-memlog..."
install -m 755 "$DIR/aura-pleco-memlog" "$BIN_DIR/aura-pleco-memlog"

echo "[*] Installing systemd user unit to $SYSTEMD_USER_DIR/aura-pleco.service..."
cp "$DIR/aura-pleco.service" "$SYSTEMD_USER_DIR/aura-pleco.service"

MODEL_DIR="$HOME/.local/share/aura-pleco/models"
GGUF_URL="https://huggingface.co/LeanProgressIQ/LFM3-1.7B-aura-grounded/resolve/main/LFM3-1.7B-aura-Q8_0.gguf"

if [ "${1:-}" = "--with-model" ]; then
    echo "[*] Downloading LFM3-1.7B-aura-grounded GGUF (1.8 GB) from Hugging Face..."
    mkdir -p "$MODEL_DIR"
    curl -L --fail -o "$MODEL_DIR/LFM3-1.7B-aura-Q8_0.gguf" "$GGUF_URL"
    if command -v ollama >/dev/null 2>&1; then
        echo "[*] Registering model with Ollama as aura-grounded..."
        printf 'FROM %s\n' "$MODEL_DIR/LFM3-1.7B-aura-Q8_0.gguf" > "$MODEL_DIR/Modelfile"
        ollama create aura-grounded -f "$MODEL_DIR/Modelfile"
        echo "[*] Try it: ollama run aura-grounded"
    else
        echo "[*] Ollama not found — point LM Studio (or any GGUF runner) at:"
        echo "    $MODEL_DIR/LFM3-1.7B-aura-Q8_0.gguf"
    fi
fi

echo "[*] Reloading systemd user daemon and enabling service..."
systemctl --user daemon-reload
systemctl --user enable --now aura-pleco.service

echo "========================================================"
echo "[+] SUCCESS: Aura Pleco is active and swimming in your tank!"
echo "    Check logs: journalctl --user -u aura-pleco -f"
echo "========================================================"
