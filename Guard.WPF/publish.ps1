# Build normal and portable app executables
dotnet publish -r win-x64 -c Release --p:PublishSingleFile=true --self-contained true -o bin\publish
dotnet publish -r win-x64 -c Release --p:PublishSingleFile=true --self-contained true -p:IsPortable=true -o bin\portable
Move-Item bin\portable\2FAGuard.exe bin\portable\2FAGuard-Portable.exe -Force
# Sign
signtool.exe sign /n "Open Source Developer, Timo Kössler" /t "http://time.certum.pl/" /fd sha256 /d "2FAGuard" /du "https://2faguard.app" .\bin\publish\2FAGuard.exe .\bin\portable\2FAGuard-Portable.exe
# Create installer
Set-Location Installer
Start-Process -NoNewWindow -FilePath "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" -ArgumentList "./installer.iss"
Set-Location ..