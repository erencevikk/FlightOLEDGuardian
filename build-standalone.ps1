param([string]$RuntimeVersion='8.0.34',[string]$RuntimeFeed='https://api.nuget.org/v3/index.json')
$ErrorActionPreference='Stop'
$guardianRoot=$PSScriptRoot
$guardianEnv=@{MSBuildEnableWorkloadResolver='false';DOTNET_CLI_HOME=(Join-Path $guardianRoot 'work/dotnet');NUGET_PACKAGES=(Join-Path $guardianRoot 'work/packages');APPDATA=(Join-Path $guardianRoot 'work/appdata');LOCALAPPDATA=(Join-Path $guardianRoot 'work/localappdata')}
$guardianPrevious=@{}
try {
foreach($name in $guardianEnv.Keys){$guardianPrevious[$name]=[Environment]::GetEnvironmentVariable($name,'Process');[Environment]::SetEnvironmentVariable($name,$guardianEnv[$name],'Process')}
dotnet publish (Join-Path $guardianRoot 'src/FlightOLEDGuardian.App') -c Release -r win-x64 --self-contained true -o (Join-Path $guardianRoot 'outputs/FlightOLEDGuardian-v0.3-Standalone') -m:1 -p:RuntimeFrameworkVersion=$RuntimeVersion -p:RestoreConfigFile="$guardianRoot/NuGet.Config" -p:RestoreSources="$RuntimeFeed" -p:NuGetAudit=false -p:UseSharedCompilation=false -nodeReuse:false
if($LASTEXITCODE -ne 0){throw 'Standalone publish failed'}
}finally{foreach($name in $guardianPrevious.Keys){[Environment]::SetEnvironmentVariable($name,$guardianPrevious[$name],'Process')}}
