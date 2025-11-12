#define MyAppName "2FAGuard"
#define MyAppVersion "1.6.3"
#define MyAppPublisher "Timo Kössler"
#define MyAppURL "https://2faguard.app"

#define PreviewEnv GetEnv("PREVIEW")
#if PreviewEnv != ""
  #define MyAppExeName "2FAGuard-Preview.exe"
  #define OutputFileName "2FAGuard-Installer-Preview"
#else
  #define MyAppExeName "2FAGuard.exe"
  #define OutputFileName "2FAGuard-Installer"
#endif

#include "CodeDependencies.iss"

[Setup]
AppId={{E975C7D9-79F6-47D9-9597-014331FC3C0F}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} {#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DisableDirPage=yes
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=.\bin\installer
OutputBaseFilename={#OutputFileName}
SetupIconFile=..\Guard.WPF\totp.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern dynamic windows11
ArchitecturesAllowed=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
AppCopyright=Timo Kössler and Open Source Contributors
MinVersion=10.0.17763
ShowLanguageDialog=auto
DisableReadyPage=yes
UsePreviousTasks=yes
DisableFinishedPage=yes
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesInstallIn64BitMode=x64compatible
SignTool=mysigntool sign /sha1 0839626A858F4D2E44EDC99708362609E432DA5A /t $qhttp://time.certum.pl/$q /fd sha256 /d $q2FAGuard Installer$q /du $qhttps://2faguard.app$q $f
SignedUninstaller=yes
SignToolRetryCount=0

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "italian"; MessagesFile: "compiler:Languages\Italian.isl"
Name: "arabic"; MessagesFile: "compiler:Languages\Arabic.isl"
Name: "japanese"; MessagesFile: "compiler:Languages\Japanese.isl"
Name: "czech"; MessagesFile: "compiler:Languages\Czech.isl"
Name: "korean"; MessagesFile: "compiler:Languages\Korean.isl"
Name: "polish"; MessagesFile: "compiler:Languages\Polish.isl"
// Unofficial translations
// Download: https://jrsoftware.org/files/istrans/
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "chinesetraditional"; MessagesFile: "compiler:Languages\ChineseTraditional.isl"
Name: "greek"; MessagesFile: "compiler:Languages\Greek.isl"
Name: "brazilianportuguese"; MessagesFile: "compiler:Languages\BrazilianPortuguese.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}";

[Files]
Source: ".\bin\{#MyAppExeName}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall; Parameters: setup; Check: LaunchChecked

[Code]
function InitializeSetup: Boolean;
begin
  Dependency_AddVC2015To2022;
  Result := True;
end;
procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpSelectTasks then
    WizardForm.NextButton.Caption := SetupMessage(msgButtonInstall)
  else
    WizardForm.NextButton.Caption := SetupMessage(msgButtonNext);
end;
function CmdLineParamExists(const Value: string): Boolean;
var
  I: Integer;  
begin
  Result := False;
  for I := 1 to ParamCount do
    if CompareText(ParamStr(I), Value) = 0 then
    begin
      Result := True;
      Exit;
    end;
end;

function LaunchChecked: Boolean;
begin
  Result := not CmdLineParamExists('/NOSTART');
end;