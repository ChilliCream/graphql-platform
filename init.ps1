# Initialize a fresh checkout: restore .NET packages for the root solution, then
# install the website's yarn dependencies.

$ErrorActionPreference = 'Stop'

Push-Location (Split-Path -Parent $PSCommandPath)
try {
    & dotnet restore src/All.slnx
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    Push-Location website
    try {
        & yarn
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    }
    finally {
        Pop-Location
    }
}
finally {
    Pop-Location
}
