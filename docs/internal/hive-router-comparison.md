# Hive Router vs HotChocolate Fusion Router - Feature Comparison

*Based on source code analysis of both routers (April 2026)*

## Feature Matrix

| Feature | Fusion | Hive | Notes |
|---------|:------:|:----:|-------|
| **Subscriptions** | | | |
| WebSocket (graphql-transport-ws) | YES | YES | Both implement the graphql-transport-ws subprotocol |
| Apollo Subscriptions Protocol | YES | -- | **ADVANTAGE** - Legacy Apollo WS protocol support |
| Server-Sent Events (SSE) | YES | YES | Both: full SSE client + server (`text/event-stream`) |
| JSON Lines (JSONL) | YES | -- | **ADVANTAGE** - `application/jsonl` + `application/graphql-response+jsonl` |
| Multipart HTTP streaming | YES | YES | Both support `multipart/mixed` |
| HTTP Callback (Apollo callback/1.0) | -- | YES | **GAP** - ULID-based subscription IDs, verifier tokens |
| Per-subgraph protocol selection | YES | YES | Fusion: `-settings.json`. Hive: per-subgraph config |
| Per-subgraph format selection | YES | -- | **ADVANTAGE** - Formats (JSONL, SSE, multipart) per capability per subgraph |
| Environment-specific transport config | YES | -- | **ADVANTAGE** - `{{VAR}}` references resolved per environment |
| **Query Planning** | | | |
| Cost-based optimization | YES | YES | Fusion: A* search. Hive: graph-based pathfinding |
| A* search with tunable heuristics | YES | -- | **ADVANTAGE** - Depth/operation/fanout weights |
| Planning timeout / guardrails | YES | YES | Both configurable |
| Operation merge policies | YES | -- | **ADVANTAGE** - Conservative / Balanced / Aggressive |
| Parallel execution | YES | YES | Both |
| @skip / @include evaluation | YES | YES | Both |
| Entity batch fetching | YES | YES | Both |
| Query normalization (defragmentation) | YES | YES | Fusion: `InlineFragmentOperationRewriter` (inline fragments, merge fields, remove static skips). Hive: normalize/minify pipeline |
| Mutation sequencing | YES | YES | Fusion: `CreateMutationPlanBase` slices mutations into sequential root nodes. Hive: `turn_mutations_into_sequence` |
| Variable deduplication | YES | YES | Fusion: `VariableDedupTable` hash-based. Hive: entity representation dedup |
| Entity representation dedup | YES | YES | Both: hash-based dedup within a fetch. Fusion: `VariableDedupTable` with `AdditionalPaths` fan-out. Hive: representation hash dedup |
| Multi-type entity batching | YES | YES | **ADVANTAGE** - Fusion: query-plan-level batching with streaming (JSONL/SSE/multipart). Hive: aliased `_entities` in single request/response |
| Progressive override (@override) | -- | YES | **GAP** - Percentage-based and expression-based routing |
| **Caching** | | | |
| Query plan cache | YES | YES | Fusion: LRU (default 256). Hive: moka concurrent cache |
| Document/parse cache | YES | YES | Both |
| Single-flight plan dedup | YES | YES | Both coalesce concurrent identical plan requests |
| Validation cache (positive results) | YES | YES | Both cache successful validation outcomes |
| Normalization cache | -- | YES | **GAP** - Hive caches normalized operations separately |
| Cache diagnostics / metrics | YES | YES | Both expose hit/miss metrics |
| Query result caching (@cacheControl) | YES | -- | **ADVANTAGE** - Built-in cache control directive composition |
| Serializable query plans | YES | -- | **ADVANTAGE** - JSON format + parser; enables centralized 2nd-level cache (Nitro, external storage) |
| Persisted Operations / APQ | YES | Plugin | **ADVANTAGE** - Built-in with Redis, InMemory, FileSystem backends. Hive: plugin example only |
| Response cache (Redis-backed) | -- | Plugin | Hive: plugin example, not built-in |
| **Observability** | | | |
| OpenTelemetry tracing | YES | YES | Both comprehensive |
| OTLP export (HTTP + gRPC) | YES | YES | Both via SDK |
| Prometheus metrics | SDK | YES | Fusion: via .NET OTEL SDK. Hive: built-in `/metrics` endpoint |
| Granular activity scopes | YES | YES | Both |
| Cache-level metrics | YES | YES | Fusion: `CacheDiagnostics` (hit/miss/evict/size/capacity). Hive: per-cache counters |
| Trace propagation (W3C, B3, Jaeger) | SDK | YES | Fusion: via .NET OTEL SDK. Hive: built-in config |
| Sampling rate | SDK | YES | Fusion: via .NET OTEL SDK. Hive: built-in config |
| GraphQL semantic conventions | YES | YES | **ADVANTAGE** - Fusion implements the OTel draft spec for GraphQL. Hive: OTel HTTP semantics |
| Span depth | 23+ | ~12 | **ADVANTAGE** - Deeper span coverage |
| Diagnostic event listeners (46+ events) | YES | -- | **ADVANTAGE** - 4 listener interfaces, extensible |
| Activity enricher (38 virtual methods) | YES | -- | **ADVANTAGE** - Customizable span enrichment |
| EventSource diagnostics | YES | -- | **ADVANTAGE** - .NET EventSource for planning + pool telemetry |
| DataLoader instrumentation | YES | -- | **ADVANTAGE** - Batch/dispatch/cache hit tracing |
| Hive Console integration | -- | YES | Usage reporting and tracing to Hive platform |
| **Security** | | | |
| Max query depth | YES | YES | Both |
| Custom validation rules | YES | YES | Both |
| Disable introspection | YES | YES | Fusion: auto-disabled in non-dev. Hive: VRL-based |
| @authorize / @authenticated | YES | YES | Both |
| JWT authentication | YES | YES | Fusion: ASP.NET Core middleware. Hive: built-in JWKS |
| Max tokens limit | YES | YES | Both |
| Max fields limit | YES | -- | **ADVANTAGE** - `MaxAllowedFields` (default 2,048) |
| Max nodes limit | YES | -- | **ADVANTAGE** - `MaxAllowedNodes` |
| Cost analysis (@cost, @listSize) | YES | -- | **ADVANTAGE** - Built-in cost analyzer, field + type limits |
| Request body size limit | YES | YES | Fusion: 20MB. Hive: 2MB. Both configurable |
| CSRF / Preflight prevention | YES | YES | Fusion: `GraphQL-Preflight` header. Hive: Content-Type checks |
| Max batch size limit | YES | -- | **ADVANTAGE** - Default 1,024 |
| Default security policy (prod) | YES | -- | **ADVANTAGE** - Cost + introspection auto-disabled |
| Max aliases limit | YES | YES | Fusion: covered by `MaxAllowedFields` (aliases are fields). Hive: dedicated setting |
| Max directives limit | YES | YES | **ADVANTAGE** - Fusion: per-location uniqueness (spec-compliant, stricter). Hive: global count limit |
| Auth filter vs reject modes | YES | YES | Both support filtering. Fusion: handled in validation. Hive: filter or reject modes |
| Composition validation (40+ rules) | YES | -- | **ADVANTAGE** |
| **Transport** | | | |
| HTTP/1.1 + HTTP/2 | YES | YES | Both |
| TLS | YES | YES | Both |
| File upload (multipart) | YES | -- | **ADVANTAGE** |
| Variable batching | YES | -- | **ADVANTAGE** - Composite Schema Spec format |
| Request batching | YES | -- | **ADVANTAGE** |
| Apollo request batching | YES | -- | **ADVANTAGE** - Per subgraph |
| In-memory connector (testing) | YES | -- | **ADVANTAGE** |
| **Schema Management** | | | |
| Multi-stage composition (40+ rules) | YES | -- | **ADVANTAGE** |
| Directive merging framework | YES | -- | **ADVANTAGE** |
| Hot reload (file watching) | YES | YES | Fusion: FileSystemWatcher + hash. Hive: file polling |
| Hot reload (CDN with retry) | YES | YES | Fusion: via Nitro integration (`IFusionConfigurationProvider`). Hive: built-in CDN polling |
| Supergraph archive (signed) | YES | -- | **ADVANTAGE** |
| Source schema settings files | YES | -- | **ADVANTAGE** - Per-subgraph JSON with env vars |
| **Extensibility** | | | |
| Request middleware (short-circuit) | YES | YES | Both can terminate pipeline early |
| Middleware ordering (before/after) | YES | -- | **ADVANTAGE** |
| Error filters | YES | YES | Both |
| HTTP request interceptor | YES | YES | Both |
| WebSocket session interceptor (7 hooks) | YES | -- | **ADVANTAGE** |
| Subgraph request/response hooks | YES | YES | Fusion: 3 delegates. Hive: 2 hooks |
| Operation planner interceptor | YES | -- | **ADVANTAGE** |
| Diagnostic listeners (46+ events) | YES | -- | **ADVANTAGE** |
| Type interceptors (20+ hooks) | YES | -- | **ADVANTAGE** - Schema build-time |
| Field/directive middleware | YES | -- | **ADVANTAGE** |
| Pre-parse / pre-validate hooks | YES | YES | Fusion: request middleware with before/after ordering can insert before any pipeline stage. Hive: plugin hooks |
| Supergraph reload hook | YES | YES | Fusion: register a configuration observer (`IObservable<FusionConfiguration>`). Hive: `on_supergraph_reload` |
| Plugin init/shutdown hooks | YES | YES | Fusion: configuration observers + background tasks (e.g. operation warming). Hive: `on_plugin_init`/`on_shutdown` |
| 18 shipped plugin examples | -- | YES | APQ, auth, caching, feature flags, etc. |
| **Performance** | | | |
| Object/memory pooling | YES | -- | **ADVANTAGE** |
| UTF-8 string interning | YES | -- | **ADVANTAGE** |
| Streaming result composition (MetaDB) | YES | -- | **ADVANTAGE** - Row-oriented, no full materialization |
| Subgraph variable dedup | YES | YES | Both |
| Subgraph in-flight request dedup | YES | YES | Both: leader/follower with hash fingerprinting. Fusion: `RequestDeduplicationHandler` (XxHash64). Hive: `dedupe.rs` (xxh3) |
| Router-level incoming request dedup | -- | YES | **GAP** - Hive deduplicates identical incoming client requests before execution |
| Subscription broadcast sharing | -- | YES | **GAP** - Multiple clients share one upstream subscription |
| Cross-transport dedup | -- | YES | **GAP** - WS + HTTP can share results |
| Concurrent request limiting | YES | YES | Fusion: concurrency gate (default 64, configurable per-endpoint). Hive: `max_long_lived_clients` (default 128) |
| Zero-copy JSON (sonic-rs) | -- | YES | Rust-specific optimization |
| **Header Propagation** | | | |
| Request header forwarding | YES | YES | Fusion: `UseHeaderPropagation()` (Microsoft middleware) + `OnBeforeSend`. Hive: declarative config |
| Response header forwarding | -- | YES | **GAP** - Merge algorithms (first/last/append) |
| Declarative header framework | SDK | YES | Fusion: Microsoft `HeaderPropagation` middleware (declarative). Hive: built-in YAML config |
| **Configuration** | | | |
| Fluent builder API (.NET DI) | YES | -- | **ADVANTAGE** |
| Source schema settings (JSON) | YES | -- | **ADVANTAGE** - With env var system |
| YAML / JSON / JSON5 router config | -- | YES | Different approach |
| VRL expressions | -- | YES | **GAP** - Sandboxed expression language |
| **Infrastructure** | | | |
| Health checks | ASP.NET | YES | Fusion: via ASP.NET Core. Hive: built-in |
| CORS | ASP.NET | YES | Fusion: via ASP.NET Core. Hive: built-in |
| Warmup service | YES | YES | Both |
| .NET Aspire integration | YES | -- | **ADVANTAGE** |
| **@defer / @stream** | | | |
| @defer support | YES | -- | **ADVANTAGE** - Full @defer support with incremental delivery |
| @stream support | Soon | -- | **ADVANTAGE** - PR open, merging imminently |
| **Relay / Global Object ID** | | | |
| Node interface routing | YES | -- | **ADVANTAGE** |
| Multiple ID formats | YES | -- | **ADVANTAGE** |

