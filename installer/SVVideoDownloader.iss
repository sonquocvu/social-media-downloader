#ifndef ProductVersion
  #error ProductVersion must be supplied by build-installer.ps1.
#endif
#ifndef VersionMajor
  #error VersionMajor must be supplied by build-installer.ps1.
#endif
#ifndef VersionMinor
  #error VersionMinor must be supplied by build-installer.ps1.
#endif
#ifndef VersionPatch
  #error VersionPatch must be supplied by build-installer.ps1.
#endif
#ifndef RepositoryRoot
  #error RepositoryRoot must be supplied by build-installer.ps1.
#endif
#ifndef PublishDir
  #error PublishDir must be supplied by build-installer.ps1.
#endif
#ifndef OutputDir
  #error OutputDir must be supplied by build-installer.ps1.
#endif

#define AppName "SV Video Downloader"
#define AppExecutable "SVVideoDownloader.App.exe"
#define AppId "{{881F3DA4-6E82-44F2-8A1F-87F949B0B2E7}"
#define AppUninstallGuid "{881F3DA4-6E82-44F2-8A1F-87F949B0B2E7}"
#define LegacyMsiUpgradeCode "{75798F98-7518-4F21-9BFB-338F9BF16BF1}"
#define LegacyMsi100ProductCode "{00B76B32-4C7B-4F8A-A39F-2DAE5923074A}"
#define LegacyMsi110ProductCode "{CDE18BC0-AC01-433B-930B-B1984121789F}"

