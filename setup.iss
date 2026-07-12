[Setup]
AppName=Spur
AppVersion=1.0
DefaultDirName={pf}\Spur
DefaultGroupName=Spur
OutputBaseFilename=SpurInstaller
Compression=lzma2
SolidCompression=yes
OutputDir=Output
ArchitecturesInstallIn64BitMode=x64

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop icon"; GroupDescription: "Additional icons:"; Flags: unchecked

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Spur"; Filename: "{app}\Spur.exe"
Name: "{commondesktop}\Spur"; Filename: "{app}\Spur.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Spur.exe"; Description: "Launch Spur"; Flags: nowait postinstall skipifsilent
