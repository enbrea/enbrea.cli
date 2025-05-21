# Copyright (c) STÜBER SYSTEMS GmbH. All rights reserved.
# Licensed under the MIT License.

<#
 .Synopsis
    Install Enbrea CLI on Windows.
 .Description
    Always the latest Enbrea CLI release package will be installed.
 .Parameter Destination
    The destination path to install Enbrea CLI to. 
 .Parameter Language
    The display language of the MSI setup package and the Enbrea CLI.
 .Example
    # Install the newest MSI package
    .\install-enbrea-cli.ps1
 .Example
    # Invoke this script directly from GitHub
    iex "& { $(irm https://raw.githubusercontent.com/enbrea/enbrea.cli/main/tools/install-enbrea-cli.ps1) }"
#>
[CmdletBinding(DefaultParameterSetName = "MSI")]
param(
    [string] 
    $Destination = $null,
    [ValidateSet($null,'en','de')]
    [string] 
    $Language = $null,
    [Parameter(ParameterSetName = "MSI")]
	[switch] 
    $Quiet,
    [Parameter(ParameterSetName = "Portable")]
    [switch] 
    $Portable,
    [Parameter(ParameterSetName = "Portable")]
    [switch] 
    $AddToPath
)
#process
#{
    Set-StrictMode -Version 5.1
    $ErrorActionPreference = "Stop"

    # Get OS platform
    $IsLinuxEnv = (Get-Variable -Name "IsLinux" -ErrorAction Ignore) -and $IsLinux
    $IsMacOSEnv = (Get-Variable -Name "IsMacOS" -ErrorAction Ignore) -and $IsMacOS
    $IsWinEnv = !$IsLinuxEnv -and !$IsMacOSEnv

    # Get system language if not specified
    if ([string]::IsNullOrEmpty($Language))
    {
        $CultureInfo = Get-Culture
        $Language = $CultureInfo.TwoLetterISOLanguageName
        Write-Verbose "Found language is '$Language'" -Verbose
    }   
    
    # Set default language if given language is not supported
    if (-not ('en', 'de' -contains $Language))
    {
        $Language = 'en'
        Write-Verbose "Found language is '$Language'" -Verbose
    }

    # Set default installation folder
    if (-not $Destination) 
    {
        if ($IsWinEnv) 
        {
            $Destination = "$env:LOCALAPPDATA\Stueber Systems\Enbrea CLI"
        } 
        else 
        {
            $Destination = "~/.enbrea-cli"
        }
    }

    # Resolve installation folder variable
    $Destination = $PSCmdlet.SessionState.Path.GetUnresolvedProviderPathFromPSPath($Destination)

    # Get OS architecture
    if (-not $IsWinEnv) 
    {
        throw "This Powershell script currently only supports Windows."
    } 
    elseif ($(Get-ComputerInfo -Property OsArchitecture).OsArchitecture -eq "ARM 64-bit Processor") 
    {
        $architecture = "arm64"
    } 
    else 
    {
        switch ($env:PROCESSOR_ARCHITECTURE) 
        {
            "AMD64" { $architecture = "x64" }
            "x86"   { $architecture = "x86" }
            default { throw "Enbrea CLI package for OS architecture '$_' is not supported." }
        }
    }
    
    # We need a temp folder for the download
    $tempDir = Join-Path ([System.IO.Path]::GetTempPath()) ([System.IO.Path]::GetRandomFileName())
    $null = New-Item -ItemType Directory -Path $tempDir -Force -ErrorAction SilentlyContinue
    
    # Download and install...
    try 
    {
        # Setting Tls to 12 to prevent the Invoke-WebRequest : The request was
        # aborted: Could not create SSL/TLS secure channel. error.
        $originalValue = [Net.ServicePointManager]::SecurityProtocol
        [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

        if ($IsWinEnv) 
        {
            if (-not $Portable) 
            {
                $packageName = "enbrea.cli-${architecture}-${language}.msi"
            } 
            else 
            {
                $packageName = "enbrea.cli-portable.zip"
            }
        }

        $downloadURL = "https://github.com/enbrea/enbrea.cli/releases/latest/download/${packageName}"
        Write-Verbose "About to download package from '$downloadURL'" -Verbose

        $packagePath = Join-Path -Path $tempDir -ChildPath $packageName
        if (!$PSVersionTable.ContainsKey('PSEdition') -or $PSVersionTable.PSEdition -eq "Desktop") 
        {
            # On Windows PowerShell, progress can make the download significantly slower
            $oldProgressPreference = $ProgressPreference
            $ProgressPreference = "SilentlyContinue"
        }

        try 
        {
            Invoke-WebRequest -Uri $downloadURL -OutFile $packagePath
        } 
        finally 
        {
            if (!$PSVersionTable.ContainsKey('PSEdition') -or $PSVersionTable.PSEdition -eq "Desktop") 
            {
                $ProgressPreference = $oldProgressPreference
            }
        }

        $contentPath = Join-Path -Path $tempDir -ChildPath "new"

        $null = New-Item -ItemType Directory -Path $contentPath -ErrorAction SilentlyContinue
        if ($IsWinEnv) 
        {
            if (-not $Portable) 
            {
                if ($Quiet) 
                {
                    Write-Verbose "Performing quiet install"
                    $ArgumentList=@("/i", $packagePath, "/quiet")
                    $process = Start-Process msiexec -ArgumentList $ArgumentList -Wait -PassThru
                    if ($process.exitcode -ne 0) 
                    {
                        throw "Quiet install failed, please rerun install without -Quiet switch or ensure you have administrator rights"
                    }
                } 
                else
                {
                    Write-Verbose "Performing install"
                    $ArgumentList=@("/i", $packagePath)
                    $process = Start-Process msiexec -ArgumentList $ArgumentList -Wait -PassThru
                    if ($process.exitcode -ne 0) 
                    {
                        Exit $process.exitcode
                    }
                } 
            }
            else 
            {
                Write-Verbose "Extract package archive"
                Expand-Archive -Path $packagePath -DestinationPath $contentPath
            }
        }

        if ($Portable) 
        {
            Remove-Destination $Destination
            if (Test-Path $Destination) 
            {
                Write-Verbose "Copying files" -Verbose
                # only copy files as folders will already exist at $Destination
                Get-ChildItem -Recurse -Path "$contentPath" -File | ForEach-Object 
                {
                    $DestinationFilePath = Join-Path $Destination $_.fullname.replace($contentPath, "")
                    Copy-Item $_.fullname -Destination $DestinationFilePath
                }
            } 
            else 
            {
                $null = New-Item -Path (Split-Path -Path $Destination -Parent) -ItemType Directory -ErrorAction SilentlyContinue
                Move-Item -Path $contentPath -Destination $Destination
            }
        }

        if ($AddToPath -and $Portable) 
        {
            if ($IsWinEnv) 
            {
                if ((-not ($Destination.StartsWith($ENV:USERPROFILE))) -and
                    (-not ($Destination.StartsWith($ENV:APPDATA))) -and
                    (-not ($Destination.StartsWith($env:LOCALAPPDATA)))) 
                {
                    $TargetRegistry = [System.EnvironmentVariableTarget]::Machine
                    try 
                    {
                        Add-PathToSettings -Path $Destination -Target $TargetRegistry
                    } 
                    catch 
                    {
                        Write-Warning -Message "Unable to save the new path in the machine wide registry: $_"
                        $TargetRegistry = [System.EnvironmentVariableTarget]::User
                    }
                } 
                else 
                {
                    $TargetRegistry = [System.EnvironmentVariableTarget]::User
                }

                # If failed to install to machine wide path or path was not appropriate for machine wide path
                if ($TargetRegistry -eq [System.EnvironmentVariableTarget]::User) 
                {
                    try 
                    {
                        Add-PathToSettings -Path $Destination -Target $TargetRegistry
                    } 
                    catch 
                    {
                        Write-Warning -Message "Unable to save the new path in the registry for the current user : $_"
                    }
                }        
            }
            
            ## Add to the current process 'Path'
            $env:Path = $Destination + [System.IO.Path]::PathSeparator + $env:Path
        }

    }
    finally 
    {
        # Restore original value
        [Net.ServicePointManager]::SecurityProtocol = $originalValue
        Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
    }
#}

<#
 .Synopsis
    Removes current installation folder
 .Description
    It renames the installation folder by adding `.old`. A previously renamed folder is deleted
#>
Function Remove-Destination {
    param(
        [string] 
        $Destination
    )
    process
    {
        if (Test-Path -Path $Destination) 
        {
            Write-Verbose "Removing old installation: $Destination" -Verbose
            if (Test-Path -Path "$Destination.old") 
            {
                Remove-Item "$Destination.old" -Recurse -Force
            }
            # Unix systems don't keep open file handles so you can just move files/folders even if in use
            Move-Item "$Destination" "$Destination.old"
        }
    }
}

<#
 .Synopsis
    Validation for Add-PathToSettings.
 .Description
    Validates that the parameter being validated:
    - is not null
    - is a folder and exists
    - and that it does not exist in settings where settings is:
        = the process PATH for Linux/OSX
        - the registry PATHs for Windows
#>
Function Test-PathNotInSettings {
    param(
        [string] 
        $Path
    )
    process
    {
        if ([string]::IsNullOrWhiteSpace($Path)) 
        {
            throw 'Argument is null'
        }

        # Remove ending DirectorySeparatorChar for comparison purposes
        $Path = [System.Environment]::ExpandEnvironmentVariables($Path.TrimEnd([System.IO.Path]::DirectorySeparatorChar));

        if (-not [System.IO.Directory]::Exists($Path)) 
        {
            throw "Path does not exist: $Path"
        }

        # [System.Environment]::GetEnvironmentVariable automatically expands all variables
        [System.Array] $InstalledPaths = @()
        if ([System.Environment]::OSVersion.Platform -eq "Win32NT") 
        {
            $InstalledPaths += @(([System.Environment]::GetEnvironmentVariable('PATH', [System.EnvironmentVariableTarget]::User)) -split ([System.IO.Path]::PathSeparator))
            $InstalledPaths += @(([System.Environment]::GetEnvironmentVariable('PATH', [System.EnvironmentVariableTarget]::Machine)) -split ([System.IO.Path]::PathSeparator))
        } 
        else 
        {
            $InstalledPaths += @(([System.Environment]::GetEnvironmentVariable('PATH'), [System.EnvironmentVariableTarget]::Process) -split ([System.IO.Path]::PathSeparator))
        }

        # Remove ending DirectorySeparatorChar in all items of array for comparison purposes
        $InstalledPaths = $InstalledPaths | ForEach-Object { $_.TrimEnd([System.IO.Path]::DirectorySeparatorChar) }

        # if $InstalledPaths is in setting return false
        if ($InstalledPaths -icontains $Path) 
        {
            throw 'Already in PATH environment variable'
        }

        return $true
    }
}

<#
 .Synopsis
    Adds a Path to settings (Supports Windows Only)
 .Description
    Adds the target path to the target registry.
 .Parameter Path
    The path to add to the registry. It is validated with Test-PathNotInSettings which ensures that:
    - The path exists
    - Is a directory
    - Is not in the registry (HKCU or HKLM)
 .Parameter Target
    The target hive to install the Path to.
    Must be either User or Machine
    Defaults to User
#>
Function Add-PathToSettings {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory, ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [ValidateNotNullOrEmpty()]
        [ValidateScript({Test-PathNotInSettings $_})]
        [string] 
        $Path,
        [Parameter(ValueFromPipeline, ValueFromPipelineByPropertyName)]
        [ValidateNotNullOrEmpty()]
        [ValidateSet([System.EnvironmentVariableTarget]::User, [System.EnvironmentVariableTarget]::Machine)]
        [System.EnvironmentVariableTarget] 
        $Target = ([System.EnvironmentVariableTarget]::User)
    )
    process
    {
        if (-not $IsWinEnv) {
            return
        }

        if ($Target -eq [System.EnvironmentVariableTarget]::User) 
        {
            [string] $Environment = 'Environment'
            [Microsoft.Win32.RegistryKey] $Key = [Microsoft.Win32.Registry]::CurrentUser.OpenSubKey($Environment, [Microsoft.Win32.RegistryKeyPermissionCheck]::ReadWriteSubTree)
        } 
        else 
        {
            [string] $Environment = 'SYSTEM\CurrentControlSet\Control\Session Manager\Environment'
            [Microsoft.Win32.RegistryKey] $Key = [Microsoft.Win32.Registry]::LocalMachine.OpenSubKey($Environment, [Microsoft.Win32.RegistryKeyPermissionCheck]::ReadWriteSubTree)
        }

        # $key is null here if it the user was unable to get ReadWriteSubTree access.
        if ($null -eq $Key) 
        {
            throw (New-Object -TypeName 'System.Security.SecurityException' -ArgumentList "Unable to access the target registry")
        }

        # Get current unexpanded value
        [string] $CurrentUnexpandedValue = $Key.GetValue('PATH', '', [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)

        # Keep current PathValueKind if possible/appropriate
        try {
            [Microsoft.Win32.RegistryValueKind] $PathValueKind = $Key.GetValueKind('PATH')
        } catch {
            [Microsoft.Win32.RegistryValueKind] $PathValueKind = [Microsoft.Win32.RegistryValueKind]::ExpandString
        }

        # Evaluate new path
        $NewPathValue = [string]::Concat($CurrentUnexpandedValue.TrimEnd([System.IO.Path]::PathSeparator), [System.IO.Path]::PathSeparator, $Path)

        # Upgrade PathValueKind to [Microsoft.Win32.RegistryValueKind]::ExpandString if appropriate
        if ($NewPathValue.Contains('%')) 
        { 
            $PathValueKind = [Microsoft.Win32.RegistryValueKind]::ExpandString 
        }

        $Key.SetValue("PATH", $NewPathValue, $PathValueKind)
    }
}

