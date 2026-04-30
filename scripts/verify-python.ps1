#Requires -Version 5.1
<#
.SYNOPSIS
  Runs Python QA (ruff, pyright, pytest) under services/python-ai when present; otherwise skips with exit code 0.

.EXAMPLE
  .\scripts\verify-python.ps1
#>

$ErrorActionPreference = 'Stop'

$RepoRoot = Split-Path -Parent $PSScriptRoot
$PythonServicePath = [System.IO.Path]::Combine($RepoRoot, 'services', 'python-ai')

if (-not (Test-Path -LiteralPath $PythonServicePath -PathType Container)) {
    Write-Host ""
    Write-Host "Skipping Python verification: services/python-ai does not exist yet." -ForegroundColor Yellow
    Write-Host "When the Python service is added, this script will run ruff, pyright, and pytest from that directory."
    Write-Host ""
    exit 0
}

Set-Location $PythonServicePath

Write-Host ">>> python -m ruff check . (in $PythonServicePath)" -ForegroundColor Cyan
python -m ruff check .

Write-Host ">>> python -m ruff format --check ." -ForegroundColor Cyan
python -m ruff format --check .

Write-Host ">>> python -m pyright" -ForegroundColor Cyan
python -m pyright

Write-Host ">>> python -m pytest" -ForegroundColor Cyan
python -m pytest

Write-Host ""
Write-Host "verify-python: OK (all steps passed)." -ForegroundColor Green
