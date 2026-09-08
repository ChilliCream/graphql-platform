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

At this stage of the cost engine effort, `FixtureWellFormednessTests` only
checks that every fixture is well-formed: its schema and operation parse
and its expected numbers are finite numbers. It intentionally runs before
the engine exists. A later task (core-plan-evaluate) adds the evaluate
theory that compiles and evaluates each fixture and asserts the result
against its `expected` values.

## Regenerating the corpus

The nine article fixtures here are hand-written from the article's own
listings; there is nothing to regenerate for them. A later task vendors the
~35 MIT-licensed `graphql-static-analysis-rs` unit-test fixtures and the
1,980-case ExactCase cost corpus dumped from the same crate; when that
lands, this section documents the dump command and its MIT attribution.
