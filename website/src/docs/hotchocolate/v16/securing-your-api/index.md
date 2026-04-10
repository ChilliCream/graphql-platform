---
title: "Overview"
---

Securing a GraphQL API requires more than authentication and authorization. Unlike REST, where each endpoint has a predictable cost, a single GraphQL query can traverse deep relationships and request large datasets. You need a strategy that matches your API's threat model.

Hot Chocolate provides two golden paths depending on whether your API is public or private.

# Public APIs: Cost Analysis

Public APIs face unpredictable clients. You do not control who sends queries or how complex those queries are. An attacker can craft a deeply nested query that consumes significant server resources.

**Cost analysis** is your primary defense for public APIs. It assigns a weight to each field and list in your schema, then calculates the total cost of an incoming query before executing it. Queries that exceed your cost budget are rejected.

Combine cost analysis with:

- **Pagination limits** to cap the number of items returned per connection.
- **Execution depth limits** to prevent deeply nested queries.
- **Execution timeouts** to abort long-running queries.

[Learn more about cost analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis)

# Private APIs: Trusted Documents

Private APIs serve known clients that you control, such as your own web or mobile applications. For these APIs, **trusted documents** (also called persisted operations) provide the strongest security guarantee.

With trusted documents, you extract all GraphQL operations from your client at build time and register them with the server. At runtime, the server only accepts operations it recognizes. This eliminates the risk of arbitrary queries entirely.

[Learn more about trusted documents](/docs/hotchocolate/v16/performance/trusted-documents)

# Defense in Depth

Regardless of whether your API is public or private, apply these additional protections:

## Authentication

Authentication determines who is making a request. Hot Chocolate integrates with the ASP.NET Core authentication system, supporting JWT, cookies, and other authentication schemes.

[Learn more about authentication](/docs/hotchocolate/v16/securing-your-api/authentication)

## Authorization

Authorization controls what an authenticated user can access. Hot Chocolate provides the `@authorize` directive for field-level and type-level access control, integrating with ASP.NET Core roles and policies.

[Learn more about authorization](/docs/hotchocolate/v16/securing-your-api/authorization)

## Request Limits

Hot Chocolate enforces limits at every stage of request processing -- parsing, validation, and execution -- to keep resource consumption bounded. This includes limits on fields, directives, nesting depth, execution depth, timeouts, and more.

[Learn more about request limits](/docs/hotchocolate/v16/securing-your-api/request-limits)

## Introspection

Introspection powers developer tools but can also reveal your schema to attackers. You can restrict or disable introspection in production.

[Learn more about introspection](/docs/hotchocolate/v16/securing-your-api/introspection#disabling-introspection)

## FIPS Compliance

Hot Chocolate uses MD5 for document hashing by default. If you need FIPS compliance, switch to SHA256:

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddSha256DocumentHashProvider();
```

[Learn more about hashing providers](/docs/hotchocolate/v16/performance/trusted-documents#hashing-algorithms)

# Next Steps

- **Building a public API?** Start with [Cost Analysis](/docs/hotchocolate/v16/securing-your-api/cost-analysis).
- **Building a private API?** Start with [Trusted Documents](/docs/hotchocolate/v16/performance/trusted-documents).
- **Need authentication?** See [Authentication](/docs/hotchocolate/v16/securing-your-api/authentication).
- **Need authorization?** See [Authorization](/docs/hotchocolate/v16/securing-your-api/authorization).
