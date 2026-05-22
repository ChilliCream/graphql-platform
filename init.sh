#!/usr/bin/env bash
set -euo pipefail
SCRIPT_DIR=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)

dotnet restore "$SCRIPT_DIR/src/All.slnx"
(cd "$SCRIPT_DIR/website" && yarn)