## Extensibility Architecture Comparison

### Hive Router: Single Plugin Trait

11 lifecycle hooks with `Proceed / EndWithResponse / OnEnd` control flow:

| Hook | Purpose |
|------|---------|
| `on_plugin_init` | Plugin initialization |
| `on_http_request` | Incoming HTTP request |
| `on_graphql_params` | GraphQL parameters parsed |
| `on_graphql_parse` | Document parsed |
| `on_graphql_validation` | Document validated |
| `on_query_plan` | Query plan created |
| `on_execute` | Execution start/end |
| `on_subgraph_execute` | Subgraph operation |
| `on_subgraph_http_request` | Subgraph HTTP call |
| `on_supergraph_reload` | Schema change |
| `on_graphql_error` | Error post-processing |
| `on_shutdown` | Graceful shutdown |

Ships 18 plugin examples (APQ, auth, response cache, feature flags, dedup, etc.).

### HotChocolate Fusion: Multi-Layer Architecture

| Layer | Interface | Hook Count | Short-Circuit |
|-------|-----------|------------|---------------|
| Request Middleware | `UseRequest()` | Full pipeline | YES |
| HTTP Interceptor | `IHttpRequestInterceptor` | 1 (OnCreate) | YES (via middleware) |
| Socket Interceptor | `ISocketSessionInterceptor` | 7 hooks | YES (Reject) |
| Planner Interceptor | `IOperationPlannerInterceptor` | 1 (OnAfterPlan) | No |
| Subgraph Hooks | `SourceSchemaHttpClientConfiguration` | 3 delegates | No |
| Error Filters | `IErrorFilter` | 1 (OnError) | No |
| Diagnostics | 4 listener interfaces | 46+ events | No (observational) |
| Type Interceptors | `TypeInterceptor` | 20+ hooks | No (schema-time) |
| Field Middleware | `UseField<T>()` | Per-field | YES |

