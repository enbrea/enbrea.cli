<#
    .Synopsis
    Generates all supported MSI packages for testing purposes only.

    .Description
    This script builds and publish the ENBREA.CLI project and all supported MSI packages for testing purposes. There is no code signing.

    .Parameter TargetFolder
    The folder into which the final MSI packages are to be copied.

    .Example
    .\create-setups -TargetFolder "c:\MyFolder"
#>

param(
  [Parameter(Mandatory=$true)]
  [string]$TargetFolder
)

# Restore
dotnet restore "Enbrea.Cli.slnx"
# Publish x64
dotnet publish "./src/Enbrea.Cli/Enbrea.Cli.csproj" /p:PublishProfile=x64
# Publish x86
dotnet publish "./src/Enbrea.Cli/Enbrea.Cli.csproj" /p:PublishProfile=x86
# Create MSI Setup x64
dotnet build "./setup/Enbrea.Cli.Setup.wixproj" -c Release -p:Platform=x64
# Create MSI Setup x86
dotnet build "./setup/Enbrea.Cli.Setup.wixproj" -c Release -p:Platform=x86
# Copy MSI Setups to target folder
Copy-Item -Path "./setup/bin/x64/Release/de-DE/enbrea.cli-x64.msi" -Destination (Join-Path -Path $TargetFolder -ChildPath "enbrea.cli-x64-de.msi")
Copy-Item -Path "./setup/bin/x64/Release/en-US/enbrea.cli-x64.msi" -Destination (Join-Path -Path $TargetFolder -ChildPath "enbrea.cli-x64-en.msi")
Copy-Item -Path "./setup/bin/x86/Release/de-DE/enbrea.cli-x86.msi" -Destination (Join-Path -Path $TargetFolder -ChildPath "enbrea.cli-x86-de.msi")
Copy-Item -Path "./setup/bin/x86/Release/en-US/enbrea.cli-x86.msi" -Destination (Join-Path -Path $TargetFolder -ChildPath "enbrea.cli-x86-en.msi")
# Create sub folders for additional binary files
if (!(Test-Path -Path (Join-Path -Path $TargetFolder -ChildPath "binaries/x86"))) {New-Item -Type Directory (Join-Path -Path $TargetFolder -ChildPath "binaries/x86")}
if (!(Test-Path -Path (Join-Path -Path $TargetFolder -ChildPath "binaries/x64"))) {New-Item -Type Directory (Join-Path -Path $TargetFolder -ChildPath "binaries/x64")}
# Additionally copy binary files to the target folder
Copy-Item -Path "./src/Enbrea.Cli/bin/Publish/win-x64/Enbrea.exe" -Destination (Join-Path -Path $TargetFolder -ChildPath "binaries/x64/Enbrea.exe")
Copy-Item -Path "./src/Enbrea.Cli/bin/Publish/win-x86/Enbrea.exe" -Destination (Join-Path -Path $TargetFolder -ChildPath "binaries/x86/Enbrea.exe")
