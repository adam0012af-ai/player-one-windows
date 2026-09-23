#define MyAppName "Player One"
#define MyAppVersion "1.1.0"
#define MyAppExeName "PlayerOne.exe"
[Setup]
AppId={{B29E7898-0F74-4C2B-98EF-4D132C819026}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
DefaultDirName={autopf}\Player One
DefaultGroupName=Player One
OutputDir=..\artifacts\installer
OutputBaseFilename=Player-One-Setup
SetupIconFile=..\src\PlayerOne.Windows\Assets\player-one.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
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