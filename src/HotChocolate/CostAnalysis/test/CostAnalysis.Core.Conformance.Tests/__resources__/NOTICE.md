# Notice

The fixtures under `article/` reproduce the six cost-precision cases from:

> "Static Analysis for GraphQL, Verified in Lean"
> https://duckki.github.io/2026/08/30/static-analysis-for-graphql-verified-in-lean.html
> Published 2026-08-30.

Operation text, variables and expected `typeCost`/`fieldCost` numbers are
taken verbatim from the article. Schemas for every case except C5 are not
published in the article (it states the relevant `@cost` weights in prose
instead); this repository's schemas for those cases are faithful
reconstructions that reproduce the stated weights. C5's schema is the
article's own, published in full.

None of these fixtures are copied from the `graphql-lean` repository
(https://github.com/duckki/graphql-lean), which carries no license.

The article's numbers are cross-checked against the MIT-licensed
`graphql-static-analysis-rs` crate:

> https://github.com/duckki/graphql-static-analysis-rs
> Commit fec57fd7a980b5399637fa464a9bdce0781d91dd (v0.2.0)
> Copyright 2026 graphql-static-analysis contributors
> License: MIT

See the crate's `LICENSE` file for the full MIT license text.
