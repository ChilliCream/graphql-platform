---
path: "/blog/2026/06/06/newsletter-may-2026"
date: "2026-06-06"
title: "Newsletter May 2026"
description: "Hot Chocolate 16, Fusion 16, MCP, OpenAPI, Semantic Introspection, skillz, and more. Read the newsletter to learn about all the things we shipped in May and what comes next."
tags:
  [
    "hotchocolate",
    "fusion",
    "graphql",
    "dotnet",
    "ai",
    "mcp",
    "openapi",
    "semantic-introspection",
    "release",
  ]
author: Pascal Senn
authorUrl: https://github.com/pascalsenn
authorImageUrl: https://avatars.githubusercontent.com/u/14233220?v=4
featuredImage: "header.png"
---

Dear ChilliCream Community,

It has been more than a year since our last major platform cycle, and May was worth the wait. Hot Chocolate 16 and Fusion 16 are both out, we added new adapters for MCP and OpenAPI, we pushed Semantic Introspection forward for AI-driven schema discovery, we released `skillz` on NuGet to help you bring your conventions to your AI agents, and many of us got to see each other in person again at GraphQLConf 2026. It was one of our biggest months yet, and it was shaped by the people reading this. Here is what we shipped and what comes next.

## Fusion 16

GraphQL gateway performance is often framed as a Rust or Go story. With Fusion 16, it is also a .NET story.

While other vendors rewrote their gateways in Rust and Go to chase throughput, we stayed on .NET. Fusion 16 now ranks #2 in our federation benchmarks, second only to the Hive Router. It outperforms two Rust-based routers and a Go router, and when subgraphs carry realistic IO latency, the remaining gap nearly disappears.

That performance comes from a brand-new execution engine. Fusion 16 is no longer built as an extension on top of Hot Chocolate. It now has its own architecture with a memory model inspired by Rust-style arena allocation: everything is managed as bytes, results are referenced instead of copied, and each request rents and returns fixed-size memory chunks.

The result is a fast gateway without giving up the .NET platform. Your gateway remains an ASP.NET Core application running on .NET 8, 9, and 10. Authentication, configuration, resilience, and observability stay in your hands, and you automatically inherit every Kestrel and runtime improvement Microsoft ships.

Read the full post: [What's new in Fusion 16](/blog/2026/05/15/fusion-16).

## Hot Chocolate 16

Hot Chocolate 16 is our first major GraphQL server release in over a year, and it touches some of the deepest parts of the server. We reworked the type system, tightened scalar contracts, improved batching, adopted new GraphQL spec proposals, and made the defaults safer.

Read the full post: [What's new for Hot Chocolate 16](/blog/2026/05/11/hot-chocolate-16).

## OpenAPI adapter

Every GraphQL project eventually meets a consumer that needs REST: a partner integration, a legacy system, or a tool that cannot speak GraphQL. The new OpenAPI adapter lets you expose selected parts of your graph as REST endpoints without building and maintaining a second API.

Read the full post: [Open Your GraphQL API for the REST](/blog/2026/06/11/open-your-graphql-api-for-the-rest).

## MCP adapter

Agents are becoming API consumers, and with v16 you can give them an MCP server built directly on top of your data graph. Tools are authored as GraphQL operations, so they reuse the schema, validation, authorization, and execution pipeline you already have.

But that is not all. The new MCP adapter also supports the MCP Apps standard, so you can colocate UI with your tools and return richer, agentic experiences directly from your GraphQL server or gateway.

Read the full post: [From GraphQL to MCP in Two Lines](/blog/2026/05/28/mcp-hotchocolate-fusion).

## skillz

`skillz` is a .NET CLI for installing, updating, and authoring Agent Skills. You package your team's conventions once as a skill, and any compatible agent loads it when a matching task comes up, so you stop re-explaining the same context every session. It runs one-shot with `dnx`, the way `npx` runs a package from npm.

Alongside the CLI, we are publishing our first skill for the platform: `graphql-schema-design` for schema design and review. More are on the way, including `graphql-backend` for Hot Chocolate v16 backend patterns and `dataloader` for Green Donut DataLoaders.

```bash
dnx skillz add ChilliCream/agent-skills --skill graphql-schema-design
```

Read the announcement: [Introducing skillz](/blog/2026/06/05/introducing-skillz).

## Semantic Introspection

Classic GraphQL introspection tells a client everything about a schema. That works well for developer tools, but it is too much context for agents working against large APIs. Semantic Introspection adds a search layer to introspection, so an agent can find the schema members relevant to a task and then fetch the exact definitions it needs.

Hot Chocolate 16 and Fusion 16 include this through `__search` and `__definitions`. By default, the schema is indexed with BM25, so discovery can scale from small schemas to large enterprise graphs without sending the whole schema to the model on every turn. GraphQL keeps its precision for data fetching, while discovery becomes practical for AI workflows.

With Nitro, Semantic Introspection can go beyond BM25 by adding embeddings to your schema, giving agents true semantic search across your API. That can make agent interaction dramatically cheaper than loading full API descriptions into context, while keeping GraphQL's precision intact. For the first time, you can really talk to your data.

Read the full post: [Semantic Introspection](/blog/2026/04/22/semantic-introspection).

## From the community

None of this lands without you. Two major releases in a single cycle meant a long preview period, and the people who ran those previews against real workloads, filed the issues that caught the rough edges, sent pull requests, and argued schema design with us in the open are the reason v16 feels solid on day one. Your feedback shaped real decisions in the type system, the scalar contracts, and the new Fusion engine. Thank you for that.

It was also great to see so many of you in person at GraphQLConf 2026. Putting faces to GitHub handles and Slack avatars was the best part. Talking through federation, schema evolution, and GraphQL for agents over nitro cold brew is the part no release notes can capture.

We would love to hear what you are building. If you have shipped something with Hot Chocolate, Fusion, or Nitro that you are proud of, tell us about it. Come share it, ask questions, and join the conversation with the rest of the community on [Slack](https://slack.chillicream.com).

## Thank you

May was a release month, but the work continues. We will keep publishing more Agent Skills, more v16 documentation, and more guidance for building production GraphQL systems on .NET.

To everyone who helped get this over the line: thank you. We are glad to build this alongside you.

Warm regards,

The ChilliCream Team
