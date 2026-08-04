#!/usr/bin/env bash
# Start the eShop.Reviews service (a plain HotChocolate Core server) in LEDGER mode: the buffer-pool
# event listener is attached and writes a CSV ledger of JsonMemory / FixedSizeArrayPool (and arena, if
# any) rent/return/abandon events for offline leak + allocation analysis.
#
# Runs in the FOREGROUND so Ctrl+C performs a graceful shutdown that flushes the ledger. The ledger
# defaults to the repo's .work folder; override by pre-setting FUSION_ARENA_TRACE.
#
# Usage:
#   ./start-review.sh            # Release
#   ./start-review.sh aot        # NativeAOT publish + run
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../../../.." && pwd)"
MODE="${1:-release}"
REVIEWS_PORT=5224

# Ledger -> .work by default; override by exporting FUSION_ARENA_TRACE before calling.
FUSION_ARENA_TRACE="${FUSION_ARENA_TRACE:-$REPO_ROOT/.work/review-ledger.csv}"
export FUSION_ARENA_TRACE
mkdir -p "$(dirname "$FUSION_ARENA_TRACE")"

export ASPNETCORE_ENVIRONMENT=Production

echo "============================================"
echo "eShop.Reviews (HotChocolate Core) - LEDGER mode"
echo "============================================"
echo "  Endpoint:  http://localhost:$REVIEWS_PORT/graphql"
echo "  Ledger:    $FUSION_ARENA_TRACE"
echo "  Mode:      $MODE"
echo ""
echo "  Drive load from another terminal, attach dotMemory to the dotnet process, then press"
echo "  Ctrl+C here for a graceful shutdown that flushes the ledger. To profile via dotMemory's own"
echo "  launcher instead, set FUSION_ARENA_TRACE=$FUSION_ARENA_TRACE in its environment."
echo ""

cd "$SCRIPT_DIR/eShop.Reviews"

case "$MODE" in
  aot|AOT)
    echo "Building eShop.Reviews (NativeAOT)..."
    dotnet publish -c Release /p:PublishAot=true --nologo -v quiet -o ./bin/publish-aot
    if [ ! -f "./bin/publish-aot/eShop.Reviews" ]; then
      echo "AOT build failed - executable not found"
      exit 1
    fi
    echo "Starting (AOT) on port $REVIEWS_PORT..."
    exec ./bin/publish-aot/eShop.Reviews
    ;;

  release|RELEASE|*)
    echo "Building eShop.Reviews (Release)..."
    dotnet build -c Release --nologo -v quiet
    echo "Starting (Release) on port $REVIEWS_PORT..."
    exec dotnet run -c Release --no-build --no-restore
    ;;
esac
