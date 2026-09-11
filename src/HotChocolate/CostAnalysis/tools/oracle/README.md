# Cost differential oracle

Regenerates the vendored ExactCase cost corpus
(`../../test/CostAnalysis.Core.Conformance.Tests/__resources__/rust-corpus/`)
from the MIT-licensed
[`graphql-static-analysis-rs`](https://github.com/duckki/graphql-static-analysis-rs)
crate, the differential oracle for this cost engine.

## Prerequisites

Corpus regeneration and live differential runs require rustup with stable
Rust >= 1.90 (the pinned commit's `rust-version`) and a C++ compiler. The
crate's `fuzz` package links `libfuzzer-sys` 0.4 unconditionally, including
for a plain `cargo build` or `cargo run`. Install Rust with:

```bash
curl --proto '=https' --tlsv1.2 -sSf https://sh.rustup.rs | sh -s -- -y --profile minimal
. "$HOME/.cargo/env" && rustup toolchain install stable
```

The scripts install nothing. `clone-oracle.sh` and `run-nightly.sh` print the
install line when `cargo` is missing, and `run-nightly.sh` also rejects Rust
versions below 1.90 and missing C++ compilers. Pull-request CI does not run
Rust. The scheduled workflow provisions stable Rust and uses the compiler on
`ubuntu-latest`.

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
- `oracle.rs`: a Rust example that coerces generated variables, builds a
  `CostModel`, and evaluates each case with `ExactCase` and its declared
  default list size.
- `Fuzz/`: the deterministic .NET generator and comparison command. It emits
  real `AddCostAnalyzer` interceptor schemas and synthetic annotated schemas.
- `run-nightly.sh`: generates cases, runs the live oracle, compares raw f64
  bits, and writes a standalone fixture for the first disagreement.
- `LICENSE-graphql-static-analysis-rs.md`: the crate's MIT license text and
  attribution for the vendored commit.

## Running the live oracle

```bash
bash src/HotChocolate/CostAnalysis/tools/oracle/run-nightly.sh \
    --seed 1 --cases 200
```

Every case stores its seed and uses the same finite `DefaultListSize` on both
engines. Generated operations are query-only and reject introspection
meta-fields, `@defer`, and `@stream` before emission. Variables are valid for
the generated operation and are coerced by `apollo_compiler` before the Rust
estimator runs.

Real schemas are printed from servers configured with `AddCostAnalyzer`,
cursor and offset paging, filtering, sorting, and list-of-scalar fields. The
printed SDL therefore carries explicit resolver, filter, sort, paging, and
list-of-scalar annotations. Synthetic schemas cover signed weights,
`@listSize`, input defaults, interfaces, unions, and directive argument
weights. Operations cover named fragments, Boolean directives, aliases, and
duplicate response names.

`slicingArgumentDefaultValue` is a ChilliCream extension. When a generated
operation supplies none of a field's slicing arguments, the generator adds
the declared default literal to the oracle copy of that operation only. It
does not rewrite the schema, and it adds nothing when any slicing argument is
already supplied.

Comparison uses the hexadecimal IEEE-754 bit patterns for `typeCost` and
`fieldCost`. A mismatch exits nonzero and writes one isolated
`source: "fuzz-found"` fixture under the conformance resources. Vendor it
before merging the engine fix so pull-request CI retains the regression case
without Rust.

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

The generator can build and emit cases without cloning or running the oracle:

```bash
dotnet run --project src/HotChocolate/CostAnalysis/tools/oracle/Fuzz \
    --framework net11.0 -- --seed 1 --cases 20 \
    --emit-only /tmp/fuzz-cases.json
```

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
