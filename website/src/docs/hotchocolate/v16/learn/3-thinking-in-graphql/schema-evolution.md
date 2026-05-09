---
title: "Schema evolution"
description: "Evolve a Hot Chocolate v16 schema without surprising clients by classifying changes, using deprecation, validating operations, and coordinating rollout."
---

```graphql
type Query {
  product(id: ID!): Product
  productById(id: ID!): Product
}

type Product {
  id: ID!
  name: String!
}
```

The safest GraphQL change is often an addition that lets old and new operations work at the same time. In this example, existing clients can keep calling `product`, while new clients move to `productById`.

A GraphQL schema is a living client contract. It defines the types, fields, arguments, input objects, enum values, directives, descriptions, and nullability rules that clients build operations against. Hot Chocolate can generate much of that schema from .NET code, but the schema is the public contract clients observe.

# Evolve the schema clients already use

GraphQL favors one evolving schema over URL-based API versions. Clients select the fields they need, so you can often add a new field, argument, type, enum value, or payload field without changing existing operations.

That does not mean every addition is safe for every consumer. A new enum value can break generated clients that use exhaustive switching. A new field can expose data that needs authorization, cost limits, or opt-in discovery. A new argument can become a breaking change if it is required.

Use existing operations as the compatibility boundary. A schema change is breaking when it makes an existing valid operation fail, validate differently, deserialize differently, or mean something different to the client.

Before you merge a schema change, ask:

> Would any existing query, mutation, persisted operation, generated client, dashboard, or partner integration need to change on release day?

If the answer is yes, use an additive replacement, a migration plan, or an approved breaking release.

# Know who depends on the current schema

Before you change a field, find the blast radius. A schema can have known consumers such as first-party apps and unknown consumers such as partners, scripts, dashboards, or public API users.

Use more than one evidence source when possible:

| Evidence source | What it can show | Watch out for |
| --- | --- | --- |
| Checked-in operations | Which fields current first-party clients select | Runtime clients may still send older documents. |
| Persisted or trusted operations | Which operation documents are allowed or published | Variable values, client versions, and active rollout state still matter. |
| Request logs and telemetry | Which operations run in production | Anonymous or dynamic operations may be hard to map to owners. |
| Client names and versions | Which app and release sent the operation | Require consistent headers such as `GraphQL-Client-Id` and `GraphQL-Client-Version`. |
| API keys or caller identity | Which partner, tenant, or integration uses the field | One owner may operate several applications. |
| Schema and client registries | Which schema coordinates, operations, clients, and stages are affected | Registry data helps most when teams publish schemas and operations consistently. |

Nitro can provide a shared view when the change crosses team boundaries. Use the [Nitro schema registry](/docs/nitro/apis/schema-registry/) for schema history and schema checks, the [Nitro client registry](/docs/nitro/apis/client-registry/) for client versions and known operations, and [Nitro operation reporting](/docs/nitro/apis/operation-reporting/) for observed operation usage.

For public APIs, assume some clients will not upgrade quickly. Deprecated fields that are hidden by default in tooling may not create enough migration pressure by themselves. Communicate the change through release notes, owner follow-up, examples, and support channels.

Use this worksheet before approving a removal:

| Schema coordinate | Known operations | Client owner | Generated client impact | Telemetry signal | Migration status |
| --- | --- | --- | --- | --- | --- |
| `Query.product` | `GetProductPage`, `CartLineItem` | Web, mobile | Generated query APIs use `product` | 18 percent of product traffic | Replacement shipped, mobile migration pending |

# First classify the change

Start every review by labeling the change. Exact impact depends on generated clients, persisted operations, mobile release cycles, gateway composition, and whether you control the consumers.

| Planned change | Classification | Safer alternative | Verify with |
| --- | --- | --- | --- |
| Add `Product.displayName: String!` | Usually safe | Add with description, authorization review, and cost review | Schema snapshot, operation tests, generated-client refresh |
| Rename `product` to `productById` | Breaking | Add `productById`, deprecate `product`, migrate, then remove | Schema diff, known operation validation, usage data |
| Add `locale: String!` to `search` | Breaking | Add `locale: String = "en"` or `locale: String` first | Old search operations validate unchanged |
| Add `locale: String = "en"` to `search` | Usually safe | Keep the default stable and document it | Operation tests with old and new variables |
| Add `DISCONTINUED` to an output enum | Dangerous | Coordinate with generated-client owners and document unknown-value handling | Representative client regeneration and compile checks |
| Change `name: String` to `name: String!` | Dangerous | Prove the resolver guarantee and error behavior before tightening | Nullability review, operation tests, generated-client refresh |
| Change `name: String!` to `name: String` | Dangerous or breaking for generated clients | Add a new field if null now has a new meaning | Generated-client compile checks and client review |
| Change default sort of `products` | Dangerous | Add an explicit argument or a new field for the new order | Operation tests and release notes |
| Change `price` from seller currency to buyer-localized currency | Breaking semantic change | Add `localizedPrice` or a `currency` argument | Client review and old operation expectations |
| Remove an enum value, field, argument, or input field | Breaking | Deprecate, migrate, monitor usage, then remove | Schema checks, operation registry, telemetry |

