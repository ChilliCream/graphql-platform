# Cost differential oracle

Regenerates the vendored ExactCase cost corpus
(`../../test/CostAnalysis.Core.Conformance.Tests/__resources__/rust-corpus/`)
from the MIT-licensed
[`graphql-static-analysis-rs`](https://github.com/duckki/graphql-static-analysis-rs)
crate, the differential oracle for this cost engine.

## Prerequisite

Rust is **not** installed on this machine or in CI by default. Regenerating
the corpus needs rustup with stable Rust >= 1.90 (the pinned commit's
`rust-version`) and a C++ compiler, because the crate's `fuzz` package links
`libfuzzer-sys` 0.4 unconditionally even for a plain `cargo build`/`cargo run`.
Install with:

```bash
curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y --profile minimal
. "$HOME/.cargo/env" && rustup toolchain install stable
```

Nothing here installs Rust for you; `clone-oracle.sh` checks for `cargo` and
prints this same install line if it is missing.

## Files

- `clone-oracle.sh`: clones `graphql-static-analysis-rs` at the pinned commit
  into the gitignored `.oracle/` next to this file, applies `oracle.patch`,
  and copies `dump_corpus.rs` into the checkout's `fuzz/examples/`.
- `oracle.patch`: a small patch against the pinned commit. `SCHEMA` and
  `variable_values` in the crate's `fuzz/src/tree_summary/operation.rs` are
  `pub(super)` (private to the fuzz package's own targets); the patch adds
  `pub fn schema()` and `pub fn variables_json(variable_case: u8)` accessors
  and registers `dump_corpus` as a `[[example]]` in `fuzz/Cargo.toml`.
- `dump_corpus.rs`: the dump program itself (see below).
- `LICENSE-graphql-static-analysis-rs.md`: the crate's MIT license text and
  attribution for the vendored commit.

## Regenerating the corpus

```bash
cd src/HotChocolate/CostAnalysis/tools/oracle
./clone-oracle.sh
cargo run --manifest-path .oracle/fuzz/Cargo.toml --example dump_corpus -- \
    ../../test/CostAnalysis.Core.Conformance.Tests/__resources__/rust-corpus
```

This overwrites `exact-case.json` and `manifest.json` in that directory. The
dump is deterministic: regenerating against the same pinned commit reproduces
byte-identical output, so `git status` on the output directory should be
clean after the first commit.

## What the dump does

`dump_corpus.rs` iterates `TreeSummaryInput::exhaustive_cases()`, the crate's
own deterministic 15,840-case matrix (14,400 legacy-family cases plus 1,440
structural cases; `deterministic_matrix_sizes_are_stable` in the crate pins
this count). It keeps only the IBM cost observation (`observation == 3`) on
the ExactCase backend (`mode == 0`): 1,980 rows. Syntactic-backend rows are
not emitted; this engine is ExactCase-only.

For each row:

- The operation's declared variables (if any) are coerced against the
  supplied `variable_case` values with `apollo_compiler::request::coerce_variable_values`,
  the crate's own spec-accurate `CoerceVariableValues()` implementation. A row
  whose variables real GraphQL coercion would reject (a missing non-null
  variable without an operation default, an explicit `null` for a non-null
  variable, or a value of the wrong type) is dropped and counted in the
  manifest's `excludedInvalidVariables`. The crate's own `TreeSummaryInput`
  bypasses coercion (`Valid::assume_valid_ref`) to reach more of its
  analyzer's code paths under fuzzing; this dump does not inherit that
  bypass, because the .NET conformance suite runs these fixtures through real
  variable coercion.
- Rows whose `(operation, variables, defaultListSize)` triple repeats an
  already-kept row are dropped and counted in `deduplicated`. The most common
  case is `variable_case` 0 (no variables supplied) and 1 (an empty variables
  object): the crate's own cost path (`Option::unwrap_or_default`) treats
  both identically, so they reach the estimator as the same request.
  Operations that reference neither `$x` nor `$y` collapse further, since the
  default-value suffix on an unused variable declaration never renders.
- The remaining rows are costed with the crate's own `CostEstimator` in
  `ExactCase` mode, with `default_list_size` set to the row's `list_size`
  (0..4 for the legacy-family rows; the structural rows fix it at 2). The
  emitted `typeCost`/`fieldCost` are the raw `f64` values `CostEstimator`
  returns, never the crate's `cost:T,F` display format.
- `emitted + excludedInvalidVariables + deduplicated == exactCostRows` always
  holds (asserted by the dump itself); the manifest records all four numbers
  plus the pinned commit.

The crate's shared fuzz schema declares
`directive @tag(flag: Boolean) on FIELD | INLINE_FRAGMENT`, which collides
with HotChocolate's built-in `@tag` directive. The dump drops that one line
from the emitted `sdl`; no dumped operation uses `@tag` (the crate only
applies it inside a named-fragment traversal variant this dump does not use).

## Rust-free validation

Reviewers without Rust installed can still validate the committed artifact
shape:

```bash
python3 -c "
import json
d = json.load(open('../../test/CostAnalysis.Core.Conformance.Tests/__resources__/rust-corpus/exact-case.json'))
m = json.load(open('../../test/CostAnalysis.Core.Conformance.Tests/__resources__/rust-corpus/manifest.json'))
assert m['matrixSize'] == 15840 and m['exactCostRows'] == 1980
assert len(d) == m['emitted'] == 1980 - m['excludedInvalidVariables'] - m['deduplicated'], m
assert all(r['backend'] == 'exact-case' and 'directive @tag' not in r['sdl'] for r in d)
print('ok', len(d))
"
```

This does not re-run the estimator, so it cannot catch a wrong `typeCost` or
`fieldCost`; it only checks the corpus's shape and bookkeeping.
