#!/usr/bin/env bash
# Clones the graphql-static-analysis-rs differential oracle at its pinned
# commit into ./.oracle (gitignored) and applies oracle.patch, which exposes
# the fuzz package's shared schema/variables for dump_corpus.rs and wires it
# in as a cargo example.
#
# Prerequisite: rustup with stable Rust >= 1.90, plus a C++ compiler (the
# fuzz package links libfuzzer-sys 0.4 unconditionally). Install with:
#   curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y --profile minimal
#   . "$HOME/.cargo/env" && rustup toolchain install stable
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ORACLE_DIR="$SCRIPT_DIR/.oracle"
REPO_URL="https://github.com/duckki/graphql-static-analysis-rs"
PINNED_COMMIT="fec57fd7a980b5399637fa464a9bdce0781d91dd"

if ! command -v cargo >/dev/null 2>&1; then
    echo "error: cargo not found on PATH." >&2
    echo "Install rustup first:" >&2
    echo "  curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y --profile minimal" >&2
    echo "  . \"\$HOME/.cargo/env\" && rustup toolchain install stable" >&2
    echo "A C++ compiler is also required; libfuzzer-sys links it unconditionally." >&2
    exit 1
fi

if [ -d "$ORACLE_DIR/.git" ]; then
    echo "Reusing existing checkout at $ORACLE_DIR"
    git -C "$ORACLE_DIR" fetch --quiet origin "$PINNED_COMMIT"
else
    rm -rf "$ORACLE_DIR"
    git clone --quiet "$REPO_URL" "$ORACLE_DIR"
fi

git -C "$ORACLE_DIR" checkout --quiet --force "$PINNED_COMMIT"
git -C "$ORACLE_DIR" clean --quiet -fdx

echo "Applying oracle.patch"
git -C "$ORACLE_DIR" apply --whitespace=nowarn "$SCRIPT_DIR/oracle.patch"

mkdir -p "$ORACLE_DIR/fuzz/examples"
cp "$SCRIPT_DIR/dump_corpus.rs" "$ORACLE_DIR/fuzz/examples/dump_corpus.rs"

echo "Oracle ready at $ORACLE_DIR (commit $PINNED_COMMIT)"
