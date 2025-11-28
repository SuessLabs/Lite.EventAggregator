# Build script for generating release package

Write-Output "Cleaning output folders..."

if (Test-Path -Path "output\")
{
  Remove-Item output\* -Recurse -Force
}

# Clean both debug and release
dotnet clean src/Lite.EventIpc.slnx --configuration Release

dotnet build src/Lite.EventIpc.slnx --configuration Release

# Publish
Write-Output "Cleaning Publish folder.."

if (Test-Path -Path "publish\")
{
  Remove-Item publish\* -Recurse -Force
}
else
{
  New-Item -Path '.\publish' -ItemType Directory
}

## Publish build artifacts
##dotnet publish src/Lite.EventIpc/Lite.EventIpc.csproj /p:PublishProfile=src/Lite.EventIpc/Properties/PublishProfiles/win-x64.pubxml /p:DebugType=None /p:DebugSymbols=false
##
#### Compress published artifacts
##Write-Output "Compressing published artifacts..."
##$dttm = (Get-Date).ToString("yyyy-MM-dd")
##$version = (Get-Item -Path "publish/win-x64/Lite.EventIpc.dll").VersionInfo.FileVersion
##Compress-Archive -Path "publish/win-x64/*" -DestinationPath "publish/Lite.EventIpc-${version}-(win-x64)_${dttm}.zip"
