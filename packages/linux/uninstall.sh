#!/usr/bin/env bash
set -e

echo "========================================================"
echo "    🐟 AURA PLECO (LINUX / AURA OS) UNINSTALLER"
echo "========================================================"

echo "[*] Stopping and disabling aura-pleco service..."
systemctl --user stop aura-pleco.service 2>/dev/null || true
systemctl --user disable aura-pleco.service 2>/dev/null || true

echo "[*] Removing service unit and binary..."
rm -f "$HOME/.config/systemd/user/aura-pleco.service"
rm -f "$HOME/.local/bin/aura-pleco"

systemctl --user daemon-reload

echo "========================================================"
echo "[+] Aura Pleco has been removed from your system."
echo "========================================================"
