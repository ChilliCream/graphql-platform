---
title: "Coming from earlier Hot Chocolate versions"
description: "Plan a safe Hot Chocolate v16 upgrade from v15 or earlier by aligning packages, reading the migration guide, validating the schema contract, and testing important operations."
---

If you already know Hot Chocolate, you’re in the right place. Upgrading to v16 is not about relearning GraphQL. It is about carefully updating your framework version while keeping your schema stable, aligning your package set, applying documented breaking changes, and verifying that your key operations still work as expected.

This page guides you through the upgrade process. For specific code changes from v15 to v16, always refer to the [Migrate Hot Chocolate from 15 to 16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16) guide.

If your server is on v14 or earlier, follow each migration guide in order before you tackle the v15 to v16 step. For example, upgrading from v13 means you must apply the [13 to 14](/docs/hotchocolate/v16/migrating/migrate-from-13-to-14), [14 to 15](/docs/hotchocolate/v16/migrating/migrate-from-14-to-15), and [15 to 16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16) guides. Treat all major-version guides as part of your v16 upgrade plan so you don’t miss any breaking changes.

# Is this the right upgrade path for you?

Use this page if:

- You maintain an existing Hot Chocolate server.
- Your server currently runs v15 or an earlier version.
- You want to upgrade that server to v16.
- You can edit package references and build the solution.
- You need a plan and validation steps before diving into every breaking change.

If you’re starting your first v16 server, begin with [Installation and setup](/docs/hotchocolate/v16/learn/4-installation-and-setup/). If you’re adding Hot Chocolate to an ASP.NET Core app that hasn’t used it before, see [Existing ASP.NET Core app](/docs/hotchocolate/v16/learn/4-installation-and-setup/existing-aspnet-core-app/).

Before you start, answer these questions:

| Question | Your answer |
| --- | --- |
| Which service or bounded area moves first? | |
| What Hot Chocolate version does it run today? | |
| Which concrete v16 package version is the target? | |
| Where are the v15 to v16 migration notes? | [Migrate Hot Chocolate from 15 to 16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16) |
| Which schema snapshot or SDL file is the baseline? | |
| Which client operations prove the upgrade? | |

By the end of this section, you should have one service, a target v16 version, and a clear validation plan.

# What changes in v16?

Upgrading is more than editing package versions. Hot Chocolate v16 brings compile-time changes, runtime behavior updates, hosting differences, schema output changes, and new testing patterns.

The migration guide lists every detail, but use this table to help you classify the work as you encounter it:

