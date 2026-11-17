$ErrorActionPreference='Stop'
$guardianRoot=$PSScriptRoot
$guardianStage=Join-Path $guardianRoot ('work/standalone-package-'+[guid]::NewGuid().ToString('N'))
$guardianApp=Join-Path $guardianRoot 'outputs/FlightOLEDGuardian-v0.3-Standalone'
if(-not(Test-Path (Join-Path $guardianApp 'coreclr.dll'))){throw 'Run build-standalone.ps1 first'}
New-Item -ItemType Directory -Path $guardianStage,(Join-Path $guardianStage 'ThirdParty') -Force | Out-Null
Get-ChildItem $guardianApp | Where-Object {$_.Name -notmatch '\.pdb$|FlightTest'} | Copy-Item -Destination $guardianStage -Recurse -Force
Copy-Item (Join-Path $guardianRoot 'docs/STANDALONE-README.txt') (Join-Path $guardianStage 'README-English-Turkce.txt')
Copy-Item (Join-Path $guardianRoot 'LICENSE') -Destination $guardianStage
$guardianRuntime=Get-Content (Join-Path $guardianApp 'FlightOLEDGuardian.App.runtimeconfig.json') -Raw | ConvertFrom-Json
$guardianRuntimeVersion=$guardianRuntime.runtimeOptions.includedFrameworks[0].version
foreach($guardianPackage in @('microsoft.netcore.app.runtime.win-x64','microsoft.windowsdesktop.app.runtime.win-x64')){
    $guardianPackageRoot=Join-Path $guardianRoot ('work/packages/'+$guardianPackage+'/'+$guardianRuntimeVersion)
    $guardianLicenseFiles=@(Get-ChildItem $guardianPackageRoot -File | Where-Object Name -match '^(LICENSE(\.TXT)?|THIRD-PARTY-NOTICES\.TXT)$')
    if($guardianLicenseFiles.Count -eq 0){throw ('Runtime license missing: '+$guardianPackage)}
    foreach($guardianFile in $guardianLicenseFiles){Copy-Item $guardianFile.FullName (Join-Path $guardianStage ('ThirdParty/'+$guardianPackage+'-'+$guardianFile.Name))}
}
$guardianArchive=Join-Path $guardianRoot 'outputs/FlightOLEDGuardian-v0.3-Standalone-win-x64.zip'
Compress-Archive -Path (Join-Path $guardianStage '*') -DestinationPath $guardianArchive -Force
Get-FileHash $guardianArchive -Algorithm SHA256
