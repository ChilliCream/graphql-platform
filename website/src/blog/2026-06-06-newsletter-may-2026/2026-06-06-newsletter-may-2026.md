---
path: "/blog/2026/06/06/newsletter-may-2026"
date: "2026-06-06"
title: "Newsletter May 2026"
description: "Hot Chocolate 16, Fusion 16, skillz, and more. Read the newsletter to learn about all the things we shipped in May and what comes next."
tags: ["hotchocolate", "fusion", "graphql", "dotnet", "ai", "release"]
author: Pascal Senn
authorUrl: https://github.com/pascalsenn
authorImageUrl: https://avatars.githubusercontent.com/u/14233220?v=4
featuredImage: "header.png"
---

Dear ChilliCream Community,

It has been more than a year since our last major platform cycle, and May was worth the wait. Hot Chocolate 16 and Fusion 16 are both out, we released `skillz` on NuGet to help you bring your conventions to your AI agents, and many of us got to see each other in person again at GraphQLConf 2026. It was one of our biggest months yet, and it was shaped by the people reading this. Here is what we shipped and what comes next.

## Fusion 16

Fusion 16 is a major step for our distributed GraphQL work. It is no longer built as an extension on top of Hot Chocolate; instead, it now has its own architecture while staying fully aligned with ASP.NET Core and the .NET ecosystem.

That gives Fusion a cleaner foundation for gateway execution, planning, and composition. Your gateway remains an ASP.NET Core application, so authentication, configuration, resilience, and observability stay in your hands.

Read the full post: [What's new in Fusion 16](/blog/2026/05/15/fusion-16).

## Hot Chocolate 16

Hot Chocolate 16 is our first major release in over a year, and it touches some of the deepest parts of the server. We reworked the type system, tightened scalar contracts, improved batching, adopted new GraphQL proposals, and made the defaults safer.

The new type system is the foundation for a lot of what comes next. It gives Hot Chocolate, the mutable SDL model, and Fusion a shared abstraction, which reduces duplication and makes cross-cutting features easier to build.

Read the full post: [What's new for Hot Chocolate 16](/blog/2026/05/11/hot-chocolate-16).

## skillz

`skillz` is a .NET CLI for installing, updating, and authoring Agent Skills. You package your team's conventions once as a skill, and any compatible agent loads it when a matching task comes up, so you stop re-explaining the same context every session. It runs one-shot with `dnx`, the way `npx` runs a package from npm.

Alongside the CLI, we are publishing our first skill for the platform: `graphql-schema-design` for schema design and review. More are on the way, including `graphql-backend` for Hot Chocolate v16 backend patterns and `dataloader` for Green Donut DataLoaders.

```bash
dnx skillz add ChilliCream/agent-skills --skill graphql-schema-design
```

Read the announcement: [Introducing skillz](/blog/2026/06/05/introducing-skillz).

## From the community

None of this lands without you. Two major releases in a single cycle meant a long preview period, and the people who ran those previews against real workloads, filed the issues that caught the rough edges, sent pull requests, and argued schema design with us in the open are the reason v16 feels solid on day one. Your feedback shaped real decisions in the type system, the scalar contracts, and the new Fusion engine. Thank you for that.

It was also great to see so many of you in person at GraphQLConf 2026. Putting faces to GitHub handles and Slack avatars was the best part. Talking through federation, schema evolution, and GraphQL for agents over nitro cold brew is the part no release notes can capture.

We would love to hear what you are building. If you have shipped something with Hot Chocolate, Fusion, or Nitro that you are proud of, tell us about it. Come share it, ask questions, and join the conversation with the rest of the community on [Slack](https://slack.chillicream.com).

## Thank you

May was a release month, but the work continues. We will keep publishing more Agent Skills, more v16 documentation, and more guidance for building production GraphQL systems on .NET.

To everyone who helped get this over the line: thank you. We are glad to build this alongside you.

Warm regards,

The ChilliCream Team
