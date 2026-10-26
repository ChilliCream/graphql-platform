# CostAnalysis.Core.Conformance.Tests

A fixture-driven conformance suite for cost analysis. It includes six
hand-written precision cases (nine files) covering exclusive types,
list-size variables, complementary include/skip, duplicate response names,
signed weights and zero-length lists, plus fixtures ported from the Rust
estimator's unit tests and its exhaustive cost corpus.

## Running the suite

```bash
dotnet test --project src/HotChocolate/CostAnalysis/test/CostAnalysis.Core.Conformance.Tests/HotChocolate.CostAnalysis.Core.Conformance.Tests.csproj --framework net11.0
```

This command runs the `Precision_`, `RustUnit_`, `FuzzFound_`, and `Corpus_`
theories. Every populated row is parsed and compiled, and supplied variables
are coerced through the production coercion path before bit-exact comparison.

## Fixtures

Standalone fixtures under `__resources__/precision`, `__resources__/rust-unit`,
and `__resources__/fuzz-found` match `__resources__/fixture.schema.json`: a
schema, an operation, optional variables, and the expected IBM
`typeCost`/`fieldCost` pair. `__resources__/rust-corpus/exact-case.json` is an
array whose elements use the same fixture object shape.
`__resources__/rust-corpus/manifest.json` is corpus metadata, not a fixture.

The three populated fixture families are:

- `__resources__/precision/`: six hand-written precision cases (nine files)
  covering exclusive types, list-size variables, complementary include/skip,
  duplicate response names, signed weights and zero-length lists.
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

The precision family can be run on its own:

```bash
dotnet test --project src/HotChocolate/CostAnalysis/test/CostAnalysis.Core.Conformance.Tests/HotChocolate.CostAnalysis.Core.Conformance.Tests.csproj --framework net11.0 --filter-method "*Precision_Fixture_Should_MatchOracle*"
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

The nine precision fixtures are hand-written.
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

The generated corpus excludes the unused `@tag` definition and rows whose
variables fail GraphQL coercion. Each conformance test also validates its
fixture's variables through production coercion.
