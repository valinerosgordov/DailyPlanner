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

[Code]
function GetUninstallString(): String;
var
  sUnInstPath: String;
  sUnInstallString: String;
begin
  sUnInstPath := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#SetupSetting("AppId")}_is1';
  sUnInstallString := '';
  if not RegQueryStringValue(HKLM, sUnInstPath, 'UninstallString', sUnInstallString) then
    RegQueryStringValue(HKCU, sUnInstPath, 'UninstallString', sUnInstallString);
  Result := sUnInstallString;
end;

function IsUpgrade(): Boolean;
begin
  Result := (GetUninstallString() <> '');
end;

function UninstallOldVersion(): Integer;
var
  sUnInstallString: String;
  iResultCode: Integer;
begin
  Result := 0;
  sUnInstallString := GetUninstallString();
  if sUnInstallString <> '' then begin
    sUnInstallString := RemoveQuotes(sUnInstallString);
    if Exec(sUnInstallString, '/SILENT /NORESTART /SUPPRESSMSGBOXES', '', SW_HIDE, ewWaitUntilTerminated, iResultCode) then
      Result := 3
    else
      Result := 2;
  end else
    Result := 1;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssInstall) then begin
    if (IsUpgrade()) then
      UninstallOldVersion();
  end;
end;

function InitializeSetup(): Boolean;
begin
  Result := True;
  if IsUpgrade() then begin
    if MsgBox('Daily Planner уже установлен на этом компьютере.' + #13#10 + #13#10 +
              'Удалить старую версию и установить новую?' + #13#10 +
              '(Ваши данные сохранятся)',
              mbConfirmation, MB_YESNO) = IDNO then
      Result := False;
  end;
end;