**Coverage mapping:**

| Hive Hook | Fusion Equivalent | Status |
|-----------|-------------------|--------|
| `on_http_request` | `IHttpRequestInterceptor.OnCreateAsync()` | Covered |
| `on_graphql_params` | Same interceptor (OperationRequestBuilder) | Covered |
| `on_graphql_parse` | Diagnostic event only | Observe only |
| `on_graphql_validation` | Custom validation rules + diagnostic | Covered differently |
| `on_query_plan` | `IOperationPlannerInterceptor.OnAfterPlanCompleted()` | Covered (post-plan) |
| `on_execute` | Request middleware + diagnostics | Covered |
| `on_subgraph_execute` | Diagnostic only | **Partial** |
| `on_subgraph_http_request` | `OnBeforeSend`/`OnAfterReceive`/`OnSourceSchemaResult` | Covered (3 hooks) |
| `on_supergraph_reload` | Configuration observer (`IObservable<FusionConfiguration>`) | Covered |
| `on_graphql_error` | `IErrorFilter.OnError()` | Covered |
| `on_shutdown` | Background tasks + `IHostApplicationLifetime` | Covered |
| `on_plugin_init` | DI configuration + background tasks (e.g. operation warming) | Covered |

## Summary

### Our Advantages (26 features)

1. **A* search query planning** with tunable heuristics
2. **Operation merge policies** (Conservative / Balanced / Aggressive)
3. **Built-in @cacheControl** directive composition
4. **Multi-stage schema composition** with 40+ validation rules
5. **Directive merging framework** with pluggable strategies
6. **Supergraph archive packaging** with signature verification
7. **Serializable query plans** (JSON format + parser) enabling centralized 2nd-level caching via Nitro or external storage
8. **Variable batching** + **Request batching** + **Apollo request batching** to subgraphs
9. **File upload** (multipart/form-data) through the gateway
10. **In-memory connector** for testing without HTTP
11. **JSONL subscription transport** (`application/jsonl`, `application/graphql-response+jsonl`)
12. **Apollo Subscriptions Protocol** (legacy WebSocket)
13. **Persisted Operations / APQ** with Redis, InMemory, and FileSystem backends (built-in, not a plugin)
14. **OTel draft spec for GraphQL** semantic conventions implementation
15. **Per-subgraph transport format selection** (formats per capability per subgraph)
14. **Environment-specific transport config** with `{{VAR}}` system
15. **Cost analysis** (@cost, @listSize) with field + type cost limits
16. **Max fields limit**, **max nodes limit**, **max batch size limit**
17. **Default security policy** - cost + introspection auto-disabled in production
18. **WebSocket session interceptor** (7 lifecycle hooks)
19. **46+ diagnostic events** across 4 listener interfaces
20. **Activity enricher** (38 virtual enrichment methods)
21. **DataLoader instrumentation** (batch/dispatch/cache tracing)
22. **Type interceptors** (20+ schema build hooks) + field/directive middleware
23. **Middleware insertion ordering** (before/after)
24. **Extensive object pooling** + UTF-8 string interning
25. **Streaming result composition** (MetaDB, no full materialization)
26. **Relay Global Object ID** routing with multiple serialization formats
27. **.NET Aspire integration**
28. **Source schema settings files** with per-subgraph JSON config

### Actual Gaps (features Hive has that we're missing)

#### High Priority

1. **HTTP Callback subscriptions** (Apollo callback/1.0) - Used by Apollo ecosystem
2. **Subscription broadcast sharing** - Multiple clients share one upstream subscription connection
3. **Router-level incoming request deduplication** - Identical client requests share one execution before planning

#### Medium Priority
7. **Response header propagation** - Merge algorithms (first/last/append) for multi-subgraph responses
9. **Progressive override** (@override with labels) - Percentage-based traffic routing

#### Lower Priority
14. **Cross-transport request deduplication** (WS + HTTP sharing results)
15. **VRL expressions** for dynamic configuration values
