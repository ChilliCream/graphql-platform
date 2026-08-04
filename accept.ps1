# Promote snapshot files left in `__mismatch__/` directories by failing snapshot
# tests to be the new canonical snapshots.
#
# For each `__mismatch__/` directory under the repo root:
#   - move its top-level files up one level (overwriting any existing snapshot),
#   - then remove the `__mismatch__/` directory entirely (deleting any nested
#     mismatch leftovers).

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSCommandPath

Get-ChildItem -LiteralPath $repoRoot -Recurse -Directory -Filter '__mismatch__' |
    ForEach-Object {
        $mismatchDir = $_.FullName
        if (-not (Test-Path -LiteralPath $mismatchDir)) { return }

        $snapshotDir = Split-Path -Parent $mismatchDir
        Write-Host "Analyzing $mismatchDir ..."

        Get-ChildItem -LiteralPath $mismatchDir -File | ForEach-Object {
            Move-Item -LiteralPath $_.FullName -Destination (Join-Path $snapshotDir $_.Name) -Force
        }
        Remove-Item -LiteralPath $mismatchDir -Recurse -Force
    }
