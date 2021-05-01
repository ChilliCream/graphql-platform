[CmdletBinding(PositionalBinding = $false)]
param ()

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function MarkShipped([string]$dir) {
    $shippedFilePath = Join-Path $dir "PublicAPI.Shipped.txt"
    [string[]]$shipped = Get-Content $shippedFilePath
    if ($null -eq $shipped) {
        $shipped = @()
    }

    $unshippedFilePath = Join-Path $dir "PublicAPI.Unshipped.txt"
    [string[]]$unshipped = Get-Content $unshippedFilePath | Where-Object { $_.trim() -ne "" }
    if ($null -eq $unshipped || $unshipped.Length -lt 1) {
        return
    }

    $removed = @()
    $removedPrefix = "*REMOVED*";

    Write-Host "Processing $dir"

    foreach ($item in $unshipped) {
        if ($item.Length -gt 0) {
            if ($item.StartsWith($removedPrefix)) {
                $item = $item.Substring($removedPrefix.Length)
                $removed += $item
            }
            else {
                $shipped += $item
            }
        }
    }

    $shipped | Sort-Object -Stable -Unique | Where-Object { -not $removed.Contains($_) } | Out-File $shippedFilePath -Encoding Ascii
    "" | Out-File $unshippedFilePath -Encoding Ascii
}

try {
    foreach ($file in Get-ChildItem -Path "../src" -Recurse -Include "PublicApi.Shipped.txt") {
        $dir = Split-Path -parent $file
        MarkShipped $dir
    }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    exit 1
}