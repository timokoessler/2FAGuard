dotnet publish -p:PublishProfile=FolderProfile
Start-Process -NoNewWindow -FilePath "C:\Program Files (x86)\Inno Setup 6\ISCC.exe" -ArgumentList "./installer.iss"
dotnet publish -p:PublishProfile=Portable -p:IsPortable=true