| Area | What to check | Where to go next |
| --- | --- | --- |
| Startup and schema initialization | v16 initializes the schema and request executor during startup by default. Remove old `InitializeOnStartup` usage or move warmup work to `AddWarmupTask`. | [Migration guide: Eager initialization](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16#eager-initialization-by-default) and [Warmup](/docs/hotchocolate/v16/server/warmup/) |
| Schema services and application services | Some schema configuration components now require explicit application service registration with `AddApplicationService<T>()`. Resolver service injection is unchanged. | [Migration guide: Schema and application services](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16#clearer-separation-between-schema-and-application-services) and [Dependency Injection](/docs/hotchocolate/v16/resolvers-and-data/dependency-injection/) |
| Request execution APIs | Custom middleware, tests, and extensions may reference renamed or removed abstractions like `IRequestContext`, `IOperationResult`, or `OperationResultBuilder`. | [Migration guide](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16) and [Testing guide](/docs/hotchocolate/v16/guides/testing/) |
| Server options and transport behavior | Server options now favor schema-level configuration via `ModifyServerOptions`. Batching is off by default. Incremental delivery uses a new default response format. | [Migration guide: Server options](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16#server-options-now-configured-via-modifyserveroptions), [Batching](/docs/hotchocolate/v16/server/batching/), and [HTTP transport](/docs/hotchocolate/v16/server/http-transport/) |
| Scalars and value coercion | Some scalar names, mappings, and runtime values changed. This includes `Any` and `Json`, `TimeSpan` and `Duration`, NodaTime, byte arrays, `Uri`, GUID formatting, and `DateTime` serialization. | [Migration guide](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16) and [Scalars](/docs/hotchocolate/v16/building-a-schema/scalars/) |
| Data and pagination | Pagination API changes may affect custom `Page<T>`, edge, cursor, or connection code. Filtering with global IDs also changed if global object identification is not enabled. | [Migration guide: Page and cursor API changes](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16#page-and-cursor-api-changes) and [Pagination](/docs/hotchocolate/v16/resolvers-and-data/pagination/) |
| Observability | OpenTelemetry spans, attributes, and custom instrumentation hooks changed. Dashboards and alerts may need updates even if GraphQL responses are stable. | [Migration guide: Instrumentation](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16#addinstrumentation) and [Instrumentation](/docs/hotchocolate/v16/server/instrumentation/) |
| Schema contract | Generated SDL may change due to naming, scalar, nullability, directive, or option changes. Clients see the schema, not the C# diff. | [Testing GraphQL APIs](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/testing-graphql-apis/) and [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/) |

Keep this table as a risk map. Don’t add every row to your refactor plan until the migration guide or your build points you there.

# What stays the same?

You’ll find plenty of familiar ground:

- The GraphQL schema is still the contract your clients use.
- Queries, mutations, subscriptions, variables, fragments, validation, execution, nullability, and errors all keep their GraphQL meaning.
- ASP.NET Core hosting, dependency injection, endpoint mapping, authentication, authorization, and middleware order remain core parts of your server.
- Package-based setup still matters. Feature packages like `HotChocolate.Data`, `HotChocolate.AspNetCore.Authorization`, and subscription providers still enable specific capabilities.
- Schema snapshots and operation tests are still the best way to prove your clients get the contract and behavior they expect.
- Your domain language should guide your review. Upgrading packages should not rename fields, change meanings, or expose internal details without careful review.

Anchor your review on the schema. A client-first schema review is more valuable than a mechanical C# API search, because clients experience field names, argument types, enum values, nullability, errors, and response shapes.

For design guidance during your review, see [Schema design principles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-design-principles/) and [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/).

# Prepare a baseline before updating packages

Make your upgrade reversible and measurable before you change any dependencies.

## Inventory your Hot Chocolate packages

From each project directory that references Hot Chocolate, run:

```bash
dotnet list package
```

If you use Central Package Management, also check `Directory.Packages.props`.

Record all direct Hot Chocolate references across your application, shared libraries, integration tests, and GraphQL test projects.

| Project | Package | Current version | Target v16 version | Notes |
| --- | --- | --- | --- | --- |
| `Catalog.Api` | `HotChocolate.AspNetCore` | `15.x.x` | `16.x.x` | Main server package |
| `Catalog.Api` | `HotChocolate.Data` | `15.x.x` | `16.x.x` | Filtering, sorting, projections |
| `Catalog.Tests` | `HotChocolate.Execution` or GraphQL test packages | `15.x.x` | Remove, replace, or align per the migration guide | Verify whether the package is still needed |

Checkpoint: you should be able to name every direct `HotChocolate.*` reference and know where its version is set.

## Save your current schema contract

Before changing packages, capture your current schema SDL using your existing snapshot test, schema export, or registry workflow.

When you compare schema output after the upgrade, review these areas:

- Type names
- Field names
- Field nullability
- Argument names, types, defaults, and nullability
- Input object fields
- Enum values
- Directive usage
- Descriptions (when clients or docs depend on them)
- Scalar names and `@specifiedBy` URLs

If you don’t have a schema snapshot yet, create one before upgrading. See [Testing GraphQL APIs](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/testing-graphql-apis/) for the CookieCrumble pattern.

## Choose representative operations

Schema validation answers, “Did the contract change?” Operation validation answers, “Do our critical workflows still behave as expected?”

Pick operations that cover the features your server uses:

| Operation type | Why include it in the upgrade check |
| --- | --- |
| High-traffic query | Proves the main read path still returns the expected `data` shape |
| Critical mutation | Proves input coercion, side effects, errors, and payload shape |
| Authorization | Proves allowed and denied callers still produce expected results |
| Filtering, sorting, paging, or projections | Proves data middleware and generated arguments still behave |
| DataLoader-heavy fields | Proves resolver wiring and batching behavior |
| Custom scalars | Proves variable coercion and result serialization |
| Subscriptions | Proves event payload, authorization, and transport behavior |
| Persisted or trusted operations | Proves registered documents still validate and execute |
| Incremental delivery with `@defer` or `@stream` | Proves clients can consume the selected response format |

Checkpoint: every important client workflow should have at least one operation or manual verification step.

## Plan rollback and review ownership

Before your upgrade branch grows, document:

- Which package changes must be reverted together
- The deployment artifact or container tag to roll back to
- Who approves schema diffs
- Who approves runtime behavior diffs
- Who reviews migration guide items
- Which failures block release

Plan your rollback before updating packages. It’s much harder to recover if you try to recreate your baseline after failures appear.

# Update all Hot Chocolate packages together

Hot Chocolate packages are released as a coordinated set. Update every direct `HotChocolate.*` reference to the same v16 version across all server and test projects involved in the upgrade.

Never mix v16 Hot Chocolate packages with other major versions in the same server.

If you use `.csproj` package references, your result should look like this (with your chosen version):

```xml
<PackageReference Include="HotChocolate.AspNetCore" Version="16.x.x" />
<PackageReference Include="HotChocolate.Data" Version="16.x.x" />
<PackageReference Include="HotChocolate.AspNetCore.Authorization" Version="16.x.x" />
```

If your v16 code uses `[QueryType]`, `[MutationType]`, `[SubscriptionType]`, or `.AddTypes()`, add the analyzer package at the same version:

```xml
<PackageReference Include="HotChocolate.Types.Analyzers" Version="16.x.x" PrivateAssets="all" />
```

The analyzer-generated registration also needs module metadata. Make sure `Properties/ModuleInfo.cs` exists in the project that owns the attributed types:

```csharp
using HotChocolate;

[assembly: Module("Types")]
```

`Module("Types")` names the generated module used by the no-argument `AddTypes()` call. For more schema registration options, see [Building a schema](/docs/hotchocolate/v16/building-a-schema/).

For more on package selection and setup, see [Packages](/docs/hotchocolate/v16/learn/4-installation-and-setup/packages/).

After editing packages, restore and build:

```bash
dotnet restore
dotnet build
```

You should see:

```text
Build succeeded.
```

If restore fails, align all versions before changing application code. If build fails, match each error to [Migrate Hot Chocolate from 15 to 16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16) before refactoring broadly.

Good upgrade commits are focused:

1. Baseline and inventory
2. Package alignment
3. Migration-guide code changes
4. Schema and operation validation updates
5. Documentation or release notes for accepted client-visible changes

# Apply the v15 to v16 migration guide

Read the migration guide from start to finish for your source version. The v15 to v16 guide is the canonical source for breaking changes and code examples. If you’re starting from v14 or earlier, work through each earlier guide in order before the final v15 to v16 pass.

Use this triage pattern as you work:

| Signal | What to do |
| --- | --- |
| Build error about a removed or renamed API | Find the matching migration-guide entry, apply the replacement, rebuild |
| Startup fails during schema initialization | Check eager initialization, schema service registration, package alignment, and scalar registration |
| Tests fail with response or error differences | Compare with runtime behavior changes in the migration guide before updating expectations |
| Schema snapshot changes | Review the diff as a client contract change, not as test noise |
| Observability dashboard changes | Check instrumentation span and attribute changes before changing alerts |

Common v16 migration areas include:

- Eager schema initialization
- Schema-service activation with `AddApplicationService<T>()`
- Request context APIs
- Operation result APIs
- Server options
- Batching defaults
- Scalar changes
- Pagination APIs
- Nitro options
- Instrumentation

Don’t use this page as your breaking-change list. Keep the migration guide open as you edit.

# Validate your schema contract

After aligning packages and applying migration-guide edits, run your schema snapshot or export again.

Start with this question:

> Did clients see a contract change?

Use this schema-diff checklist:

| Diff area | Review question |
| --- | --- |
| Removed type, field, argument, input field, enum value, or directive | Which existing operations break? |
| Renamed type, field, scalar, argument, or enum value | Is there a compatibility path, or is this an approved breaking change? |
| Nullability change | Can old clients still send and read the same operations safely? |
| New required argument or input field | Do existing variables still validate? |
| Scalar mapping or serialization change | Do clients and generated models still parse values? |
| Error extension change | Do clients, logs, snapshots, or alerts depend on the old extension key? |
| Description or deprecation change | Does documentation or discovery need an update? |

Only accept a schema diff if it’s tied to a migration-guide item, an intentional application change, or an approved schema evolution decision.

Checkpoint:

- The schema snapshot is unchanged, or
- Every diff is documented, approved, and communicated to affected consumers

For deeper review mechanics, see [Testing GraphQL APIs](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/testing-graphql-apis/), [Testing guide](/docs/hotchocolate/v16/guides/testing/), and [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/).

# Validate important operations

After the build and schema review pass, run your representative operation tests.

Compare:

- `data` shape
- `errors` shape
- Error `path` values
- Stable values in `extensions`
- Null propagation
- Authorization results
- Filtering, sorting, paging, and projection behavior
- Scalar input and output formats
- Subscription event payloads
- Persisted or trusted operation behavior
- HTTP or WebSocket behavior (when clients depend on transport details)

For example, a v15 to v16 upgrade can change error extension names, batching defaults, scalar output, incremental delivery response format, or instrumentation without changing every field in the schema.

Assign operation failures to one of three outcomes:

| Outcome | Meaning | Next step |
| --- | --- | --- |
| Expected migration difference | The migration guide explains the changed behavior | Update code, tests, clients, or release notes intentionally |
| Application regression | The migration changed your wiring or behavior in a way you did not intend | Fix the server before release |
| Unknown difference | No guide entry or application change explains it | Pause release and investigate before accepting the result |

Use [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/) for partial data or error envelopes. Use [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/) and [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication/) if caller behavior changed. Use [HTTP transport](/docs/hotchocolate/v16/server/http-transport/) and [Endpoints](/docs/hotchocolate/v16/server/endpoints/) if route, content type, GET, batching, file upload, or WebSocket behavior changed.

# Decide if you’re ready to ship

Base your release decision on validation evidence:

| Signal | Meaning | Next step |
| --- | --- | --- |
| All Hot Chocolate packages are aligned to one v16 version | The dependency set is coherent | Keep the package inventory with the upgrade notes |
| Restore and build pass | Compile-time migration work is complete for this slice | Continue to schema and operation validation |
| Schema snapshot is unchanged | Clients should see the same GraphQL contract | Continue to operation validation |
| Schema snapshot changed and every diff is approved | The contract changed intentionally | Document client impact and release timing |
| Important operation tests pass | Key workflows still execute as expected | Prepare release checks |
| Operation failures are explained and assigned | The upgrade is not done, but work is scoped | Fix before shipping or split the release |
| Package conflicts, unexplained schema drift, or unexplained operation failures remain | The upgrade risk is not bounded | Pause or roll back |

Ship only when package alignment is complete, the build passes, migration-guide items are addressed, schema changes are approved, and important operations pass.

Never ship with unexplained schema drift. A renamed field, new required input, scalar change, or error shape change can break production clients even if the server starts.

# Troubleshooting common upgrade issues

| Symptom | Likely cause | Recovery | Verification |
| --- | --- | --- | --- |
| `dotnet restore` reports downgrades or conflicts | One or more `HotChocolate.*` packages are still on another version family | Review every project and central package version. Align all direct Hot Chocolate references to v16 | `dotnet restore` succeeds without downgrade warnings |
| `AddTypes()` is missing | `HotChocolate.Types.Analyzers` is not referenced, not restored, or not on the same v16 version | Add the analyzer package and rebuild | The project builds and generated type registration is available |
| Attributed types are missing from the schema after a successful build | Analyzer module metadata is missing or the generated module is not the one used by `AddTypes()` | Confirm `Properties/ModuleInfo.cs` exists in the project with the attributed types and contains `[assembly: Module("Types")]` | The generated types appear in the schema snapshot or export |
| Startup now fails before the first request | v16 builds the schema and executor during startup by default | Fix the schema error, remove old `InitializeOnStartup`, or use `AddWarmupTask` for warmup work | The app starts and reaches the GraphQL endpoint |
| A diagnostic listener, error filter, interceptor, or optimizer cannot resolve an app service | Schema services need explicit access to selected application services | Register the required service with `.AddApplicationService<T>()` where the migration guide requires it | Startup succeeds and the component receives the expected service |
| Batched requests stop working | Batching is disabled by default in v16 | Enable batching intentionally with `ModifyServerOptions` if clients require it | A representative batch request returns the expected response |
| Schema snapshot changes unexpectedly | Generated schema output changed, packages are mismatched, or unrelated app changes entered the branch | Compare fields, nullability, arguments, enum values, directives, descriptions, and scalars with the migration guide | Every diff is accepted or fixed |
| Operation tests fail after schema approval | Runtime behavior changed outside the SDL, such as scalar coercion, error extensions, authorization, batching, or result formatting | Compare the failing response with migration-guide entries and feature docs | Tests pass or failures are tracked as migration tasks |
| Dashboards or alerts lose GraphQL attributes | Instrumentation spans and attributes changed in v16 | Update dashboard queries and custom enrichers using the migration guide | Observability checks report the expected operation and error data |
| Rollback path is unclear | The upgrade started without a package, schema, and deployment baseline | Recreate the inventory and baseline before continuing | The team knows which package and deployment changes to revert |

# Where to find migration notes and related guides

Use these links for your next steps:

- [Migrate Hot Chocolate from 15 to 16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16) is the authoritative v15 to v16 migration guide.
- For v14 or older servers, follow every intervening guide in order: [10 to 11](/docs/hotchocolate/v16/migrating/migrate-from-10-to-11), [11 to 12](/docs/hotchocolate/v16/migrating/migrate-from-11-to-12), [12 to 13](/docs/hotchocolate/v16/migrating/migrate-from-12-to-13), [13 to 14](/docs/hotchocolate/v16/migrating/migrate-from-13-to-14), [14 to 15](/docs/hotchocolate/v16/migrating/migrate-from-14-to-15), and [15 to 16](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16).
- [Packages](/docs/hotchocolate/v16/learn/4-installation-and-setup/packages/) explains v16 package selection, feature packages, analyzer package usage, and version alignment.
- [Existing ASP.NET Core app](/docs/hotchocolate/v16/learn/4-installation-and-setup/existing-aspnet-core-app/) helps when endpoint registration, middleware order, or `MapGraphQL()` must fit beside existing routes.
- [Testing GraphQL APIs](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/testing-graphql-apis/) helps you choose schema snapshots, operation tests, transport tests, and guardrail tests.
- [Testing guide](/docs/hotchocolate/v16/guides/testing/) covers the mechanics of building a test executor and executing operations.
- [Schema evolution](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-evolution/) helps you classify client-visible schema changes.
- [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/) explains GraphQL `data` and `errors` behavior for response validation.
- [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization/), [Request Limits](/docs/hotchocolate/v16/securing-your-api/request-limits/), [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis/), and [Public API guide](/docs/hotchocolate/v16/guides/public-api/) help validate production guardrails.
- [Coming from another stack](/docs/hotchocolate/v16/learn/5-coming-from/) lets you route teammates who are moving from REST, OData, Apollo Server, GraphQL.NET, or another background.

You’re ready to move forward when you have an aligned v16 package set, a passing build, reviewed migration-guide items, an approved schema result, passing representative operations, and a documented rollback plan.
