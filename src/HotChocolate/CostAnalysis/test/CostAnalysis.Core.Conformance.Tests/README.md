# CostAnalysis.Core.Conformance.Tests

A fixture-driven conformance suite for the cost engine, and the public
proof-of-6/6 artifact for the six cost-precision cases published in
["Static Analysis for GraphQL, Verified in Lean"](https://duckki.github.io/2026/08/30/static-analysis-for-graphql-verified-in-lean.html).

## Running the suite

```bash
dotnet test src/HotChocolate/CostAnalysis/test/CostAnalysis.Core.Conformance.Tests/HotChocolate.CostAnalysis.Core.Conformance.Tests.csproj --framework net11.0
```

## Fixtures

Every fixture is a JSON file under `__resources__` that matches
`__resources__/fixture.schema.json`: a schema, an operation, optional
variables, and the expected IBM `typeCost`/`fieldCost` pair. `__resources__/NOTICE.md`
records the license and provenance of every vendored fixture.

`__resources__/article/` holds the six article cases (nine files: two of
the six also carry a named-fragment respelling of the same case, and C5
also carries an already-merged control operation). Attribution for these
fixtures is in `NOTICE.md`.

`__resources__/rust-unit/` holds 32 ExactCase fixtures ported from the
MIT-licensed `graphql-static-analysis-rs` estimator unit tests. The family
covers default and signed weights, abstract output weights, list sizing,
field collection, input coercion and costs, directive argument costs, and
operation and schema defaults. Its README records the pinned source and
the eight tests excluded from the JSON fixture form.

`ConformanceTests` has one theory per fixture family. Each theory parses a
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

## Regenerating the corpus

The nine article fixtures are hand-written from the article's own listings.
The 32 Rust unit fixtures are direct JSON ports of the pinned estimator
tests and carry source-line provenance in each file. A later task vendors
the 1,980-case ExactCase cost corpus dumped from the same crate and records
its dump command here.
