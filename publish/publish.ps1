# Script to build the WPF and CLI app, sign the executables and build the installer

function Confirm-Requirements {
    # Check if current directory is the publish directory
    if (-not (Test-Path .\publish.ps1)) {
        Write-Host "Please run the script from the publish directory"
        Exit
    }
    # Check if Inno Setup 6 is installed
    if (-not (Test-Path "C:\Program Files (x86)\Inno Setup 6\ISCC.exe")) {
        Write-Host "Inno Setup 6 is required to build the installer"
        Exit
    }
    # Check if the command sign tool is available
    if (-not (Get-Command "signtool.exe" -errorAction SilentlyContinue)) {
        Write-Host "SignTool is required to sign the executables"
        Exit
    }
    # Check if the command dotnet is available
    if (-not (Get-Command "dotnet" -errorAction SilentlyContinue)) {
        Write-Host "Dotnet is required to build the executables"
        Exit
    }
    # Create required directories
    if (-not (Test-Path .\bin)) {
        New-Item -ItemType Directory -Path .\bin
    }
    if (-not (Test-Path .\bin\standalone)) {
        New-Item -ItemType Directory -Path .\bin\standalone
    }
    if (-not (Test-Path .\bin\portable)) {
        New-Item -ItemType Directory -Path .\bin\portable
    }
}

function Invoke-Tests {
    Set-Location ../Guard.Test
    dotnet test
    Set-Location ../publish
}
function Build-WPF-App {
    Set-Location ../Guard.WPF
    dotnet publish -r win-x64 -c Release --p:PublishSingleFile=true --self-contained true -o bin\publish
    Move-Item bin\publish\2FAGuard.exe ..\publish\bin\standalone -Force
    dotnet publish -r win-x64 -c Release --p:PublishSingleFile=true --self-contained true -p:IsPortable=true -o bin\portable
    Move-Item bin\portable\2FAGuard.exe ..\publish\bin\portable\2FAGuard-Portable.exe -Force
    Set-Location ../publish
}

function Build-CLI-App {
    Set-Location ../Guard.CLI
    dotnet publish -r win-x64 -c Release --p:PublishSingleFile=true --self-contained true -o bin\publish
    Move-Item bin\publish\2fa.exe ..\publish\bin\standalone -Force
    dotnet publish -r win-x64 -c Release --p:PublishSingleFile=true --self-contained true -p:IsPortable=true -o bin\portable
    Move-Item bin\portable\2fa.exe ..\publish\bin\portable -Force
    Set-Location ../publish
}

function Invoke-Code-Signing {
    signtool.exe sign /n "Open Source Developer, Timo Kössler" /t "http://time.certum.pl/" /fd sha256 /d "2FAGuard" /du "https://2faguard.app" .\bin\standalone\2FAGuard.exe .\bin\portable\2FAGuard-Portable.exe .\bin\standalone\2fa.exe .\bin\portable\2fa.exe
}

function Build-Installer {
    Start-Process -NoNewWindow -FilePath "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" -ArgumentList "./installer.iss"
}


Write-Host "Checking Requirements"
Confirm-Requirements
Write-Host "Running Tests"
Invoke-Tests
Write-Host "Building WPF App"
Build-WPF-App
Write-Host "Building CLI App"
Build-CLI-App
Write-Host "Signing Code"
Invoke-Code-Signing
Write-Host "Building Installer"
Build-Installer
Write-Host "Done"