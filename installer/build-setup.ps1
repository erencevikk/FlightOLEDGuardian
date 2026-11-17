$ErrorActionPreference='Stop'
$guardianRoot=Split-Path $PSScriptRoot
$guardianApp=Join-Path $guardianRoot 'outputs/FlightOLEDGuardian-v0.3'
$guardianTemp=Join-Path $guardianRoot 'work/setup'
New-Item -ItemType Directory -Path $guardianTemp -Force | Out-Null
$guardianPayload=Join-Path $guardianTemp 'Payload.zip'
$guardianFiles=Get-ChildItem -LiteralPath $guardianApp -File | Where-Object {$_.Name -notmatch 'FlightTest|\.pdb$|Start-Mock'}
Compress-Archive -LiteralPath $guardianFiles.FullName -DestinationPath $guardianPayload -Force
$guardianFramework=Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$guardianSetup=Join-Path $guardianRoot 'outputs/FlightOLEDGuardian-v0.3-Setup.exe'
$guardianReferences=@('System.dll','System.Core.dll','System.IO.Compression.dll','Microsoft.CSharp.dll','WPF/WindowsBase.dll','WPF/PresentationFramework.dll','WPF/PresentationCore.dll','System.Xaml.dll') | ForEach-Object { '/reference:'+(Join-Path $guardianFramework $_) }
& (Join-Path $guardianFramework 'csc.exe') /nologo /target:winexe /platform:x64 /optimize+ /warnaserror+ "/out:$guardianSetup" "/win32manifest:$guardianRoot/src/FlightOLEDGuardian.App/app.manifest" "/win32icon:$guardianRoot/src/FlightOLEDGuardian.App/Assets/Guardian.ico" "/resource:$guardianPayload,Payload.zip" "/resource:$guardianRoot/src/FlightOLEDGuardian.App/Assets/Guardian.ico,Guardian.ico" $guardianReferences (Join-Path $PSScriptRoot 'Setup.cs')
if($LASTEXITCODE -ne 0){throw 'Setup compilation failed'}
$guardianCheck=Join-Path $guardianTemp ('check-'+[guid]::NewGuid().ToString('N'))
$guardianProcess=Start-Process -FilePath $guardianSetup -ArgumentList @('--self-test',('"'+$guardianCheck+'"')) -WindowStyle Hidden -PassThru
if(-not $guardianProcess.WaitForExit(30000)){throw 'Setup payload test timed out'}
if($guardianProcess.ExitCode -ne 0 -or -not(Test-Path -LiteralPath (Join-Path $guardianCheck 'setup-test.txt'))){throw 'Setup payload test failed'}
Get-Content -LiteralPath (Join-Path $guardianCheck 'setup-test.txt')
Get-Item -LiteralPath $guardianSetup | Select-Object FullName,Length
