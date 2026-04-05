#define MyAppName "Password Vault"
#define MyAppExeName "blazorApp.exe"
#define MyAppPublisher "Password Vault"
#ifndef AppVersion
  #define AppVersion "1.0.0"
#endif
#define PublishDir "..\blazor\blazorApp\blazorApp\bin\Release\net10.0-windows10.0.19041.0\win-x64\publish"
#define OutputDir "..\blazor\blazorApp\blazorApp\bin\Release\Installer"

[Setup]
AppId={{A2995250-0C63-4D3E-A21D-345C1A4B0EE1}
AppName={#MyAppName}
AppVersion={#AppVersion}
AppVerName={#MyAppName} {#AppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
UninstallDisplayIcon={app}\appicon.ico
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
ChangesAssociations=no
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=PasswordVaultSetup_{#AppVersion}_x64
SetupIconFile={#PublishDir}\appicon.ico

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\appicon.ico"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; IconFilename: "{app}\appicon.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent
