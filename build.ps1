param([switch]$Test,[switch]$UiTest,[switch]$Smoke,[switch]$Publish)
$ErrorActionPreference='Stop'
$guardianEnvironment=@{MSBuildEnableWorkloadResolver='false';DOTNET_CLI_HOME=(Join-Path $PSScriptRoot 'work/dotnet');APPDATA=(Join-Path $PSScriptRoot 'work/appdata');LOCALAPPDATA=(Join-Path $PSScriptRoot 'work/localappdata');NUGET_PACKAGES=(Join-Path $PSScriptRoot 'work/packages');DOTNET_CLI_TELEMETRY_OPTOUT='1'}
$guardianPrevious=@{}
Push-Location $PSScriptRoot
try {
foreach($name in $guardianEnvironment.Keys){$guardianPrevious[$name]=[Environment]::GetEnvironmentVariable($name,'Process');[Environment]::SetEnvironmentVariable($name,$guardianEnvironment[$name],'Process')}
dotnet build FlightOLEDGuardian.sln -c Release -m:1 -p:OutputPath=bin/ReleaseVerified/ -p:BuildInParallel=false -p:UseSharedCompilation=false -p:RestoreConfigFile="$PSScriptRoot/NuGet.Config" -nodeReuse:false
if($LASTEXITCODE -ne 0){throw 'Build failed'}
if($Test -or $UiTest){dotnet tests/FlightOLEDGuardian.UnitTests/bin/ReleaseVerified/FlightOLEDGuardian.UnitTests.dll;if($LASTEXITCODE -ne 0){throw 'Unit tests failed'};if($UiTest){dotnet tests/FlightOLEDGuardian.IntegrationTests/bin/ReleaseVerified/FlightOLEDGuardian.IntegrationTests.dll --wpf}else{dotnet tests/FlightOLEDGuardian.IntegrationTests/bin/ReleaseVerified/FlightOLEDGuardian.IntegrationTests.dll};if($LASTEXITCODE -ne 0){throw 'Integration tests failed'}}
if($Smoke){dotnet src/FlightOLEDGuardian.App/bin/ReleaseVerified/FlightOLEDGuardian.App.dll --mock --smoke-test --ui-self-test --data-dir "$PSScriptRoot/work/smoke-v03-final";if($LASTEXITCODE -ne 0){throw 'WPF smoke test failed'}}
if($Publish){dotnet publish src/FlightOLEDGuardian.App -c Release --no-build --no-restore -o outputs/FlightOLEDGuardian-v0.3 -m:1 -p:OutputPath=bin/ReleaseVerified/ -p:BuildInParallel=false -p:UseSharedCompilation=false -nodeReuse:false;if($LASTEXITCODE -ne 0){throw 'Publish failed'}}
}
finally {
foreach($name in $guardianPrevious.Keys){[Environment]::SetEnvironmentVariable($name,$guardianPrevious[$name],'Process')}
Pop-Location
}
