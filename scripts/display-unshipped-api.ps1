[CmdletBinding(PositionalBinding=$false)]
param ()

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function ShowUnshipped([string]$file) {
    $unshipped = Get-Content $file

    if ([string]::IsNullOrWhiteSpace($unshipped)) {
        return
    }

    $dir = Split-Path -parent $file
    $project = Split-Path $dir -Leaf
    Write-Host "## ${project}"

    foreach ($item in $unshipped) {
        if ($item.Length -gt 0) {
            Write-Host "$item"
        }
    }
}

try {
    foreach ($file in Get-ChildItem -Path "../src" -Recurse -Include "PublicApi.Unshipped.txt") {
        ShowUnshipped $file
    }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    exit 1
}