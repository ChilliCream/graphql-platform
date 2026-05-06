---
path: "/blog/2026/05/11/hot-chocolate-16"
date: "2026-05-11"
title: "What's new for Hot Chocolate 16"
description: "Hot Chocolate 16 brings a new type system, better scalar contracts, safer defaults, improved batching, semantic introspection, and a new GraphQL error mode."
tags: ["hotchocolate", "graphql", "dotnet", "aspnetcore"]
featuredImage: "header.png"
author: Michael Staib
authorUrl: https://github.com/michaelstaib
authorImageUrl: https://avatars1.githubusercontent.com/u/9714350?s=100&v=4
---

Ages have passed since we last shipped a new major version of our platform, and after 1.2 years we have finally arrived with a new generation. There is so much to talk about across version 16 that we did not want to end up with a Stephen-Toub-sized blog post. So today we will focus on Hot Chocolate.

Some releases are incremental, some are mostly technical. Hot Chocolate 13, 14, and 15 each pushed things forward in places, but the platform itself stayed roughly where it was. Hot Chocolate 16 is different. This is the release where we dared to rearchitect the type system, and almost everything else in this post follows from that.

## The new type system

In previous versions we introduced a small library called **HotChocolate.Skimmed** for editing SDL. It offered a rich type system API similar to Hot Chocolate's core, but with one important difference: the type system was mutable and deliberately allowed invalid intermediate states.

The idea behind Skimmed was to provide a modern API for Fusion composition and Strawberry Shake client generation. For Fusion we initially considered spinning up yet another type system, tuned specifically to the gateway's needs. That pushed us into a conundrum: duplicating not just the type systems but also validation, execution, and everything around them would have left us with so much maintenance that we would have been stuck.

So instead, we introduced a new abstraction, `HotChocolate.Types.Abstractions`, that describes the basics. On top of it we rewrote everything we had built over the years: validation, execution, and the rest. We then implemented the abstraction for Skimmed (now `HotChocolate.Types.Mutable`), `HotChocolate.Types`, and `HotChocolate.Fusion`. Each has its own extras, but shared concerns like the IBM Cost spec now work across all three. That makes it easy to write things like analyzers that take a GraphQL SDL, transform it, and then run it through validation.

## Scalars

As part of this rewrite, we also tackled the scalar API.

If you have ever written a custom scalar in Hot Chocolate, you have probably noticed that the surface area is, let's say, ambitious. There is `Serialize`, `Deserialize`, `ParseLiteral`, `ParseValue`, `ParseResult`, `IsInstanceOfType`, `TryDeserialize`, and getting them all to agree with each other was a small art form. The mental model was never quite clean, and that bled into every custom scalar you wrote.

In Hot Chocolate 16, we redesigned the scalar API to align with the GraphQL reference implementation.

```csharp
public sealed class PolicyType : ScalarType<Policy, StringValueNode>
{
    public PolicyType(string name, BindingBehavior bind = BindingBehavior.Explicit)
        : base(name, bind)
    {
    }

    // Construct the runtime value from a string literal.
    protected override Policy OnCoerceInputLiteral(StringValueNode valueLiteral)
        => new(valueLiteral.Value);

    // Construct the runtime value from a variable value.
    protected override Policy OnCoerceInputValue(JsonElement inputValue, IFeatureProvider context)
        => new(inputValue.GetString()!);

    // Serialize the runtime value into the GraphQL response format.
    protected override void OnCoerceOutputValue(Policy runtimeValue, ResultElement resultValue)
        => resultValue.SetStringValue(runtimeValue.Value);

    // Construct a GraphQL literal from the runtime value, used for introspection.
    protected override StringValueNode OnValueToLiteral(Policy runtimeValue)
        => new(runtimeValue.Value);
}
```

That is it. It is a breaking change, but the gain in clarity is well worth it.

## scalars.graphql.org

For a long time, the contract of a GraphQL scalar was mostly its name. That is a weak contract for something like `DateTime`. Two servers could both expose a `DateTime` scalar and mean slightly different things, and clients had very little to go on beyond convention and documentation.

