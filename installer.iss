[Setup]
AppName=Spur
AppVersion=1.0.0
DefaultDirName={autopf}\Spur
DefaultGroupName=Spur
OutputBaseFilename=Spur-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
UninstallDisplayIcon={app}\Spur.exe

[Files]
Source: "bin\Release\net9.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Spur"; Filename: "{app}\Spur.exe"
Name: "{group}\Uninstall Spur"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Spur"; Filename: "{app}\Spur.exe"

[Run]
Filename: "{app}\Spur.exe"; Description: "Launch Spur"; Flags: nowait postinstall skipifsilent

