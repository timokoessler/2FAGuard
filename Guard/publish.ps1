dotnet publish -r win-x64 -c Release --p:PublishSingleFile=true --self-contained true -o bin\publish
dotnet publish -r win-x64 -c Release --p:PublishSingleFile=true --self-contained true -p:IsPortable=true -o bin\portable
Move-Item bin\portable\2FAGuard.exe bin\portable\2FAGuard-Portable.exe -Force
cd Installer
Start-Process -NoNewWindow -FilePath "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" -ArgumentList "./installer.iss"
cd ..