That is the gap [scalars.graphql.org](https://scalars.graphql.org/) is meant to close. It gives scalar authors a place to publish precise specifications that servers can attach to their schemas through the `@specifiedBy` directive. ChilliCream and Apollo helped define a number of those specs, and while doing that work we took a hard look at the scalars we ship in Hot Chocolate.

The result in v16 is fewer built-in scalars, but much better ones. The scalars we keep now follow published specifications and expose them directly in the schema. That means API consumers can inspect a schema, understand exactly what a scalar means, and handle it correctly on the client side instead of guessing from the name alone.

The full list, with diffs, lives in the [migration guide](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16).

## Date and time scalars

Date and time handling was one of the places where older Hot Chocolate versions were too lax. Moving from the built-in .NET types to NodaTime, or back again, was effectively a breaking API change because we let implementation details bleed into the client-facing schema. If you used the standard scalars you got one vocabulary. If you used `HotChocolate.Types.NodaTime`, you got another, with types like `OffsetType`, `InstantType`, and `ZonedDateTimeType`. Clients had to know which world they were in.

That was the wrong contract. Your schema should describe the meaning of the data, not which date and time library your server happens to use internally. In v16 we tightened things around a small set of well-specified scalars: `DateTime` for timestamps, `LocalDate` for calendar dates, `LocalTime` for clock times, `LocalDateTime` for local timestamps, and `Duration` for durations. These now follow the published scalar specifications and their ISO 8601 based formats, so the contract is precise and portable.

That also changes what `HotChocolate.Types.NodaTime` does. It no longer introduces a parallel, NodaTime-specific schema vocabulary. Instead, it plugs NodaTime in as an alternative runtime representation of those same GraphQL scalars, with the extra precision and correctness NodaTime is known for. You can move between the built-in .NET types and NodaTime without changing the schema your clients see. One `AddNodaTime()` call and you are set up:

```csharp
builder
    .AddGraphQL()
    .AddNodaTime();
```

We are already looking at a few more date and time mappings for the v16.x line. We have not made up our minds yet, so if there is a type you care about, please tell us.

## A new batching engine

Efficient batching is one of those things that sounds simple until you build a GraphQL server. DataLoader is still a great tool, but it can feel heavy for straightforward scenarios because you have to split one piece of data-fetching logic across a resolver and a DataLoader.

In v16 we reworked the batching engine from the ground up. Execution is now more predictable, and transport-level batch requests are no longer treated as a set of isolated executions that just happen to arrive together. Instead, Hot Chocolate can fold them into a single execution with a shared batching session, which means overlapping work can naturally collapse into fewer round-trips.

We also wanted to make common batching scenarios easier to express. That is why Hot Chocolate 16 introduces batch resolvers. Instead of wiring together a resolver and a DataLoader, you can now write a single resolver that receives all parent objects for a field at once:

```csharp
[BatchResolver]
public static async Task<List<int>> GetProductCountAsync(
    [Parent(requires: nameof(Brand.Id))] List<Brand> brands,
    [Service] CatalogContext context,
    CancellationToken cancellationToken)
{
    var brandIds = brands.Select(b => b.Id).ToList();

    var counts = await context.Products
        .Where(p => brandIds.Contains(p.BrandId))
        .GroupBy(p => p.BrandId)
        .Select(g => new { BrandId = g.Key, Count = g.Count() })
        .ToDictionaryAsync(g => g.BrandId, g => g.Count, cancellationToken);

    return brands.Select(b => counts.GetValueOrDefault(b.Id, 0)).ToList();
}
```

To the consumer, this still looks like a simple field in the schema:

```graphql
type Brand {
  productCount: Int
}
```

So when we run a query like this:

```graphql
{
  brands(first: 5) {
    nodes {
      productCount
    }
  }
}
```

`GetProductCountAsync` runs once for the five brands in the result set and returns all counts in one go.

Does this make DataLoaders obsolete? Not at all. DataLoaders are still the right tool when you want batching to live in your application layer instead of in a resolver. They let you define normalized data-fetching primitives once, reuse them across resolvers, and still get automatic batching and deduplication.

## Variable and Request Batching

We have also been investing in the emerging [batching proposal for GraphQL over HTTP](https://github.com/graphql/graphql-over-http/pull/307). In v16, transport batches are folded into a single work scheduler, so variable batching and request batching are executed as if the work had been sent as one colocated request. The result is that batching is no longer just a transport trick, it behaves like a first-class execution mode.

Variable batching lets you execute the same operation multiple times with different variable sets in one HTTP request. Request batching lets you send multiple independent GraphQL operations in the same HTTP request. The two compose naturally, so a single batch can contain regular requests and variable-batched requests side by side.

Here is what that looks like over the wire:

```json
[
  {
    "query": "query GetProduct($id: ID!) { productById(id: $id) { name } }",
    "operationName": "GetProduct",
    "variables": { "id": "1" }
  },
  {
    "query": "query GetProduct($id: ID!) { productById(id: $id) { name } }",
    "operationName": "GetProduct",
    "variables": [{ "id": "2" }, { "id": "3" }]
  }
]
```

And here is a JSON Lines response stream:

```text
{"data":{"productById":{"name":"Cup"}},"requestIndex":1,"variableIndex":0}
{"data":{"productById":{"name":"Hat"}},"requestIndex":0}
{"data":{"productById":{"name":"Plate"}},"requestIndex":1,"variableIndex":1}
```

Results can arrive out of order as soon as they are ready. `requestIndex` tells you which request in the outer array a result belongs to, and `variableIndex` identifies which variable set within a variable-batched request produced that result.

## Semantic Introspection

I am not going to cover all of our AI-focused work in this post, because several of those features deserve their own write-up. One addition is worth a quick mention here though: Semantic Introspection.

Classic GraphQL introspection is great when a client wants to inspect the whole schema. For agents, that is often too blunt. They usually do not want the whole schema, they want the right part of the schema for the task in front of them. Dumping thousands of fields into the model costs tokens, pollutes context, and still leaves the model to figure out what matters. On top of that, many enterprise GraphQL schemas are simply too large to fit comfortably, even in a 1M-token context window.

Semantic Introspection turns schema discovery into a search problem. With `__search`, an agent can ask for the capabilities that are relevant to a user task and get back the best matching types and fields, together with the paths that lead to them. With `__definitions`, it can then fetch just the precise schema details it needs to build the next query. That is what makes it cool: GraphQL keeps its precision, while discovery becomes a constant-shape two-step process that works the same whether your schema has 10 types or 1000. In Pascal's measurements, that also made it markedly more cost-efficient than the other discovery approaches he compared.

You can enable it explicitly like this:

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(o => o.EnableSemanticIntrospection = true);
```

By default, Hot Chocolate indexes the schema with BM25, so there is nothing else to wire up. If you want the full story, including how `__search` and `__definitions` work in practice, Pascal goes much deeper in [Semantic Introspection](/blog/2026/04/22/semantic-introspection). And if you want to see the agent side of it, including the skill prompt that teaches an agent how to use semantic introspection effectively, take a look at the [GraphQL skill prompt](https://github.com/PascalSenn/apidays-singapore/blob/main/case-study/prompt-graphql-skill.md).

## A new error mode for GraphQL

Hot Chocolate 15 had experimental support for `@semanticNonNull`. That was never meant to be the final answer. It was a bridge while the GraphQL community worked toward a proper way to express error handling.

The underlying issue is null propagation. In GraphQL today, if a non-null field errors, that error can bubble up and erase part of the response tree. For clients built around colocated fragments, that is especially painful because an error in one component can wipe out data that belongs to sibling components. The error is no longer contained, it bleeds across the selection set.

Hot Chocolate 16 moves to the new [`onError` proposal](https://github.com/graphql/graphql-spec/pull/1163). Instead of baking the behavior into the schema, the client can ask for it on the request. If you want clients to opt in per request, enable overrides:

```csharp
builder
    .AddGraphQL()
    .ModifyRequestOptions(o => o.AllowErrorHandlingModeOverride = true);
```

A client can then send:

```json
{ "query": "...", "onError": "NULL" }
```

With `onError: "NULL"`, Hot Chocolate stops null propagation, returns `null` at the field that failed, and still reports the error. That lets the client decide how to contain the failure, for example at the fragment or component boundary, instead of letting one error erase neighboring parts of the response. If you want that behavior for every request, set `DefaultErrorHandlingMode = ErrorHandlingMode.Null`.

We still have clients that understand `@semanticNonNull` but do not support `onError` yet. For those clients, you can enable the new error mode on the server and expose a `@semanticNonNull` schema. That way the runtime behavior matches the semantics the compatibility schema advertises.

```csharp
app.MapGraphQLSchema();
app.MapGraphQLSemanticNonNullSchema();
```

The same trick is available from the CLI via `schema export --semantic-non-null` or programmatically with our `SchemaFormatter` by setting `RewriteToSemanticNonNull = true`.

## Feature lifecycle with opt-in features

GraphQL has long had a good story for the end of a feature's life. `@deprecated` lets you signal that something is going away before you remove it. What it did not have was a standard story for the beginning of a feature's life, the phase where something is real, usable, and worth getting feedback on, but not yet stable enough for general use. The `@requiresOptIn` proposal closes that gap and turns the lifecycle into `experimental` -> `stable` -> `deprecated` -> `removed`.

Hot Chocolate 16 implements that proposal. You can mark fields, arguments, input fields, and enum values as opt-in, and they stay out of normal introspection until the client explicitly asks for them. That gives you a clean rollout story for experimental capabilities, expensive operations, or APIs that should only be adopted deliberately.

```csharp
builder
    .AddGraphQL()
    .ModifyOptions(o => o.EnableOptInFeatures = true)
    .OptInFeatureStability("product-recommendations", "experimental");

public class Product
{
    public int Id { get; set; }

    [RequiresOptIn("product-recommendations")]
    public IReadOnlyList<Product>? Recommendations { get; set; }
}
```

We also extended the proposal. Hot Chocolate lets you declare feature stability at the schema level and expose it through introspection with `__schema.optInFeatures`, `__schema.optInFeatureStability`, and `includeOptIn`.

```graphql
schema
  @optInFeatureStability(
    feature: "product-recommendations"
    stability: "experimental"
  ) {
  query: Query
}

type Query {
  productById(id: ID!): Product
}

type Product {
  id: Int!
  recommendations: [Product] @requiresOptIn(feature: "product-recommendations")
}
```

In practice that means schema evolution becomes much more deliberate. You can ship something as experimental, promote it to stable when it has earned it, and later retire it through the existing deprecation flow.

## Incremental delivery, the new format

`@defer` and `@stream` also got a wire-format refresh. In v16, the default is now the v0.2 format from the incremental delivery spec, the one with `pending`, `incremental` entries identified by `id`, and `completed`. We use it consistently across multipart, SSE, and JSON Lines.

The older v0.1 format, the path-based one, is still fully supported. If you need to keep an existing client on it, you have two options:

- **Per-request:** add `incrementalSpec=v0.1` to the `Accept` header.
- **Server-wide:** call `AddHttpResponseFormatter(incrementalDeliveryFormat: IncrementalDeliveryFormat.Version_0_1)`.

Most clients will not need to do anything. v0.2 is where the GraphQL ecosystem is heading, and it is the better default going forward. But if you have a client that hand-rolls multipart parsing, you can keep the old format until you are ready to move.

One more thing: batching performance is now much better for incremental requests too. Like batch requests, an incremental request runs on a single work scheduler, which means it uses a single batching coordinator.

## GraphQL semantic conventions for OpenTelemetry

OpenTelemetry has been around in the GraphQL space for a while, but it was never especially well specified. Different servers ended up with their own span names and attributes, which made cross-server tooling and conventions harder than they should have been. That changed with the new [GraphQL semantic conventions for OpenTelemetry](https://github.com/graphql/otel-wg/blob/main/spec), created by the GraphQL OTel working group.

With Hot Chocolate 16, we have adopted that specification. In practice, that means our tracing now follows a shared GraphQL vocabulary instead of the server-specific conventions we used before. If you already have dashboards or alerts wired to the old names or values, the migration guide covers the rename and value changes.

## MCP and OpenAPI

These deserve their own posts, so I will keep this short. v16 ships two new adapters that work both with Hot Chocolate and with Fusion:

- **MCP**, a [Model Context Protocol](https://modelcontextprotocol.io/) server adapter that exposes GraphQL operations as MCP tools for LLM agents. It also supports agentic UI through the MCP app extension.
- **OpenAPI**, an adapter for projecting GraphQL operations as OpenAPI definitions.

Both will get their own posts in the next few weeks. If you do not want to wait, the docs are already up for the [MCP adapter](/docs/hotchocolate/v16/guides/mcp-adapter) and the [OpenAPI adapter](/docs/hotchocolate/v16/guides/openapi-adapter).

## Wrapping up

This is only the beginning. We will follow up with more posts and YouTube episodes that dive deeper into new features across Hot Chocolate, Fusion, and Mocha.

If you are upgrading, start with our [migration guide](/docs/hotchocolate/v16/migrating/migrate-from-15-to-16).

We also have a large community on [Slack](https://slack.chillicream.com), so come join us there. And if you like what we are building, help us out by starring the [project on GitHub](https://github.com/ChilliCream/graphql-platform).
