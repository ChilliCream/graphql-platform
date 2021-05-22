[CmdletBinding(PositionalBinding = $false)]
param (
    [string]$from,
    [string]$to
)

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    if ([string]::IsNullOrWhiteSpace($to)) {
        $to = git branch --show-current
    }

    Write-Host "Diffing '$from' to '$to'..."

    git --no-pager diff --minimal -U0 --word-diff "$from" "$to" -- "../src/**/PublicAPI.Shipped.txt"
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    exit 1
}