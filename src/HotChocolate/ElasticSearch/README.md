# HotChocolate ElasticSearch Provider

This folder contains a production-ready ElasticSearch data provider for HotChocolate, based on `main` APIs and aligned with the current Data middleware pipeline.

## Concept

The provider follows the same architecture used by the other data providers (MongoDB/Raven/etc.):

1. `HotChocolate.Data.ElasticSearch.Driver` translates GraphQL filter/sort/paging arguments into an intermediate ElasticSearch operation model (`ISearchOperation`).
2. `HotChocolate.Data.ElasticSearch` rewrites that intermediate model into NEST search requests and executes against `IElasticClient`.
3. Middleware integration is done through HotChocolate's current `IQueryBuilder` contract (`Prepare`/`Apply`) and local state keys, so filtering/sorting/paging can be composed predictably.

This keeps GraphQL-specific concerns in the driver and transport/client concerns in the runtime package.

## Package Layout

- `src/Data.ElasticSearch.Driver`
  - Filter conventions and handlers
  - Sort conventions and handlers
  - Cursor/offset paging providers and handlers
  - `IElasticSearchExecutable` abstraction and execution primitives
- `src/Data.ElasticSearch`
  - NEST executable (`NestExecutable<T>`)
  - Search operation rewriter (`ISearchOperation` -> NEST `IQuery`)
  - `IElasticClient` extension methods
- `test/Data.ElasticSearch.Tests`
  - End-to-end integration coverage via real Elasticsearch container (Squadron)

## What Was Finalized

- Ported from legacy provider hooks to current HotChocolate data provider hooks.
- Updated metadata storage to modern type system features (`Features`) instead of removed definition context APIs.
- Updated executable contracts to the current `IExecutable` patterns and async APIs.
- Ported and fixed cursor and offset paging handlers for current paging infrastructure.
- Fixed range operation nullability bug that produced invalid range queries.
- Aligned tests with current CookieCrumble snapshot system and current DateTime scalar behavior.

## Testing Strategy

Integration tests run against a containerized Elasticsearch instance and validate:

- Filtering behavior (string/comparable/list/in-not-in)
- Sorting behavior
- Cursor and offset pagination behavior
- Generated query snapshots for regression safety

The test project targets `net8.0`, `net9.0`, and `net10.0`.

## Current Dependencies

- `NEST` `7.17.5`
- `Squadron.Elasticsearch` `1.0.1`

## Follow-Up Roadmap

- Add docs/examples under the main docs site for provider setup.
- Evaluate OpenSearch compatibility matrix and package split if needed.
- Add projection support parity if requested.
