# Initialize a fresh checkout: restore .NET packages for the root solution, then
# install the website's yarn dependencies.

$ErrorActionPreference = 'Stop'
$scriptDir = Split-Path -Parent $PSCommandPath

& dotnet restore (Join-Path $scriptDir 'src/All.slnx')
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Push-Location (Join-Path $scriptDir 'website')
try {
    & yarn
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}
finally {
    Pop-Location
}
