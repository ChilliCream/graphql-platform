# CostAnalysis.Core.Conformance.Tests

A fixture-driven conformance suite for the cost engine, and the public
proof-of-6/6 artifact for the six cost-precision cases published in
["Static Analysis for GraphQL, Verified in Lean"](https://duckki.github.io/2026/08/30/static-analysis-for-graphql-verified-in-lean.html).

## Running the suite

```bash
dotnet test --project src/HotChocolate/CostAnalysis/test/CostAnalysis.Core.Conformance.Tests/HotChocolate.CostAnalysis.Core.Conformance.Tests.csproj --framework net11.0
```

This command runs the `Article_`, `RustUnit_`, `FuzzFound_`, and `Corpus_`
theories. Every populated row is parsed and compiled, and supplied variables
are coerced through the production coercion path before bit-exact comparison.

## Fixtures

Standalone fixtures under `__resources__/article`, `__resources__/rust-unit`,
and `__resources__/fuzz-found` match `__resources__/fixture.schema.json`: a
schema, an operation, optional variables, and the expected IBM
`typeCost`/`fieldCost` pair. `__resources__/rust-corpus/exact-case.json` is an
array whose elements use the same fixture object shape.
`__resources__/rust-corpus/manifest.json` is corpus metadata, not a fixture.
`__resources__/NOTICE.md` records the license and provenance of every vendored
fixture.

The three populated fixture families are:

- `__resources__/article/`: the six published precision cases in nine files.
  They cover exclusive types, list-size variables, complementary directives,
  response-name collection, signed weights, and zero-length lists.
- `__resources__/rust-unit/`: 32 ExactCase fixtures ported from the Rust
  estimator unit tests. They cover directive semantics, weights, list sizing,
  response-name collection, input coercion and operation/schema defaults.
- `__resources__/rust-corpus/exact-case.json`: every emitted row from the
  vendored ExactCase cost corpus. It covers Boolean and type-condition case
  analysis, response-name collection, and `default_list_size` values 0..4.

`__resources__/fuzz-found/` is reserved for minimized disagreements found by
the nightly live-oracle job. The `FuzzFound_` theory reports a skip while this
family has no fixtures.

`ConformanceTests` has one theory per fixture family. Every theory parses a
mutable schema, coerces fixture variables through Fusion's production
coercion helper, compiles a `CostPlan`, and compares `typeCost` and
`fieldCost` bit-exactly with the fixture. Families without fixtures are
reported as skipped.

The article family can be run on its own:

```bash
dotnet test --project src/HotChocolate/CostAnalysis/test/CostAnalysis.Core.Conformance.Tests/HotChocolate.CostAnalysis.Core.Conformance.Tests.csproj --framework net11.0 --filter-method "*Article_Fixture_Should_MatchOracle*"
```

The Rust unit-test family can be run on its own:

```bash
dotnet test --project src/HotChocolate/CostAnalysis/test/CostAnalysis.Core.Conformance.Tests/HotChocolate.CostAnalysis.Core.Conformance.Tests.csproj --framework net11.0 --filter-method "*RustUnit_Fixture_Should_MatchOracle*"
```

The promoted nightly-fuzz family can be run on its own:

```bash
dotnet test --project src/HotChocolate/CostAnalysis/test/CostAnalysis.Core.Conformance.Tests/HotChocolate.CostAnalysis.Core.Conformance.Tests.csproj --framework net11.0 --filter-method "*FuzzFound_Fixture_Should_MatchOracle*"
```

The vendored corpus can be run on its own:

```bash
dotnet test --project src/HotChocolate/CostAnalysis/test/CostAnalysis.Core.Conformance.Tests/HotChocolate.CostAnalysis.Core.Conformance.Tests.csproj --framework net11.0 --filter-method "*Corpus_Should_MatchOracle*"
```

## Regenerating the corpus

The nine article fixtures are hand-written from the article's own listings.
The 32 Rust unit fixtures are direct JSON ports of the pinned estimator
tests and carry source-line provenance in each file.

The corpus is generated from the MIT-licensed
[`graphql-static-analysis-rs`](https://github.com/duckki/graphql-static-analysis-rs)
crate at the commit recorded in `__resources__/rust-corpus/manifest.json`.
The attribution and MIT license are recorded in
`../../tools/oracle/LICENSE-graphql-static-analysis-rs.md`. Regenerate it with:

```bash
cd src/HotChocolate/CostAnalysis/tools/oracle
./clone-oracle.sh
cargo run --manifest-path .oracle/fuzz/Cargo.toml --example dump_corpus -- \
    ../../test/CostAnalysis.Core.Conformance.Tests/__resources__/rust-corpus
```

The dump starts from a 15,840-case matrix and selects the 1,980 ExactCase
IBM-cost candidates. The committed manifest records how many rows were
emitted after exclusions and request-equivalent deduplication.

Two deviations are intentional and recorded by the generator:

- The fuzz schema's `directive @tag` declaration is dropped because it
  collides with HotChocolate's built-in `@tag` directive. No emitted operation
  uses the dropped declaration.
- Rows with variables that fail GraphQL coercion are excluded. Every committed
  row is coerced through the production path and a coercion failure fails its
  conformance test.
