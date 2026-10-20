---
title: "GraphQL Federation Development Just Got Easier"
description: "Use Fusion 16.6 and Aspire to work on one subgraph locally against the rest of your federated system, with automatic gateway recomposition and breaking change feedback."
youtubeId: "_gVSJRS0h5w"
example: "https://github.com/ChilliCream/fusion-demo-recommendations"
author: "michael-staib"
tags:
  - fusion
  - federation
  - aspire
  - hotchocolate
---

Hi everyone!

In this episode, we’re going to explore how Fusion 16.6 makes local development for GraphQL Federation easier. We’ll use Aspire to work on a single subgraph locally while connecting to the rest of the federated system remotely, automatically recompose our gateway, and even give AI agents immediate feedback about breaking changes that could affect production.

This means you can work in complete isolation while still getting feedback against your production system, giving you the confidence that your local changes won’t break existing clients or operations. It’s a huge step forward for local development in federated GraphQL systems.

So buckle up, and let’s jump in! 🚀

Source code: [fusion-demo-recommendations](https://github.com/ChilliCream/fusion-demo-recommendations) (the subgraph we work on) and [fusion-demo](https://github.com/ChilliCream/fusion-demo) (the rest of the federated system).

Hot Chocolate version used in this video: [16.6.0](https://www.nuget.org/packages/HotChocolate.AspNetCore/16.6.0)

Fusion gateway version used in this video: [16.6.0](https://www.nuget.org/packages/HotChocolate.Fusion.AspNetCore/16.6.0)
