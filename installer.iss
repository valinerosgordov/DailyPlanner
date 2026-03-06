[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}}
AppName=Daily Planner
AppVersion=2.1.1
AppPublisher=DailyPlanner
DefaultDirName={autopf}\DailyPlanner
DefaultGroupName=Daily Planner
UninstallDisplayName=Daily Planner
UninstallDisplayIcon={app}\DailyPlanner.exe
SetupIconFile=DailyPlanner\planner.ico
OutputDir=installer_output
OutputBaseFilename=DailyPlanner_Setup
Compression=lzma2/ultra64
SolidCompression=yes
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
DisableProgramGroupPage=yes

[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Files]
Source: "publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Daily Planner"; Filename: "{app}\DailyPlanner.exe"; IconFilename: "{app}\DailyPlanner.exe"
Name: "{group}\{cm:UninstallProgram,Daily Planner}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\Daily Planner"; Filename: "{app}\DailyPlanner.exe"; IconFilename: "{app}\DailyPlanner.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\DailyPlanner.exe"; Description: "{cm:LaunchProgram,Daily Planner}"; Flags: nowait postinstall skipifsilent
