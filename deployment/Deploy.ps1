#requires -Version 5.1
<#
.SYNOPSIS
Publishes Yuudachi for Linux ARM64 and deploys it over SSH with startup rollback.
.DESCRIPTION
Requires dotnet, OpenSSH (ssh/scp), and tar on this machine. Connect with SSH once
to verify the host key before deploying. An SSH key/agent avoids repeated password
prompts; host verification is never disabled. The SSH user must be able to write
the installation and deployment directories and control the systemd service.

The existing service must launch yuudachi (or dotnet yuudachi.dll) from inside
InstallDirectory. Its unit and environment files are not changed. The default
installation is the DIRECTORY /usr/local/bin/yuudachi, not an executable file.

On the first deployment, the existing installation is kept next to that directory
as yuudachi.previous-<release-id>. InstallDirectory becomes a symlink to the new
release. Later releases are kept in DeploymentRoot/releases. Nothing is pruned.

Server-side appsettings*.json and usersettings.json are copied once, preserving
permissions/ownership, into DeploymentRoot/shared and linked into each release.
Edit those shared files for future configuration changes. Local settings and
User Secrets are never uploaded. Existing systemd EnvironmentFile settings remain
in effect. The bot must already have working production configuration.

DeepSeek usage counters default to the service account's local application data
directory (normally ~/.local/share/yuudachi on Linux), outside release folders.
For a custom location, set Deepseek:UsageFilePath to an absolute, persistent path
writable by that account. Do not point it inside an installation/release folder.

The service is checked for a stable, active process for StartupCheckSeconds.
This detects startup crashes/restart loops, not Discord or DeepSeek availability.
Failure restores the previous installation and its original running/stopped state.
Keep the previous release until you have also verified the bot in Discord.
.EXAMPLE
.\deployment\Deploy.ps1 -WhatIf
.EXAMPLE
.\deployment\Deploy.ps1
.EXAMPLE
.\deployment\Deploy.ps1 -Server root@178.104.29.219 -StartupCheckSeconds 60
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [ValidatePattern('^[A-Za-z0-9_][A-Za-z0-9_.@-]*$')]
    [string] $Server = 'root@178.104.29.219',
    [ValidatePattern('^[A-Za-z0-9_][A-Za-z0-9_.@-]*\.service$')]
    [string] $ServiceName = 'yuudachi.service',
    [ValidatePattern('^/[A-Za-z0-9_-]+(/[A-Za-z0-9_.-]+)+$')]
    [string] $InstallDirectory = '/usr/local/bin/yuudachi',
    [ValidatePattern('^/[A-Za-z0-9_-]+(/[A-Za-z0-9_.-]+)+$')]
    [string] $DeploymentRoot = '/usr/local/lib/yuudachi',
    [ValidateRange(1, 65535)]
    [int] $Port = 22,
    [string] $IdentityFile,
    [ValidateRange(5, 300)]
    [int] $StartupCheckSeconds = 30
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false

function Invoke-NativeCommand {
    param([string] $FileName, [string[]] $ArgumentList)
    & $FileName @ArgumentList
    if ($LASTEXITCODE -ne 0) {
        throw "$FileName failed with exit code $LASTEXITCODE."
    }
}

foreach ($directory in @($InstallDirectory, $DeploymentRoot)) {
    if ($directory -match '/\.\.?(/|$)') {
        throw 'Deployment paths must not contain . or .. components.'
    }
}
if ($InstallDirectory -eq $DeploymentRoot -or
    $InstallDirectory.StartsWith("$DeploymentRoot/") -or
    $DeploymentRoot.StartsWith("$InstallDirectory/")) {
    throw 'The installation and deployment directories must not overlap.'
}

if (-not $PSCmdlet.ShouldProcess("${Server}:$InstallDirectory", "Publish linux-arm64 Release, upload, and restart $ServiceName with rollback")) {
    return
}

foreach ($tool in @('dotnet', 'ssh', 'scp', 'tar')) {
    Get-Command $tool -CommandType Application -ErrorAction Stop | Out-Null
}

$sshOptions = @('-o', 'StrictHostKeyChecking=yes', '-o', 'ConnectTimeout=15',
    '-o', 'ServerAliveInterval=15', '-o', 'ServerAliveCountMax=3')
$scpOptions = @($sshOptions) + @('-P', "$Port")
$sshOptions += @('-p', "$Port")
if ($IdentityFile) {
    $identity = (Resolve-Path -LiteralPath $IdentityFile).Path
    $sshOptions += @('-i', $identity)
    $scpOptions += @('-i', $identity)
}

$releaseId = [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ') + '-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)
$workDirectory = Join-Path ([IO.Path]::GetTempPath()) "yuudachi-deploy-$releaseId"
$publishDirectory = Join-Path $workDirectory 'publish'
$archive = Join-Path $workDirectory 'publish.tar.gz'
$project = Join-Path $PSScriptRoot '..\yuudachi\yuudachi.csproj'
$remoteScript = Join-Path $PSScriptRoot 'deploy-remote.sh'
$remoteStage = $null
$deploymentFinished = $false
New-Item -ItemType Directory -Path $workDirectory | Out-Null

try {
    Invoke-NativeCommand dotnet @('publish', $project, '--configuration', 'Release',
        '--runtime', 'linux-arm64', '--self-contained', 'true', '--output', $publishDirectory,
        '-p:PublishSingleFile=false', '-p:PublishTrimmed=false', '-p:ExcludeLocalConfigurationFromPublish=true')

    if (-not (Test-Path -LiteralPath (Join-Path $publishDirectory 'yuudachi') -PathType Leaf)) {
        throw 'The publish output is missing the Linux yuudachi executable.'
    }
    if (Get-ChildItem -LiteralPath $publishDirectory -File | Where-Object { $_.Name -like 'appsettings*.json' -or $_.Name -eq 'usersettings.json' }) {
        throw 'Publish output contains local settings. Aborting before uploading anything.'
    }

    Invoke-NativeCommand tar @('-czf', $archive, '-C', $publishDirectory, '.')
    $remoteStage = (Invoke-NativeCommand ssh ($sshOptions + @($Server,
        'umask 077; mktemp -d /tmp/yuudachi-deploy.XXXXXXXX')) | Out-String).Trim()
    if ($remoteStage -notmatch '^/tmp/yuudachi-deploy\.[A-Za-z0-9]+$') {
        throw 'SSH did not return a valid staging directory. Check for shell startup output.'
    }

    Write-Host "Uploading release $releaseId to $Server..."
    Invoke-NativeCommand scp ($scpOptions + @($archive, $remoteScript, "${Server}:$remoteStage/"))
    $command = "bash '$remoteStage/deploy-remote.sh' '$ServiceName' '$InstallDirectory' '$DeploymentRoot' '$releaseId' '$StartupCheckSeconds'"
    Invoke-NativeCommand ssh ($sshOptions + @($Server, $command))
    $deploymentFinished = $true
}
finally {
    if (-not $deploymentFinished -and $remoteStage -match '^/tmp/yuudachi-deploy\.[A-Za-z0-9]+$') {
        $cleanup = "if test -d '$remoteStage'; then rm -f -- '$remoteStage/publish.tar.gz' '$remoteStage/deploy-remote.sh' && rmdir -- '$remoteStage'; fi"
        & ssh @sshOptions $Server $cleanup
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Could not clean remote upload directory $remoteStage. Check it when SSH is available."
        }
    }
    Remove-Item -LiteralPath $workDirectory -Recurse -Force
}