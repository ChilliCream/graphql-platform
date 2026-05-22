#!/usr/bin/env bash

# Initialize a fresh checkout: restore .NET packages for the root solution, then
# install the website's yarn dependencies.

set -euo pipefail

cd "$(dirname "${BASH_SOURCE[0]}")"
dotnet restore src/All.slnx
(cd website && yarn)
