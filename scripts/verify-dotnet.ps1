#Requires -Version 5.1
<#
.SYNOPSIS
  Restore, format-verify, build, and test the LCCAP .NET solution (Release).

.EXAMPLE
  .\scripts\verify-dotnet.ps1
#>

$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $RepoRoot

function Invoke-DotNetStep([string]$Label, [ScriptBlock]$Block) {
    Write-Host $Label -ForegroundColor Cyan
    & $Block
    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }
}

Invoke-DotNetStep ">>> dotnet restore Lccap.sln" {
    dotnet restore Lccap.sln
}

Invoke-DotNetStep ">>> dotnet format Lccap.sln --verify-no-changes" {
    dotnet format Lccap.sln --verify-no-changes
}

Invoke-DotNetStep ">>> dotnet build Lccap.sln -c Release --no-restore" {
    dotnet build Lccap.sln -c Release --no-restore
}

Invoke-DotNetStep ">>> dotnet test Lccap.sln -c Release --no-build" {
    dotnet test Lccap.sln -c Release --no-build
}

Write-Host ""
Write-Host "verify-dotnet: OK (all steps passed)." -ForegroundColor Green
