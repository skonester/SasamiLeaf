; SasamiLeaf_Standalone Installer
; Inno Setup 6 Script - Windows 11 Compatible
; Registers media file associations (mp4, mkv, avi, mov, wmv, flv, webm, etc.)
; Build with Inno Setup 6.x (https://jrsoftware.org/isinfo.php)

#define AppName "SasamiLeaf"
#define AppVersion "2.0"
#define AppPublisher "SasamiLeaf Project"
#define AppURL "https://github.com/SasamiLeaf"
#define AppExeName "SasamiLeaf.exe"
#define AppIcon "SasamLeaf.ico"
#define MediaExts ".mp4|.mkv|.avi|.mov|.wmv|.flv|.webm|.m4v|.mpg|.mpeg|.ts|.m2ts"

[Setup]
AppId={{A1B2C3D4-E5F6-7890-ABCD-EF1234567890}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
AppUpdatesURL={#AppURL}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
AllowNoIcons=yes
OutputDir=.
OutputBaseFilename=SasamiLeaf_Standalone_Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
WizardResizable=no
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
SetupIconFile={#AppIcon}
UninstallDisplayIcon={app}\{#AppIcon}
MinVersion=10.0.17763

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "german"; MessagesFile: "compiler:Languages\German.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startmenu"; Description: "{cm:CreateQuickLaunchIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "fileassoc"; Description: "Associate SasamiLeaf with media files ({#MediaExts})"; GroupDescription: "File associations:"; Flags: checkedonce

[Files]
Source: "{#AppExeName}"; DestDir: "{app}"; Flags: ignoreversion
Source: "{#AppIcon}"; DestDir: "{app}"; Flags: ignoreversion
Source: "FFmpeg\*"; DestDir: "{app}\FFmpeg"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "Plugins\*"; DestDir: "{app}\Plugins"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "D3DCompiler_47_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "PenImc_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "PresentationNative_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "vcruntime140_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "wpfgfx_cor3.dll"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"; IconFilename: "{app}\{#AppIcon}"; WorkingDir: "{app}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"; IconFilename: "{sys}\shell32.dll"; IconIndex: 31
Name: "{commondesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon; IconFilename: "{app}\{#AppIcon}"; WorkingDir: "{app}"
Name: "{userappdata}\Microsoft\Windows\Start Menu\Programs\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: startmenu; IconFilename: "{app}\{#AppIcon}"; WorkingDir: "{app}"

[Registry]
Root: HKCR; Subkey: "SasamiLeaf.MediaFile"; ValueType: string; ValueName: ""; ValueData: "SasamiLeaf Media File"; Flags: uninsdeletekey; Tasks: fileassoc
Root: HKCR; Subkey: "SasamiLeaf.MediaFile"; ValueType: string; ValueName: "FriendlyTypeName"; ValueData: "SasamiLeaf Media File"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: "SasamiLeaf.MediaFile"; ValueType: string; ValueName: "PerceivedType"; ValueData: "video"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: "SasamiLeaf.MediaFile\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\{#AppIcon},0"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: "SasamiLeaf.MediaFile\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExeName}"" ""%1"""; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: "SasamiLeaf.MediaFile\shell\open"; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "{#AppName}"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: "SasamiLeaf.MediaFile\shell\play\command"; ValueType: string; ValueName: ""; ValueData: """{app}\{#AppExeName}"" ""%1"""; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: "SasamiLeaf.MediaFile\SupportedTypes"; ValueType: string; ValueName: ""; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc

; Per-extension associations (explicit, no macros)
Root: HKCR; Subkey: ".mp4"; ValueType: string; ValueName: ""; ValueData: "SasamiLeaf.MediaFile"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".mp4\OpenWithProgids"; ValueType: string; ValueName: "SasamiLeaf.MediaFile"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".mkv"; ValueType: string; ValueName: ""; ValueData: "SasamiLeaf.MediaFile"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".mkv\OpenWithProgids"; ValueType: string; ValueName: "SasamiLeaf.MediaFile"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".avi"; ValueType: string; ValueName: ""; ValueData: "SasamiLeaf.MediaFile"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".avi\OpenWithProgids"; ValueType: string; ValueName: "SasamiLeaf.MediaFile"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".mov"; ValueType: string; ValueName: ""; ValueData: "SasamiLeaf.MediaFile"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".mov\OpenWithProgids"; ValueType: string; ValueName: "SasamiLeaf.MediaFile"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".wmv"; ValueType: string; ValueName: ""; ValueData: "SasamiLeaf.MediaFile"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".wmv\OpenWithProgids"; ValueType: string; ValueName: "SasamiLeaf.MediaFile"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".flv"; ValueType: string; ValueName: ""; ValueData: "SasamiLeaf.MediaFile"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".flv\OpenWithProgids"; ValueType: string; ValueName: "SasamiLeaf.MediaFile"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".webm"; ValueType: string; ValueName: ""; ValueData: "SasamiLeaf.MediaFile"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".webm\OpenWithProgids"; ValueType: string; ValueName: "SasamiLeaf.MediaFile"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".m4v"; ValueType: string; ValueName: ""; ValueData: "SasamiLeaf.MediaFile"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".m4v\OpenWithProgids"; ValueType: string; ValueName: "SasamiLeaf.MediaFile"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".mpg"; ValueType: string; ValueName: ""; ValueData: "SasamiLeaf.MediaFile"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".mpg\OpenWithProgids"; ValueType: string; ValueName: "SasamiLeaf.MediaFile"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".mpeg"; ValueType: string; ValueName: ""; ValueData: "SasamiLeaf.MediaFile"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".mpeg\OpenWithProgids"; ValueType: string; ValueName: "SasamiLeaf.MediaFile"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".ts"; ValueType: string; ValueName: ""; ValueData: "SasamiLeaf.MediaFile"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".ts\OpenWithProgids"; ValueType: string; ValueName: "SasamiLeaf.MediaFile"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".m2ts"; ValueType: string; ValueName: ""; ValueData: "SasamiLeaf.MediaFile"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKCR; Subkey: ".m2ts\OpenWithProgids"; ValueType: string; ValueName: "SasamiLeaf.MediaFile"; ValueData: ""; Flags: uninsdeletevalue; Tasks: fileassoc

; Windows 11 Default Apps registration
Root: HKLM; Subkey: "Software\RegisteredApplications"; ValueType: string; ValueName: "SasamiLeaf"; ValueData: "Software\SasamiLeaf\Capabilities"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKLM; Subkey: "Software\SasamiLeaf\Capabilities"; ValueType: string; ValueName: "ApplicationName"; ValueData: "{#AppName}"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKLM; Subkey: "Software\SasamiLeaf\Capabilities"; ValueType: string; ValueName: "ApplicationDescription"; ValueData: "SasamiLeaf Media Player"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKLM; Subkey: "Software\SasamiLeaf\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mp4"; ValueData: "SasamiLeaf.MediaFile"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKLM; Subkey: "Software\SasamiLeaf\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mkv"; ValueData: "SasamiLeaf.MediaFile"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKLM; Subkey: "Software\SasamiLeaf\Capabilities\FileAssociations"; ValueType: string; ValueName: ".avi"; ValueData: "SasamiLeaf.MediaFile"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKLM; Subkey: "Software\SasamiLeaf\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mov"; ValueData: "SasamiLeaf.MediaFile"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKLM; Subkey: "Software\SasamiLeaf\Capabilities\FileAssociations"; ValueType: string; ValueName: ".wmv"; ValueData: "SasamiLeaf.MediaFile"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKLM; Subkey: "Software\SasamiLeaf\Capabilities\FileAssociations"; ValueType: string; ValueName: ".flv"; ValueData: "SasamiLeaf.MediaFile"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKLM; Subkey: "Software\SasamiLeaf\Capabilities\FileAssociations"; ValueType: string; ValueName: ".webm"; ValueData: "SasamiLeaf.MediaFile"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKLM; Subkey: "Software\SasamiLeaf\Capabilities\FileAssociations"; ValueType: string; ValueName: ".m4v"; ValueData: "SasamiLeaf.MediaFile"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKLM; Subkey: "Software\SasamiLeaf\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mpg"; ValueData: "SasamiLeaf.MediaFile"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKLM; Subkey: "Software\SasamiLeaf\Capabilities\FileAssociations"; ValueType: string; ValueName: ".mpeg"; ValueData: "SasamiLeaf.MediaFile"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKLM; Subkey: "Software\SasamiLeaf\Capabilities\FileAssociations"; ValueType: string; ValueName: ".ts"; ValueData: "SasamiLeaf.MediaFile"; Flags: uninsdeletevalue; Tasks: fileassoc
Root: HKLM; Subkey: "Software\SasamiLeaf\Capabilities\FileAssociations"; ValueType: string; ValueName: ".m2ts"; ValueData: "SasamiLeaf.MediaFile"; Flags: uninsdeletevalue; Tasks: fileassoc

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[Code]
const
  SHCNE_ASSOCCHANGED = $08000000;
  SHCNF_IDLIST = $0000;

procedure ShellChangeNotify(wEventId: Integer; uFlags: Integer; dwItem1: Integer; dwItem2: Integer);
external 'SHChangeNotify@Shell32.dll stdcall';

procedure RefreshShellIcons;
begin
  ShellChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, 0, 0);
end;

function InitializeSetup: Boolean;
begin
  Result := True;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if CurStep = ssPostInstall then
  begin
    // Notify Windows Explorer that file associations changed so
    // Windows 11 picks up the new defaults immediately
    RefreshShellIcons;
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
    RefreshShellIcons;
end;
