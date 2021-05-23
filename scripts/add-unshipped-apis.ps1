[CmdletBinding(PositionalBinding = $false)]
param ()

Set-StrictMode -version 2.0
$ErrorActionPreference = "Stop"

try {
    $src_dir = Resolve-Path -Path "../src/All.sln"

    Write-Host "Installing dotnet-format..."

    dotnet tool install -g dotnet-format

    Write-Host "Fixing Analyzer warnings..."

    dotnet format "$src_dir" --fix-analyzers warn --diagnostics RS0016

    Write-Host "All done!"
}
catch {
    Write-Host $_
    Write-Host $_.Exception
    exit 1
}

