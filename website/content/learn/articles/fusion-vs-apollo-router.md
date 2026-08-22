---
title: "Hot Chocolate Fusion vs. Apollo Router"
description: "How ChilliCream's Fusion gateway compares to Apollo Router on federation standard support, Apollo Federation compatibility, and published throughput benchmarks."
kind: comparison
date: "2026-08-23"
updated: "2026-08-23"
topics: ["federation"]
products: ["fusion"]
tags: ["fusion", "federation", "apollo-federation", "graphql"]
---

Both Fusion and Apollo Router sit in front of your subgraphs as a federation
gateway. The difference that matters most: Fusion speaks both the open
GraphQL Federation specification (the Composite Schema Specification) and
Apollo Federation, while Apollo Router speaks Apollo Federation. If your
subgraphs are already on Apollo Federation, Fusion runs them unchanged; if
you're evaluating where to take federation next, Fusion's compatibility and
throughput numbers below are published and reproducible.

<ComparisonVerdict
options={[
{
name: "Fusion",
ours: true,
reasons: [
"You want one gateway that runs GraphQL Federation and Apollo Federation subgraphs in the same graph, without converting schemas",
"You're already on ASP.NET Core and want the gateway on the same platform, extensible in ordinary C#",
"You want the highest median throughput in ChilliCream's published gateway benchmark",
],
},
{
name: "Apollo Router",
reasons: [
"Your subgraphs are on Apollo Federation and you have no near-term need for the newer GraphQL Federation (Composite Schema) specification",
"Your infrastructure standardizes on Rust services",
"You want the gateway maintained directly by Apollo",
],
},
]}
/>

# Where each gateway comes from

GraphQL Federation (the Composite Schema Specification) is an open
specification developed by Apollo, ChilliCream, The Guild, and other
contributors from across the GraphQL community. It's the shared foundation
for how distributed GraphQL systems compose, plan, and execute. Fusion
implements that model and, since Fusion 16.5, also implements Apollo
Federation, so a single Fusion gateway can compose subgraphs written against
either model in the same graph. Apollo Router is Apollo's own gateway,
built in Rust, for subgraphs on Apollo Federation.

# Federation standard support

A Fusion gateway requires no schema conversion or Fusion-specific
integration to compose an Apollo Federation subgraph: the subgraph keeps its
existing Federation 2 schema and exposes it through `_service` exactly as it
does today. Fusion translates directives such as `@key`, `@requires`,
`@provides`, and `@interfaceObject` into its internal composition model
during composition, and at runtime it continues to speak Apollo Federation
to that subgraph through the `_entities` field. GraphQL Federation subgraphs
use their native lookup fields instead, and Fusion's query planner treats
both models identically, so they can participate in the same query plan.
This lets a team keep the Apollo Federation subgraphs it already operates
while adopting the newer, open GraphQL Federation specification one
subgraph at a time.

# Compatibility with Apollo Federation

## At a glance

<FeatureComparison
id="fusion-vs-apollo-router-matrix"
eyebrow="Published, reproducible"
heading="Fusion vs. Apollo Router"
columns={["Fusion 16.5.0", "Apollo Router v2.16.0"]}
groups={[
{
title: "The Guild's federation gateway audit",
rows: [
{ label: "Test cases passed", cells: ["199 / 199 (100.00%)", "194 / 199 (97.49%)"] },
{ label: "Test suites passed", cells: ["46 / 46", "43 / 46"] },
],
},
{
title: "ChilliCream gateway benchmark: constant load with downstream latency",
rows: [
{ label: "Median requests / second", cells: ["1,856", "432"] },
{ label: "Technology", cells: ["C# / .NET (ASP.NET Core)", "Rust"] },
],
},
]}
/>

Both figures come from work ChilliCream has already published: the
compatibility scores are against [The Guild's federation gateway
audit](https://the-guild.dev/graphql/hive/blog/federation-gateway-audit),
and the throughput numbers are from ChilliCream's own gateway benchmark
suite. Full sourcing is in the disclosure at the end of this article.

# Performance

The benchmark behind the throughput row above simulates real-world load: a
complex query across 50 concurrent virtual users, exercising lookups and
requirements, with 4 ms of latency added to every downstream call. Under
that load, Fusion 16.5 posts a median of 1,856 requests per second against
Apollo Router v2.16.0's 432. The [gateway benchmark
suite](https://github.com/ChilliCream/graphql-gateway-benchmarks) that
produces these numbers runs daily on dedicated hardware, resetting the
system to a clean state before each gateway run, and it also tracks a
no-latency scenario and a traffic-burst scenario if you want the fuller
picture.

# Extensibility

Fusion is a .NET library built on ASP.NET Core, so authentication, dependency
injection, configuration, observability, and inter-service resilience all
use the same platform capabilities as the rest of an ASP.NET Core
application. Extending the gateway, for example implementing a subscription
provider, adding data masking, influencing the query planner, or integrating
a custom authentication provider, is ordinary C#, with no separate
extension language or hook system to learn. Apollo Router has its own
extension mechanism; see [Apollo's own
documentation](https://www.apollographql.com/docs/graphos/routing) for what
that looks like today.

# Choosing between them

If your subgraphs already run on Apollo Federation and you're happy with
your current gateway, Fusion runs them without a rewrite while opening the
door to the open GraphQL Federation spec at your own pace. If you're
evaluating gateways today, the numbers above (full compatibility with the
open federation audit plus the highest published throughput at time of
writing) are the case for starting with Fusion. Read the [Fusion getting
started guide](../../docs/fusion/getting-started.md) to try it, or [Coming
from Apollo Federation](../../docs/fusion/migration/coming-from-apollo-federation.md)
if you're migrating an existing graph.

> [!NOTE]
> Compatibility and throughput figures on this page are reproduced from
> ChilliCream's [Fusion 16.5 announcement](../../blog/2026-07-12-fusion-16-5.md)
> (published 2026-07-12), which compares Fusion 16.5.0 against Apollo Router
> v2.16.0. The compatibility scores are against [The Guild's federation
> gateway audit](https://the-guild.dev/graphql/hive/blog/federation-gateway-audit).
> The throughput numbers are from the [gateway benchmark
> suite](https://github.com/ChilliCream/graphql-gateway-benchmarks), which
> ChilliCream builds and operates. ChilliCream also builds Fusion; read the
> linked benchmark and audit methodology yourself before making a
> purchasing decision.
