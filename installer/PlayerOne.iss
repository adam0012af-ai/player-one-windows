#define MyAppName "Player One"
#define MyAppVersion "1.0.0"
#define MyAppExeName "PlayerOne.exe"
[Setup]
AppId={{A4B7A45D-9A9C-49F5-A201-PLAYERONE2026}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\Player One
DefaultGroupName=Player One
OutputDir=..\artifacts\installer
OutputBaseFilename=Player-One-Setup
Compression=lzma2
SolidCompression=yes
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
[Files]
Source: "..\artifacts\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
[Icons]
Name: "{autoprograms}\Player One"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\Player One"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional icons:"
[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch Player One"; Flags: nowait postinstall skipifsilent
