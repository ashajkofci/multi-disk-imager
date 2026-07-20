#ifndef AppVersion
  #error AppVersion must be provided by the release workflow
#endif
#ifndef NumericVersion
  #error NumericVersion must be provided by the release workflow
#endif
#ifndef SourceExe
  #error SourceExe must be provided by the release workflow
#endif
#ifndef OutputDir
  #error OutputDir must be provided by the release workflow
#endif
#ifndef IconFile
  #error IconFile must be provided by the release workflow
#endif

#define AppName "bNovate Multi Disk Imager"
#define AppPublisher "bNovate Technologies SA"
#define AppExeName "MultiDiskImager.exe"

[Setup]
AppId={{E54120FD-B4EE-4F92-AED0-EC86CF46864D}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL=https://www.bnovate.com
AppSupportURL=https://github.com/ashajkofci/multi-disk-imager/issues
AppUpdatesURL=https://github.com/ashajkofci/multi-disk-imager/releases
DefaultDirName={autopf}\bNovate\Multi Disk Imager
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
OutputDir={#OutputDir}
OutputBaseFilename=bnovate-multi-disk-imager-{#AppVersion}-windows-x64-setup
SetupIconFile={#IconFile}
UninstallDisplayIcon={app}\{#AppExeName}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
CloseApplications=yes
RestartApplications=no
VersionInfoVersion={#NumericVersion}
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} Setup
VersionInfoProductName={#AppName}
VersionInfoProductVersion={#AppVersion}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
Source: "{#SourceExe}"; DestDir: "{app}"; DestName: "{#AppExeName}"; Flags: ignoreversion

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent runasoriginaluser
