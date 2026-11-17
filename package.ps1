$ErrorActionPreference='Stop'
$guardianRoot=$PSScriptRoot
$guardianApp=Join-Path $guardianRoot 'outputs/FlightOLEDGuardian-v0.3'
if(-not(Test-Path -LiteralPath (Join-Path $guardianApp 'FlightOLEDGuardian.App.exe'))){throw 'Run build.ps1 -Publish first'}
Copy-Item -LiteralPath (Join-Path $guardianRoot 'README.md'),(Join-Path $guardianRoot 'LICENSE') -Destination $guardianApp -Force
& (Join-Path $guardianRoot 'installer/build-setup.ps1')
@'
@echo off
start "" "%~dp0FlightOLEDGuardian.App.exe" --mock
'@ | Set-Content -LiteralPath (Join-Path $guardianApp 'Start-Mock.cmd') -Encoding ascii
@'
@echo off
start "" "%~dp0FlightOLEDGuardian.App.exe"
'@ | Set-Content -LiteralPath (Join-Path $guardianApp 'Start-Guardian.cmd') -Encoding ascii
$guardianZip=Join-Path $guardianRoot 'outputs/FlightOLEDGuardian-v0.3-Complete.zip'
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$guardianStream=[IO.File]::Open($guardianZip,[IO.FileMode]::Create)
$guardianArchive=[IO.Compression.ZipArchive]::new($guardianStream,[IO.Compression.ZipArchiveMode]::Create)
try {
    [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($guardianArchive,(Join-Path $guardianRoot 'outputs/FlightOLEDGuardian-v0.3-Setup.exe'),'FlightOLEDGuardian-v0.3/FlightOLEDGuardian-v0.3-Setup.exe',[IO.Compression.CompressionLevel]::Optimal) | Out-Null
    foreach($file in Get-ChildItem -LiteralPath $guardianApp -File | Where-Object {$_.Name -notmatch 'FlightTest|\.pdb$'}){[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($guardianArchive,$file.FullName,('FlightOLEDGuardian-v0.3/App/'+$file.Name),[IO.Compression.CompressionLevel]::Optimal) | Out-Null}
    $guardianSourceFiles=@(Get-ChildItem -LiteralPath (Join-Path $guardianRoot 'src'),(Join-Path $guardianRoot 'tests'),(Join-Path $guardianRoot 'docs'),(Join-Path $guardianRoot 'installer') -File -Recurse | Where-Object {$_.FullName -notmatch '\\(bin|obj)\\'})
    $guardianSourceFiles+=Get-ChildItem -LiteralPath $guardianRoot -File -Force | Where-Object {$_.Name -in @('FlightOLEDGuardian.sln','Directory.Build.props','NuGet.Config','README.md','LICENSE','build.ps1','package.ps1','.gitignore')}
    foreach($file in $guardianSourceFiles){$relative=[IO.Path]::GetRelativePath($guardianRoot,$file.FullName).Replace('\','/');[IO.Compression.ZipFileExtensions]::CreateEntryFromFile($guardianArchive,$file.FullName,('FlightOLEDGuardian-v0.3/Source/'+$relative),[IO.Compression.CompressionLevel]::Optimal) | Out-Null}
}
finally {$guardianArchive.Dispose();$guardianStream.Dispose()}
Get-Item -LiteralPath $guardianZip | Select-Object FullName,Length
