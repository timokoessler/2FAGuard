#!/usr/bin/env bash
# Builds the Linux desktop app and CLI as self-contained single-file executables and
# packs them into an installable archive and a portable archive.
# Usage: ./publish-linux.sh [--preview]
set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"
PUBLISH_DIR="$(pwd)"
ROOT_DIR="$(cd .. && pwd)"
RID="linux-x64"
SUFFIX=""

for arg in "$@"; do
    case "$arg" in
        --preview) SUFFIX="-Preview" ;;
        *)
            echo "Unknown option: $arg" >&2
            exit 1
            ;;
    esac
done

if ! command -v dotnet >/dev/null; then
    echo "The .NET SDK is required to build 2FAGuard" >&2
    exit 1
fi

OUT_DIR="$PUBLISH_DIR/bin/$RID"
STAGE_DIR="$OUT_DIR/2FAGuard"
PORTABLE_DIR="$OUT_DIR/2FAGuard-Portable"
rm -rf "$OUT_DIR"
mkdir -p "$STAGE_DIR/icons" "$PORTABLE_DIR/2FAGuard-Data"

PUBLISH_ARGS=(-c Release -r "$RID" --self-contained true
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true)

echo "Building desktop app"
dotnet publish "$ROOT_DIR/Guard.Linux/Guard.Linux.csproj" "${PUBLISH_ARGS[@]}" -o "$OUT_DIR/app"
echo "Building portable desktop app"
dotnet publish "$ROOT_DIR/Guard.Linux/Guard.Linux.csproj" "${PUBLISH_ARGS[@]}" -p:IsPortable=true -o "$OUT_DIR/app-portable"
echo "Building CLI"
dotnet publish "$ROOT_DIR/Guard.CLI/Guard.CLI.csproj" "${PUBLISH_ARGS[@]}" -o "$OUT_DIR/cli"

install -m 755 "$OUT_DIR/app/2fa-guard" "$STAGE_DIR/2fa-guard"
install -m 755 "$OUT_DIR/cli/2fa" "$STAGE_DIR/2fa"
install -m 755 "$ROOT_DIR/linux/install.sh" "$STAGE_DIR/install.sh"
install -m 644 "$ROOT_DIR/linux/2fa-guard.desktop" "$STAGE_DIR/2fa-guard.desktop"
install -m 644 "$ROOT_DIR/linux/app.2faguard.policy" "$STAGE_DIR/app.2faguard.policy"
install -m 644 "$ROOT_DIR/Guard.WPF/Assets/logo.svg" "$STAGE_DIR/icons/2fa-guard.svg"
install -m 644 "$ROOT_DIR/Guard.WPF/Assets/logo-256.png" "$STAGE_DIR/icons/2fa-guard-256.png"
install -m 644 "$ROOT_DIR/Guard.WPF/Assets/logo-512.png" "$STAGE_DIR/icons/2fa-guard-512.png"
install -m 644 "$ROOT_DIR/LICENSE" "$STAGE_DIR/LICENSE"

install -m 755 "$OUT_DIR/app-portable/2fa-guard" "$PORTABLE_DIR/2fa-guard"
install -m 755 "$OUT_DIR/cli/2fa" "$PORTABLE_DIR/2fa"
install -m 644 "$ROOT_DIR/LICENSE" "$PORTABLE_DIR/LICENSE"

rm -rf "$OUT_DIR/app" "$OUT_DIR/app-portable" "$OUT_DIR/cli"

tar -C "$OUT_DIR" -czf "$PUBLISH_DIR/bin/2FAGuard-$RID$SUFFIX.tar.gz" 2FAGuard
tar -C "$OUT_DIR" -czf "$PUBLISH_DIR/bin/2FAGuard-Portable-$RID$SUFFIX.tar.gz" 2FAGuard-Portable

echo "Done:"
echo "  $PUBLISH_DIR/bin/2FAGuard-$RID$SUFFIX.tar.gz"
echo "  $PUBLISH_DIR/bin/2FAGuard-Portable-$RID$SUFFIX.tar.gz"
