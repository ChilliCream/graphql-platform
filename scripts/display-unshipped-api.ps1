[CmdletBinding(PositionalBinding = $false)]
param ()

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

function ShowUnshipped([string]$file, [string]$src_dir) {
    [string[]]$unshipped = Get-Content $file

    if ([string]::IsNullOrWhiteSpace($unshipped)) {
        return
    }

    $dir = (Split-Path -parent $file) -replace [regex]::escape($src_dir + [IO.Path]::DirectorySeparatorChar), ""
    Write-Host -Foreground green "## ${dir}"

    foreach ($item in $unshipped) {
        if ($item.Length -gt 0) {
            Write-Host "$item"
        }
    }
}

try {
    $src_dir = Resolve-Path -Path "../src"

    foreach ($file in Get-ChildItem -Path "$src_dir" -Recurse -Include "PublicApi.Unshipped.txt") {
        ShowUnshipped $file $src_dir
    }
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    exit 1
}