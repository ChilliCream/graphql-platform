---
title: "Overview"
---

When we think about API security, we, in many cases, only think about authorization. With GraphQL, we need to think further about security concepts to ensure that our GraphQL API performs predictably and malicious actors do not bring our service down or degrade performance for others. In this document, we will give you an overview of the various approaches.

# Authentication

TODO

[Learn more about Authentication](/docs/hotchocolate/security/authentication)

# Authorization

Authorization is one of the most basic security concepts. With authorization, you can limit what data people can fetch. Hot Chocolate integrates with the ASP.NET Core authorization policies, which can be applied to fields.

[Learn more about Authorization](/docs/hotchocolate/security/authorization)

# Persisted Queries

Depending on our setup and requirements, the simplest way to make our server secure and control the request impact is to use persisted queries. With this approach, we can export the request from our client applications at development time and only allow the set of known queries to be executed in our production environment.

[Learn more about persisted queries](/docs/hotchocolate/performance/persisted-queries)
