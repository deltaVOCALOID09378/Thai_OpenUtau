[Setup]
AppName=OptiLink Diffsinger to VCCV Converter
AppVersion=1.4
DefaultDirName={autopf}\OptiLink_Diffsinger_VCCV
DefaultGroupName=OptiLink System
OutputDir=A:\Thai OpenUtau by DELTA SYNTH\OpenUtau.Plugin.Builtin\Dictionary\Installer
OutputBaseFilename=Diffsinger_Converter_Setup_v1.4
Compression=lzma
SolidCompression=yes
SetupIconFile=compiler:SetupClassicIcon.ico
UninstallDisplayIcon={app}\Diffsinger_Converter.exe

[Files]
Source: "A:\Thai OpenUtau by DELTA SYNTH\OpenUtau.Plugin.Builtin\Dictionary\dist\Diffsinger_Converter\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "A:\Thai OpenUtau by DELTA SYNTH\OpenUtau.Plugin.Builtin\Dictionary\words_th_dict.txt"; DestDir: "{app}"; Flags: ignoreversion
Source: "A:\Thai OpenUtau by DELTA SYNTH\OpenUtau.Plugin.Builtin\Dictionary\g2p_th_thai_diffsinger.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\OptiLink Diffsinger Converter"; Filename: "{app}\Diffsinger_Converter.exe"
Name: "{autodesktop}\OptiLink Diffsinger Converter"; Filename: "{app}\Diffsinger_Converter.exe"
