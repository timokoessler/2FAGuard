#!/usr/bin/env bash
# Installs 2FAGuard for the current user (default) or system-wide (--system, requires root).
# Usage: ./install.sh [--system] [--uninstall]
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MODE="user"
ACTION="install"

for arg in "$@"; do
    case "$arg" in
        --system) MODE="system" ;;
        --uninstall) ACTION="uninstall" ;;
        -h|--help)
            echo "Usage: $0 [--system] [--uninstall]"
            exit 0
            ;;
        *)
            echo "Unknown option: $arg" >&2
            exit 1
            ;;
    esac
done

if [ "$MODE" = "system" ]; then
    if [ "$(id -u)" -ne 0 ]; then
        echo "System-wide installation requires root, run it with sudo" >&2
        exit 1
    fi
    APP_DIR="/opt/2fa-guard"
    BIN_DIR="/usr/local/bin"
    SHARE_DIR="/usr/share"
else
    APP_DIR="$HOME/.local/lib/2fa-guard"
    BIN_DIR="$HOME/.local/bin"
    SHARE_DIR="${XDG_DATA_HOME:-$HOME/.local/share}"
fi
POLKIT_FILE="/usr/share/polkit-1/actions/app.2faguard.policy"

refresh_caches() {
    command -v update-desktop-database >/dev/null && update-desktop-database "$SHARE_DIR/applications" 2>/dev/null || true
    command -v gtk-update-icon-cache >/dev/null && gtk-update-icon-cache -q -t "$SHARE_DIR/icons/hicolor" 2>/dev/null || true
}

if [ "$ACTION" = "uninstall" ]; then
    rm -rf "$APP_DIR"
    rm -f "$BIN_DIR/2fa-guard" "$BIN_DIR/2fa"
    rm -f "$SHARE_DIR/applications/2fa-guard.desktop"
    rm -f "$SHARE_DIR/icons/hicolor/scalable/apps/2fa-guard.svg"
    for size in 256 512; do
        rm -f "$SHARE_DIR/icons/hicolor/${size}x${size}/apps/2fa-guard.png"
    done
    if [ "$MODE" = "system" ]; then
        rm -f "$POLKIT_FILE"
    fi
    refresh_caches
    echo "2FAGuard has been uninstalled. Your tokens in ~/.local/share/2FAGuard were not deleted."
    exit 0
fi

for file in 2fa-guard 2fa; do
    if [ ! -f "$SCRIPT_DIR/$file" ]; then
        echo "Missing $file next to the install script" >&2
        exit 1
    fi
done

mkdir -p "$APP_DIR" "$BIN_DIR" "$SHARE_DIR/applications" \
    "$SHARE_DIR/icons/hicolor/scalable/apps" \
    "$SHARE_DIR/icons/hicolor/256x256/apps" \
    "$SHARE_DIR/icons/hicolor/512x512/apps"

install -m 755 "$SCRIPT_DIR/2fa-guard" "$APP_DIR/2fa-guard"
install -m 755 "$SCRIPT_DIR/2fa" "$APP_DIR/2fa"
ln -sf "$APP_DIR/2fa-guard" "$BIN_DIR/2fa-guard"
ln -sf "$APP_DIR/2fa" "$BIN_DIR/2fa"

install -m 644 "$SCRIPT_DIR/icons/2fa-guard.svg" "$SHARE_DIR/icons/hicolor/scalable/apps/2fa-guard.svg"
for size in 256 512; do
    install -m 644 "$SCRIPT_DIR/icons/2fa-guard-$size.png" "$SHARE_DIR/icons/hicolor/${size}x${size}/apps/2fa-guard.png"
done

sed "s|^Exec=.*|Exec=\"$APP_DIR/2fa-guard\"|" "$SCRIPT_DIR/2fa-guard.desktop" \
    > "$SHARE_DIR/applications/2fa-guard.desktop"
chmod 644 "$SHARE_DIR/applications/2fa-guard.desktop"

if [ "$MODE" = "system" ]; then
    install -m 644 "$SCRIPT_DIR/app.2faguard.policy" "$POLKIT_FILE"
fi

refresh_caches

echo "2FAGuard has been installed to $APP_DIR"
if [ "$MODE" = "user" ]; then
    case ":$PATH:" in
        *":$BIN_DIR:"*) ;;
        *) echo "Note: add $BIN_DIR to your PATH to use the 2fa command from the terminal." ;;
    esac
    if [ ! -f "$POLKIT_FILE" ]; then
        echo "Optional: to require your system password or fingerprint when unlocking with the keyring, run:"
        echo "  sudo install -m 644 \"$SCRIPT_DIR/app.2faguard.policy\" $POLKIT_FILE"
    fi
fi
