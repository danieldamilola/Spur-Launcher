[Setup]
AppName=Arc
AppVersion=1.0.0
DefaultDirName={autopf}\Arc
DefaultGroupName=Arc
OutputBaseFilename=Arc-Setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
UninstallDisplayIcon={app}\Arc.exe

[Files]
Source: "bin\Release\net9.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Arc"; Filename: "{app}\Arc.exe"
Name: "{group}\Uninstall Arc"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Arc"; Filename: "{app}\Arc.exe"

[Run]
Filename: "{app}\Arc.exe"; Description: "Launch Arc"; Flags: nowait postinstall skipifsilent

