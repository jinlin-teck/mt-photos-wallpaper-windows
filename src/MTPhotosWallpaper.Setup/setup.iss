; MT-Photos Wallpaper Setup Script
; Inno Setup Script

[Setup]
AppName=MT-Photos Wallpaper
AppVersion=1.0.0
AppPublisher=MT-Photos Wallpaper Team
DefaultDirName={localappdata}\MT-Photos Wallpaper
DefaultGroupName=MT-Photos Wallpaper
AllowNoIcons=yes
OutputBaseFilename=mt-photos-wallpaper-setup
Compression=lzma
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64
ArchitecturesInstallIn64BitMode=x64

[Files]
Source: "..\MTPhotosWallpaper.App\bin\Release\net8.0-windows\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\MT-Photos Wallpaper"; Filename: "{app}\MTPhotosWallpaper.exe"
Name: "{commondesktop}\MT-Photos Wallpaper"; Filename: "{app}\MTPhotosWallpaper.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"

[Run]
Filename: "{app}\MTPhotosWallpaper.exe"; Description: "Launch MT-Photos Wallpaper"; Flags: nowait postinstall skipifsilent