[Setup]
AppId={#AppId}
AppName={#AppName}
AppVersion={#ProductVersion}
AppVerName={#AppName} {#ProductVersion}
AppPublisher=SVVideoDownloader
AppCopyright=Copyright © 2026 SVVideoDownloader
VersionInfoCompany=SVVideoDownloader
VersionInfoCopyright=Copyright © 2026 SVVideoDownloader
VersionInfoDescription=Trình cài đặt {#AppName}
VersionInfoOriginalFileName=SVVideoDownloader-{#ProductVersion}-win-x64-setup.exe
VersionInfoProductName={#AppName}
VersionInfoProductTextVersion={#ProductVersion}
VersionInfoProductVersion={#ProductVersion}.0
VersionInfoTextVersion={#ProductVersion}
VersionInfoVersion={#ProductVersion}.0
DefaultDirName={autopf}\SVVideoDownloader
DefaultGroupName={#AppName}
DisableProgramGroupPage=auto
PrivilegesRequired=admin
SetupArchitecture=x64
ArchitecturesAllowed=x64compatible
OutputDir={#OutputDir}
OutputBaseFilename=SVVideoDownloader-{#ProductVersion}-win-x64-setup
SetupIconFile={#RepositoryRoot}\src\SVVideoDownloader.App\Assets\SVVideoDownloader.ico
UninstallDisplayIcon={app}\{#AppExecutable}
UninstallDisplayName={#AppName}
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern dynamic windows11 includetitlebar
WizardSizePercent=110,110
WizardResizable=no
WizardImageFile={#RepositoryRoot}\installer\Assets\setup-sidebar.png
WizardImageFileDynamicDark={#RepositoryRoot}\installer\Assets\setup-sidebar.png
WizardImageBackColor=#080c21
WizardImageBackColorDynamicDark=#080c21
WizardSmallImageFile={#RepositoryRoot}\src\SVVideoDownloader.App\Assets\SVVideoDownloader.png
WizardSmallImageFileDynamicDark={#RepositoryRoot}\src\SVVideoDownloader.App\Assets\SVVideoDownloader.png
WizardSmallImageBackColor=none
WizardSmallImageBackColorDynamicDark=none
CloseApplications=yes
CloseApplicationsFilter={#AppExecutable}
RestartApplications=no
UsePreviousAppDir=yes
UsePreviousGroup=yes
UsePreviousLanguage=yes
UsePreviousPrivileges=yes
UsePreviousTasks=yes
DisableWelcomePage=no
DisableReadyPage=no
AllowNoIcons=yes
SetupLogging=yes

[Languages]
Name: "vietnamese"; MessagesFile: "{#RepositoryRoot}\installer\Languages\Vietnamese.isl"

[CustomMessages]
vietnamese.CreateDesktopShortcut=Tạo biểu tượng trên màn hình nền
vietnamese.LaunchApplication=Khởi chạy SV Video Downloader

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopShortcut}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: checkedonce

[Files]
Source: "{#PublishDir}\{#AppExecutable}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExecutable}"; WorkingDir: "{app}"; Comment: "Tải video công khai được phép"
Name: "{commondesktop}\{#AppName}"; Filename: "{app}\{#AppExecutable}"; WorkingDir: "{app}"; Comment: "Tải video công khai được phép"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExecutable}"; Description: "{cm:LaunchApplication}"; WorkingDir: "{app}"; Flags: nowait postinstall skipifsilent runasoriginaluser

[Code]
const
  LegacyMsiUpgradeCode = '{#LegacyMsiUpgradeCode}';
  LegacyMsi100ProductCode = '{#LegacyMsi100ProductCode}';
  LegacyMsi110ProductCode = '{#LegacyMsi110ProductCode}';
  InnoUninstallRegistryKey =
    'Software\Microsoft\Windows\CurrentVersion\Uninstall\{#AppUninstallGuid}_is1';
  ErrorUnknownProduct = 1605;
  ErrorSuccessRebootRequired = 3010;

function CurrentPackedVersion: Int64;
begin
  Result := PackVersionComponents(
    {#VersionMajor}, {#VersionMinor}, {#VersionPatch}, 0);
end;

function NewerInnoVersionInstalled: Boolean;
var
  InstalledVersion: String;
  InstalledPackedVersion: Int64;
begin
  Result := False;
  if RegQueryStringValue(
       HKLM64, InnoUninstallRegistryKey, 'DisplayVersion', InstalledVersion) and
     StrToVersion(InstalledVersion, InstalledPackedVersion) then
  begin
    Result :=
      ComparePackedVersion(InstalledPackedVersion, CurrentPackedVersion) > 0;
  end;
end;

function InitializeSetup: Boolean;
begin
  Result := False;

  if NewerInnoVersionInstalled or
     IsMsiProductInstalled(
       LegacyMsiUpgradeCode, CurrentPackedVersion + 1) then
  begin
    MsgBox(
      'Máy tính đã cài đặt phiên bản SV Video Downloader mới hơn. ' +
      'Không thể cài phiên bản {#ProductVersion}.',
      mbError, MB_OK);
    Exit;
  end;

  Result := True;
end;

function RemoveLegacyMsiProduct(
  const ProductCode, DisplayVersion: String): String;
var
  ResultCode: Integer;
  MsiExecPath: String;
  Parameters: String;
begin
  Result := '';
  MsiExecPath := ExpandConstant('{sys}\msiexec.exe');
  Parameters := '/x ' + AddQuotes(ProductCode) + ' /qn /norestart';

  Log('Đang gỡ gói MSI cũ ' + DisplayVersion + ' trước khi chuyển sang Inno Setup.');
  if not Exec(
       MsiExecPath, Parameters, '', SW_HIDE, ewWaitUntilTerminated, ResultCode) then
  begin
    Result :=
      'Không thể khởi chạy Windows Installer để gỡ phiên bản ' +
      DisplayVersion + '.';
    Exit;
  end;

  if (ResultCode = 0) or (ResultCode = ErrorUnknownProduct) then
  begin
    Exit;
  end;

  if ResultCode = ErrorSuccessRebootRequired then
  begin
    Result :=
      'Windows cần khởi động lại để hoàn tất gỡ phiên bản ' +
      DisplayVersion + '. Hãy khởi động lại máy rồi chạy lại trình cài đặt.';
    Exit;
  end;

  Result :=
    'Không thể gỡ phiên bản ' + DisplayVersion +
    ' (mã Windows Installer ' + IntToStr(ResultCode) + ').';
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
begin
  Result := '';
  if not IsMsiProductInstalled(LegacyMsiUpgradeCode, 0) then
  begin
    Exit;
  end;

  Result := RemoveLegacyMsiProduct(LegacyMsi100ProductCode, '1.0.0');
  if Result <> '' then
  begin
    Exit;
  end;

  Result := RemoveLegacyMsiProduct(LegacyMsi110ProductCode, '1.1.0');
  if Result <> '' then
  begin
    Exit;
  end;

  if IsMsiProductInstalled(LegacyMsiUpgradeCode, 0) then
  begin
    Result :=
      'Máy tính còn một gói MSI SV Video Downloader cũ không được nhận diện. ' +
      'Hãy gỡ gói đó trong Cài đặt Windows rồi chạy lại trình cài đặt.';
  end;
end;
