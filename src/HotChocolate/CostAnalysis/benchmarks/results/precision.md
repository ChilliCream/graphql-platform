# Cost precision

This exhibit compares the current cost engine with the six-case scorecard in
[Static Analysis for GraphQL, Verified in Lean](https://duckki.github.io/2026/08/30/static-analysis-for-graphql-verified-in-lean.html).
The comparison uses the article's published values. The other implementations
were not rerun for this exhibit.

## Published scorecard

| Implementation                  | Published passes       | Published score |
| ------------------------------- | ---------------------- | --------------- |
| HotChocolate 16.6.2             | C1, C4                 | 2/6             |
| Apollo Router 2.17.0            | C4, C6                 | 2/6             |
| Hive Router 0.2.2               | C3, C4, C6             | 3/6             |
| Cosmo Router 0.343.1            | C1, C2, C3, C4, C5     | 5/6             |
| graphql-query-complexity 2.0.0  | C1, C2, C3, C6         | 4/6             |

## Current results

| In-repository surface     | Logical cases | Fixture rows |
| ------------------------- | ------------- | ------------ |
| CostAnalysis.Core         | 6/6           | 9/9          |
| HotChocolate server       | 6/6           | 9/9          |
| Fusion gateway            | 6/6           | 9/9          |

All three surfaces read the same nine committed article fixtures. The six
logical cases occupy nine rows because C1 and C4 each have a named-fragment
variant, and C5 has a merged-control variant. The
[Core theory](../../test/CostAnalysis.Core.Conformance.Tests/ConformanceTests.cs)
evaluates the expected `typeCost` and `fieldCost` pair directly. The
[HotChocolate test](../../test/CostAnalysis.Tests/ArticleCasesTests.cs) and
[Fusion test](../../../Fusion/test/Fusion.AspNetCore.Tests/ArticleCasesTests.cs)
exercise their request pipelines and compare the reported `operationCost`
values with the same fixture expectations.

## Oracle agreement

The Core conformance project performs 1,054 exact cost evaluations:

- 9 article fixture rows
- 32 Rust ExactCase unit fixture rows
- 1,013 emitted Rust corpus rows

Each evaluation uses bit-exact equality for both expected `typeCost` and
`fieldCost`. The committed corpus
[manifest](../../test/CostAnalysis.Core.Conformance.Tests/__resources__/rust-corpus/manifest.json)
starts with a 15,840-entry matrix, selects 1,980 ExactCase cost candidates,
excludes 427 rows with invalid variables, and deduplicates 540
request-equivalent rows. The remaining 1,013 rows equal the number of elements
in the committed corpus file. The 32 standalone fixtures and their provenance
are documented in the
[Rust unit fixture README](../../test/CostAnalysis.Core.Conformance.Tests/__resources__/rust-unit/README.md).

The complete conformance project reports 1,097 passes and one expected skip
for the empty `fuzz-found` family. In addition to the 1,054 exact cost
evaluations, its passing tests include 41 standalone fixture-validation
rows, one fixture-discovery test, and one corpus-manifest test. HotChocolate
and Fusion run the nine article fixtures directly; they do not run the Rust
unit or corpus families through their end-to-end pipelines.

## Reproduce

Run all commands from the repository root. The first command runs the complete
Core conformance project:

```bash
dotnet test --project src/HotChocolate/CostAnalysis/test/CostAnalysis.Core.Conformance.Tests/HotChocolate.CostAnalysis.Core.Conformance.Tests.csproj --framework net11.0
```

The following filters reproduce the 9 article, 32 Rust unit, and 1,013 corpus
exact-evaluation counts. `--filter-method` matches the theory method name, not
the individual theory-row arguments.

```bash
dotnet test --project src/HotChocolate/CostAnalysis/test/CostAnalysis.Core.Conformance.Tests/HotChocolate.CostAnalysis.Core.Conformance.Tests.csproj --framework net11.0 --filter-method "*Article_Fixture_Should_MatchOracle*"
dotnet test --project src/HotChocolate/CostAnalysis/test/CostAnalysis.Core.Conformance.Tests/HotChocolate.CostAnalysis.Core.Conformance.Tests.csproj --framework net11.0 --filter-method "*RustUnit_Fixture_Should_MatchOracle*"
dotnet test --project src/HotChocolate/CostAnalysis/test/CostAnalysis.Core.Conformance.Tests/HotChocolate.CostAnalysis.Core.Conformance.Tests.csproj --framework net11.0 --filter-method "*Corpus_Should_MatchOracle*"
```

The manifest test reproduces the corpus accounting:

```bash
dotnet test --project src/HotChocolate/CostAnalysis/test/CostAnalysis.Core.Conformance.Tests/HotChocolate.CostAnalysis.Core.Conformance.Tests.csproj --framework net11.0 --filter-class "*CorpusManifestTests*"
```

The HotChocolate and Fusion filters each reproduce their nine article fixture
rows, which represent the six logical cases:

```bash
dotnet test --project src/HotChocolate/CostAnalysis/test/CostAnalysis.Tests/HotChocolate.CostAnalysis.Tests.csproj --framework net11.0 --filter-class "*ArticleCasesTests*"
dotnet test --project src/HotChocolate/Fusion/test/Fusion.AspNetCore.Tests/HotChocolate.Fusion.AspNetCore.Tests.csproj --framework net11.0 --filter-class "*ArticleCasesTests*"
```

Validate this document with the pinned Markdown linter:

```bash
npx --yes markdownlint-cli2@0.22.1 src/HotChocolate/CostAnalysis/benchmarks/results/precision.md
```
