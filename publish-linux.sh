#!/bin/bash
# Linux publish script for 2FAGuard

set -e

echo "Building 2FAGuard for Linux..."

# Restore dependencies
dotnet restore Guard.Avalonia/Guard.Avalonia.csproj

# Build in Release mode
dotnet build Guard.Avalonia/Guard.Avalonia.csproj -c Release

# Publish for Linux x64
dotnet publish Guard.Avalonia/Guard.Avalonia.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ./publish/linux-x64

echo "Publish complete! Output: ./publish/linux-x64"
echo "Run with: ./publish/linux-x64/Guard.Avalonia"