; AppGroup 1.6.0 汉化版 安装脚本
; 基于 iandiv/AppGroup (MIT) 源码本地化构建

#define MyAppName "AppGroup"
#define MyAppVersion "1.6.0"
#define MyAppVerName "AppGroup " + MyAppVersion + " 汉化版"
#define MyAppExeName "AppGroup.exe"
#define MySrcDir "D:\Code\MineProject\AppGroupCn\pkg\AppGroup-cn"

[Setup]
AppId={{A6F1D2C4-9B3E-4D7A-8C51-2E6F4B9A7D30}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppVerName}
AppPublisher=IanDiv
AppPublisherURL=https://github.com/iandiv/AppGroup
AppSupportURL=https://github.com/iandiv/AppGroup
AppUpdatesURL=https://github.com/iandiv/AppGroup/releases
DefaultDirName={autopf}\AppGroup
DisableProgramGroupPage=yes
PrivilegesRequired=admin
OutputDir=D:\SoftWare
OutputBaseFilename=AppGroup-1.6.0-cn-setup
SetupIconFile={#MySrcDir}\AppGroup.ico
UninstallDisplayIcon={app}\{#MyAppExeName}
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763

[Languages]
Name: "chinesesimplified"; MessagesFile: "compiler:Languages\ChineseSimplified.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
Source: "{#MySrcDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\AppGroup"; Filename: "{app}\{#MyAppExeName}"
Name: "{autodesktop}\AppGroup"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,AppGroup}"; Flags: nowait postinstall skipifsilent
