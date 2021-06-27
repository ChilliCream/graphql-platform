---
title: "Overview"
---

When we think about API security, we, in many cases, only think about authorization. With GraphQL, we need to think further about security concepts to ensure that our GraphQL API performs predictably and malicious actors do not bring our service down or degrade performance for others. In this document, we will give you an overview of the various approaches.

# Authentication

Authentication in Hot Chocolate is built around the official authentication mechanisms in ASP.NET Core, allowing us to fully embrace their customizability and variety of authentication providers.

[Learn more about authentication](/docs/hotchocolate/security/authentication)

# Authorization

Authorization is one of the most basic security concepts. It builds on top of authentication and allows us to restrict access to types and fields, based on whether a user is authenticated, assigned specific roles or satisfies one or more policies. Hot Chocolate closely matches and nicely integrates with the official ASP.NET Core authorization APIs.

[Learn more about authorization](/docs/hotchocolate/security/authorization)

# Persisted Queries

Depending on our setup and requirements, the simplest way to make our server secure and control the request impact is to use persisted queries. With this approach, we can export the request from our client applications at development time and only allow the set of known queries to be executed in our production environment.

[Learn more about persisted queries](/docs/hotchocolate/performance/persisted-queries)