Use the [GraphQL specification type system](https://spec.graphql.org/October2021/#sec-Type-System) as the baseline for how schemas, types, fields, arguments, enum values, and directives behave. Use Hot Chocolate and Nitro checks to apply that model to your actual schema and operations.

Nitro schema checks can classify proposed schema changes as safe, dangerous, or breaking against registered schemas and known operations. Use the [schema registry](/docs/nitro/apis/schema-registry/) and the [Nitro schema CLI commands](/docs/nitro/cli-commands/schema/) when you need that review in CI.

# Prefer expand, migrate, then contract

Use expand, migrate, then contract for most public or shared schema changes.

1. **Expand** the schema by adding the replacement alongside the old shape.
2. **Migrate** clients by documenting the replacement, updating first-party operations, notifying owners, and watching usage.
3. **Contract** by removing the old shape only after usage is gone or a breaking release is approved.

```graphql
type Query {
  product(id: ID!): Product @deprecated(reason: "Use productById instead.")
  productById(id: ID!): Product
}

type Product {
  id: ID!
  name: String!
}
```

During the transition, both operations should validate and return expected data:

```graphql
query OldProduct($id: ID!) {
  product(id: $id) {
    id
    name
  }
}
```

```graphql
query NewProduct($id: ID!) {
  productById(id: $id) {
    id
    name
  }
}
```

Keep old behavior stable while migration is in progress. Point descriptions, docs, examples, starter templates, and new client snippets at the replacement. Avoid reusing names when the meaning changes.

For arguments, add optional arguments or defaults before you require new input:

| Instead of | Prefer first | Why |
| --- | --- | --- |
| `search(text: String, locale: String!)` | `search(text: String, locale: String = "en")` | Old operations continue to validate. |
| `createOrder(input: CreateOrderInput!)` with new required input field | Add a nullable input field or defaulted input field first | Existing variables remain valid. |

# Do not change meaning under the same name

A field name carries meaning, not only a type. Clients encode that meaning in UI labels, generated types, normalized cache policies, analytics, tests, and business workflows.

If clients would explain the field differently after your change, add a new field or argument instead.

| Current contract | Hidden change | Safer direction |
| --- | --- | --- |
| `Product.price` means seller catalog currency | It now means buyer-localized currency | Add `localizedPrice` or `price(currency: CurrencyCode!)`. |
| `Order.createdAt` is stored and returned in UTC | It now uses the buyer's timezone | Add `createdAtLocal` or an explicit timezone argument. |
| `Query.products` returns newest products first | It now sorts by relevance | Add an `orderBy` argument with a default, or add `recommendedProducts`. |
| `Product.id` is a stable global ID | It changes encoding or identity scope | Add a new identifier field and migrate caches deliberately. |
| `Product.available` means inventory exists | It now means purchasable by this viewer | Add `isPurchasable` and keep `available` stable. |

You can move the old field to new infrastructure internally, but preserve the old contract. Do not recycle removed names for new concepts. A name that used to mean one thing may live for years in old client code, analytics, docs, and support workflows.

# Use deprecation to make migration visible

Deprecation is communication plus telemetry. It is not removal by itself.

Use deprecation when a replacement exists and clients can migrate to it. Hot Chocolate v16 supports deprecating output fields, arguments, input fields, and enum values. You can use `[GraphQLDeprecated]`, `[Obsolete]`, or fluent `.Deprecated("reason")` in code-first configuration. The resulting schema uses the GraphQL `@deprecated` directive.

```graphql
type Query {
  product(id: ID!): Product
    @deprecated(reason: "Use productById instead. Update queries before 2025-06-30.")

  productById(id: ID!): Product
}

type Product {
  id: ID!
  name: String!
}
```

A useful deprecation reason includes:

| Include | Example |
| --- | --- |
| Reason for the change | "The old lookup name is ambiguous." |
| Replacement coordinate or pattern | "Use `Query.productById`." |
| Client migration action | "Select the same `Product` fields from `productById`." |
| Sunset or removal window, if known | "Remove after 2025-06-30." |
| Longer migration link, if available | "See the June catalog API release notes." |

Deprecated elements remain functional. Tools can show them through introspection when deprecated members are included. Some tools hide deprecated members by default, so verify that consumers can discover the warning.

Required arguments and required input fields cannot be safely deprecated without a default value. If a client must provide the value for the operation to validate, hiding or discouraging it before a replacement path exists creates a trap.

For exact Hot Chocolate syntax, see [Versioning](/docs/hotchocolate/v16/building-a-schema/versioning/). For schema descriptions that make migration paths clearer, see [Documentation](/docs/hotchocolate/v16/building-a-schema/documentation/).

# Treat input changes as the highest-risk surface

Input changes often break validation immediately because clients send arguments and variables into a fixed schema shape.

| Change | Classification | Why |
| --- | --- | --- |
| Add a required argument | Breaking | Existing operations that omit the argument no longer validate. |
| Add a nullable argument | Usually safe | Existing operations can omit it. |
| Add an argument with a default | Usually safe | Existing operations receive the documented default behavior. |
| Make an optional input field required | Breaking | Existing variables can become invalid. |
| Remove an argument or input field | Breaking for clients that send it | Validation rejects unknown or missing input. |
| Change a default value | Dangerous or semantic break | Old operations still validate but may behave differently. |

Prefer evolving mutations through optional input fields, new payload fields, and typed domain errors rather than in-place command changes.

Before:

```graphql
type Mutation {
  cancelOrder(input: CancelOrderInput!): CancelOrderPayload!
}

input CancelOrderInput {
  orderId: ID!
}

type CancelOrderPayload {
  order: Order
  errors: [CancelOrderError!]!
}

type Order {
  id: ID!
}

type CancelOrderError {
  message: String!
}
```

Safer expansion:

```graphql
input CancelOrderInput {
  orderId: ID!
  reason: OrderCancellationReason
}

type CancelOrderPayload {
  order: Order
  errors: [CancelOrderError!]!
  auditId: ID
}

type Order {
  id: ID!
}

type CancelOrderError {
  message: String!
}

enum OrderCancellationReason {
  CUSTOMER_REQUEST
}
```

Risky expansion:

```graphql
input CancelOrderInput {
  orderId: ID!
  reason: OrderCancellationReason!
}

enum OrderCancellationReason {
  CUSTOMER_REQUEST
}
```

The risky version breaks existing variables that only contain `orderId`. Add optional input first, migrate clients, then require the value only in a breaking release or a new mutation.

# Handle nullability, enums, and abstract types with extra care

Some schema changes look small in SDL but cause large client changes.

## Review nullability changes

Output nullable to non-null can be validation-compatible, but it is dangerous if the resolver cannot always uphold the guarantee. A failed non-null field can change error propagation and remove parent data under traditional GraphQL execution.

Output non-null to nullable can break generated clients that stopped checking for `null`. Input nullable to non-null is breaking because existing variables may become invalid.

Use the [Nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/) and [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/) pages when you review successful data, execution errors, and null propagation.

## Review enum changes

Adding an output enum value is often classified as dangerous rather than breaking. Existing operations still validate, but generated clients may use exhaustive switch handling.

```graphql
enum OrderStatus {
  PLACED
  SHIPPED
  CANCELED
  RETURNED
}
```

If clients generated code when the enum only had `PLACED`, `SHIPPED`, and `CANCELED`, the new `RETURNED` value may need a client release. Coordinate unknown-value handling before the server returns the new value.

Removing an enum value is breaking for clients that use it in variables, fragments, generated types, or business logic.

## Review interfaces and unions

Adding a possible object type to an interface or union can affect clients that treat the set of possible types as exhaustive.

Ask these questions before changing nullability, enums, interfaces, or unions:

- Will generated types change?
- Will exhaustive handling still compile?
- Will null propagation or `onError` behavior change?
- Will old operations still receive the same kind of result?
- Will cached records still have stable identity and type names?

For generated-client responsibilities, read [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/).

# Gate unstable additions when clients must opt in

Additive does not always mean ready for every consumer. Experimental, expensive, preview, or unstable fields may need opt-in discovery so clients make a deliberate choice before they depend on them.

Hot Chocolate v16 supports `@requiresOptIn` and related APIs for schema elements that are not part of the default introspection surface. Opt-in is a rollout tool for additions. It is not a substitute for good naming, authorization, cost analysis, descriptions, or a migration plan.

Use opt-in when:

| Scenario | Consider |
| --- | --- |
| A recommendation field is useful but expensive and still changing | Gate it behind an opt-in feature while you measure cost and semantics. |
| A preview field is available to selected partners | Combine opt-in with authorization and clear stability labels. |
| A field is stable and generally supported | Promote it deliberately, update docs, and remove the opt-in requirement through a reviewed schema change. |

For exact syntax and introspection behavior, see [Versioning](/docs/hotchocolate/v16/building-a-schema/versioning/). For production guardrails around expensive additions, see [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis/).

# Coordinate migration before removal

Removal is an organizational decision as well as a technical change. The timeline depends on consumer control, support commitments, mobile release cycles, partner contracts, and production evidence.

Use this removal plan:

| Step | Done when |
| --- | --- |
| Replacement shipped | The new field, argument, type, enum value, or payload shape works in production. |
| Deprecation visible | The old coordinate has a clear reason and replacement. |
| Docs and examples updated | New examples no longer teach the deprecated shape. |
| Owners notified | First-party, partner, mobile, generated-client, and support owners know the plan. |
| Usage monitored | Persisted operations, operation logs, telemetry, client versions, or registry checks show remaining use. |
| Brownout decision recorded, if used | Support plan, rollback criteria, approval, and communication are explicit. |
| Removal approved | Usage is gone or the team accepts a breaking release. |

Brownouts can help when communication and owner follow-up have failed, but they are disruptive. Use them only with approval, a support plan, clear rollback criteria, and strong evidence that the risk is acceptable.

Nitro can help identify coordinate usage, client usage, and active client versions before removal. Start with the [schema registry](/docs/nitro/apis/schema-registry/), [client registry](/docs/nitro/apis/client-registry/), and [operation reporting](/docs/nitro/apis/operation-reporting/).

A useful release note or issue includes:

| Field | Example |
| --- | --- |
| Old coordinate | `Query.product` |
| Replacement | `Query.productById` |
| Reason | "The old name is ambiguous with SKU lookup." |
| Deadline | "Remove no earlier than 2025-06-30." |
| Owner | "Catalog API team" |
| Verification signal | "No registered operations or production traffic for 30 days." |

# Run schema and operation checks before you ship

Do not rely on memory for schema compatibility. Put checks in CI and keep the review evidence close to the pull request.

Start with a CookieCrumble schema snapshot:

```csharp
using CookieCrumble.HotChocolate;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public sealed class SchemaTests
{
    [Fact]
    public async Task Schema_Should_Not_Change_Unexpectedly()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .BuildRequestExecutorAsync();

        // assert
        executor.Schema.MatchSnapshot();
    }
}
```

The snapshot fails when the generated SDL changes. Treat the diff as an API review prompt. If the change is intentional, explain it and update the snapshot after review. If the change is accidental, fix the schema registration, resolver binding, nullability, or descriptor configuration.

When a test builds a schema definition directly, snapshot the printed SDL:

```csharp
schema.Print().MatchSnapshot();
```

Then verify the executable contract:

| Check | What good looks like |
| --- | --- |
| Schema snapshot | SDL diff contains only intended additions, deprecations, or reviewed changes. |
| Schema diff or registry check | Changes are classified as safe, dangerous, or breaking. |
| Known operation validation | Persisted operations and representative client documents still validate. |
| Operation tests | Important queries and mutations return the expected `data` and `errors` shape. |
| Generated clients | Representative clients compile or have planned migration changes. |
| Composed graph checks | Gateway or Fusion composition still produces the intended public graph. |
| Review notes | Dangerous changes have owner sign-off and rollout notes. |

Use [Testing GraphQL APIs](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/testing-graphql-apis/) for schema snapshots and operation tests. Use [Caching and operation contracts](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/caching-and-operation-contracts/) for persisted operations, trusted documents, and operation contracts.

Use Nitro when checks need shared approval or environment awareness:

- [Nitro schema registry](/docs/nitro/apis/schema-registry/) and [schema CLI](/docs/nitro/cli-commands/schema/) for schema validation, schema history, and change classification.
- [Nitro client registry](/docs/nitro/apis/client-registry/) and [client CLI](/docs/nitro/cli-commands/client/) for active client versions and known operations.
- [Nitro stages](/docs/nitro/apis/stages/) and [deployments](/docs/nitro/apis/deployments/) for environment-specific rollout and release history.

In federated or gateway setups, check the composed schema impact and downstream contract impact. For Hot Chocolate v16 composition guidance, start with [Fusion](/docs/hotchocolate/v16/fusion/).

# Use versioning only when one schema cannot serve both clients

Do not start with URL-per-version as the default GraphQL strategy. Most changes should flow through additions, deprecations, usage tracking, and coordinated removal.

Parallel schemas can be valid when one evolving schema cannot serve both old and new clients:

- contractual external API versions,
- separate products with different contracts,
- incompatible domain model replacement,
- long-lived clients that cannot migrate,
- regulatory or partner commitments that require a frozen contract.

Parallel schemas multiply documentation, tooling, tests, authorization review, cost analysis, support, and migration work. Before you create one, ask:

> Can old and new clients share one schema during migration?

If yes, use the additive and deprecation path. If no, treat the parallel schema as an architecture decision with owners, support windows, and retirement criteria.

Keep Hot Chocolate framework upgrade migration separate from consumer schema evolution. Framework migration pages explain changes between Hot Chocolate releases. This page focuses on the GraphQL contract your clients use.

# Troubleshoot schema evolution problems

| Symptom | Likely cause | Fix direction | Verification |
| --- | --- | --- | --- |
| A client query fails after adding an argument | The argument was required or had no default | Make it optional or provide a default, then plan a later required change | Old operations validate unchanged |
| A schema snapshot fails | The public GraphQL contract changed | Review the SDL diff before updating the snapshot | Pull request explains the intended change |
| Generated clients break after a nullability change | Generated types changed even though resolver data still looks compatible | Treat the change as dangerous or breaking and coordinate regeneration | Representative clients compile |
| Clients behave differently but the SDL did not change | Resolver semantics, default order, authorization, units, or null meaning changed | Restore old behavior or add a replacement field or argument | Old and new operation expectations are tested |
| Deprecated field does not appear in tooling | Tooling hides deprecated members or introspection omitted deprecated items | Enable deprecated display or include deprecated members in the introspection query | Clients can see the deprecation and replacement |
| Cannot deprecate an input field or argument | It is non-null without a default value | Add a safe replacement or default first | Old operations still validate |
| Schema diff reports enum addition as risky | Some clients use exhaustive enum handling | Coordinate unknown-value handling and client releases | Representative clients handle the new value |
| Gateway check fails although the subgraph change looked additive | Composition, ownership, inaccessible fields, or downstream contract rules changed | Inspect composed schema diff and gateway guidance | Composed graph checks pass |
| Deprecated field still has traffic after the removal date | Owners were missed, mobile releases lag, or examples still use the field | Identify callers, update examples, extend the plan, or approve a breaking release | Usage is gone or exception is documented |

# Review checklist for a schema change

Use this checklist before you merge:

- Identify the changed schema coordinates.
- Identify known consumers, operation usage, client versions, and owners.
- Classify each change as safe, dangerous, or breaking.
- Prefer additive replacement when behavior or meaning changes.
- Keep old behavior stable during migration.
- Add clear descriptions and deprecation reasons.
- Update docs, examples, generated clients, and first-party operations.
- Run the CookieCrumble schema snapshot test.
- Run schema diff, known-operation validation, generated-client checks, and composed schema checks when they apply.
- Use Nitro schema registry, client registry, stages, deployments, and operation reporting when the review needs shared approval, known-operation validation, active client-version impact, deployment history, or usage-guided removal evidence.
- Prepare release notes and client communication.
- Remove only when usage is gone or a breaking release is approved.

# Next steps

- Revisit [Schema design principles](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/schema-design-principles/) before you add new surface area.
- Review [Nullability](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/nullability/), [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors/), and [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients/) for the client impact of type changes.
- Add or update schema snapshot and operation tests with [Testing GraphQL APIs](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/testing-graphql-apis/).
- Use [Versioning](/docs/hotchocolate/v16/building-a-schema/versioning/) for Hot Chocolate deprecation and opt-in syntax.
- Use the [Public API guide](/docs/hotchocolate/v16/guides/public-api/) or [First-party API guide](/docs/hotchocolate/v16/guides/private-api/) when the rollout needs production governance.
