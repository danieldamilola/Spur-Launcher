[Setup]
AppName=Volt
AppVersion=1.0.0
DefaultDirName={autopf}\Volt
DefaultGroupName=Volt
OutputBaseFilename=Volt-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
UninstallDisplayIcon={app}\Volt.exe

[Files]
Source: "bin\Release\net9.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Volt"; Filename: "{app}\Volt.exe"
Name: "{group}\Uninstall Volt"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Volt"; Filename: "{app}\Volt.exe"

[Run]
Filename: "{app}\Volt.exe"; Description: "Launch Volt"; Flags: nowait postinstall skipifsilent
