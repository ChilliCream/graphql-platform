#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../../../../../.." && pwd)"
ORACLE_DIR="$REPO_ROOT/src/HotChocolate/CostAnalysis/tools/oracle"
RESULTS_DIR="$REPO_ROOT/src/HotChocolate/CostAnalysis/benchmarks/results/head-to-head"
ASSEMBLY="$PROJECT_DIR/bin/Release/net11.0/HotChocolate.CostAnalysis.Core.Benchmarks.dll"
SELECTION="all"
REPLICATES="10"

while [ "$#" -gt 0 ]; do
    case "$1" in
        --endpoints)
            SELECTION="endpoints"
            ;;
        --all)
            SELECTION="all"
            ;;
        --replicates)
            shift
            REPLICATES="${1:?--replicates requires a value}"
            ;;
        *)
            echo "Unknown argument: $1" >&2
            exit 2
            ;;
    esac
    shift
done

TEMP_DIR="$(mktemp -d)"
trap 'rm -rf "$TEMP_DIR"' EXIT
CORPUS="$TEMP_DIR/corpus.json"

"$ORACLE_DIR/clone-oracle.sh"
(
    cd "$ORACLE_DIR/.oracle/benchmarks"
    cargo build --release --locked
    target/release/graphql-static-analysis-benchmark dump "$CORPUS"
    target/release/graphql-static-analysis-benchmark cost-endpoints \
        > "$TEMP_DIR/oracle-cost-endpoints.csv"
)

CORPUS_BYTES="$(wc -c < "$CORPUS")"
if [ "$CORPUS_BYTES" -le 5242880 ]; then
    echo "Generated corpus is <=5 MB and must be committed before handoff." >&2
    exit 1
fi
echo "Generated corpus is $CORPUS_BYTES bytes, regenerating from the pinned MIT source."

dotnet build "$PROJECT_DIR/HotChocolate.CostAnalysis.Core.Benchmarks.csproj" \
    --configuration Release \
    --framework net11.0 \
    --verbosity q \
    -nodeReuse:false \
    -maxcpucount:1

mkdir -p "$RESULTS_DIR"
cp "$TEMP_DIR/oracle-cost-endpoints.csv" "$RESULTS_DIR/oracle-cost-endpoints.csv"
python3 "$SCRIPT_DIR/campaign.py" \
    --corpus "$CORPUS" \
    --rust-binary "$ORACLE_DIR/.oracle/benchmarks/target/release/graphql-static-analysis-benchmark" \
    --assembly "$ASSEMBLY" \
    --output-dir "$RESULTS_DIR" \
    --selection "$SELECTION" \
    --replicates "$REPLICATES" \
    --git-root "$REPO_ROOT"
