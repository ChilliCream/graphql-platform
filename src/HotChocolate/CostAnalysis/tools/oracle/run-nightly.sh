#!/usr/bin/env bash
# Runs the deterministic .NET generator and the pinned Rust oracle, then
# compares both f64 results bit-for-bit.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPOSITORY_ROOT="$(cd "$SCRIPT_DIR/../../../../.." && pwd)"
FUZZ_PROJECT="$SCRIPT_DIR/Fuzz/HotChocolate.CostAnalysis.Fuzz.csproj"
FUZZ_FOUND="$REPOSITORY_ROOT/src/HotChocolate/CostAnalysis/test/CostAnalysis.Core.Conformance.Tests/__resources__/fuzz-found"
SEED=1
CASES=200

while [ "$#" -gt 0 ]; do
    case "$1" in
        --seed)
            SEED="$2"
            shift 2
            ;;
        --cases)
            CASES="$2"
            shift 2
            ;;
        *)
            echo "error: unknown argument '$1'" >&2
            exit 2
            ;;
    esac
done

if ! command -v cargo >/dev/null 2>&1; then
    echo "error: cargo not found on PATH." >&2
    echo "Install rustup first:" >&2
    echo "  curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y --profile minimal" >&2
    echo "  . \"\$HOME/.cargo/env\" && rustup toolchain install stable" >&2
    exit 1
fi

if ! command -v c++ >/dev/null 2>&1; then
    echo "error: a C++ compiler is required because libfuzzer-sys links unconditionally." >&2
    exit 1
fi

rustc_version="$(rustc --version | awk '{ print $2 }')"
rustc_major="${rustc_version%%.*}"
rustc_minor="${rustc_version#*.}"
rustc_minor="${rustc_minor%%.*}"
if [ "$rustc_major" -lt 1 ] || { [ "$rustc_major" -eq 1 ] && [ "$rustc_minor" -lt 90 ]; }; then
    echo "error: stable Rust >= 1.90 is required; found $rustc_version." >&2
    exit 1
fi

c++ --version | head -1
"$SCRIPT_DIR/clone-oracle.sh"
cp "$SCRIPT_DIR/oracle.rs" "$SCRIPT_DIR/.oracle/fuzz/examples/oracle.rs"

TEMP_DIRECTORY="$(mktemp -d)"
trap 'rm -rf "$TEMP_DIRECTORY"' EXIT
CASES_FILE="$TEMP_DIRECTORY/cases.json"
RESULTS_FILE="$TEMP_DIRECTORY/results.jsonl"

dotnet run --project "$FUZZ_PROJECT" --framework net11.0 -- \
    --seed "$SEED" --cases "$CASES" --emit-only "$CASES_FILE"
cargo run --quiet --manifest-path "$SCRIPT_DIR/.oracle/fuzz/Cargo.toml" \
    --example oracle -- --input "$CASES_FILE" --output "$RESULTS_FILE"
dotnet run --project "$FUZZ_PROJECT" --framework net11.0 --no-build -- \
    --compare "$CASES_FILE" --oracle-results "$RESULTS_FILE" --fuzz-found "$FUZZ_FOUND"
