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

echo "[*] Reloading systemd user daemon and enabling service..."
systemctl --user daemon-reload
systemctl --user enable --now aura-pleco.service

echo "========================================================"
echo "[+] SUCCESS: Aura Pleco is active and swimming in your tank!"
echo "    Check logs: journalctl --user -u aura-pleco -f"
echo "========================================================"
