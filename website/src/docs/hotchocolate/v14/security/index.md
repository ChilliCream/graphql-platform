---
title: "Overview"
---

In this section we will learn how to secure our GraphQL endpoint.

When we think about API security, we, in many cases, only think about authorization. With GraphQL, we need to think further about security concepts to ensure that our GraphQL API performs predictably and malicious actors do not bring our service down or degrade performance for others.

# Authentication

Authentication in Hot Chocolate is built around the official authentication mechanisms in ASP.NET Core, allowing us to fully embrace their customizability and variety of authentication providers.

[Learn more about authentication](/docs/hotchocolate/v14/security/authentication)

# Authorization

Authorization is one of the most basic security concepts. It builds on top of authentication and allows us to restrict access to types and fields, based on whether a user is authenticated, assigned specific roles or satisfies one or more policies. Hot Chocolate closely matches and nicely integrates with the official ASP.NET Core authorization APIs.

[Learn more about authorization](/docs/hotchocolate/v14/security/authorization)

# Persisted Operations

Depending on our setup and requirements, the simplest way to make our server secure and control the request impact is to use persisted operations. With this approach, we can export the request from our client applications at development time and only allow the set of known operations to be executed in our production environment.

[Learn more about persisted operations](/docs/hotchocolate/v14/performance/persisted-operations)

# Introspection

Introspection is one of the GraphQL's core features and powers many GraphQL IDEs and developer tools. But introspection can also produce large results, which can degrade the performance of our server. Apart from the performance aspect, we might want to limit who can introspect our GraphQL server. Hot Chocolate allows us to control who can access introspection fields by using query validation rules.

[Learn more about restricting introspection](/docs/hotchocolate/v14/server/introspection#disabling-introspection).

# Pagination

Pagination is another topic we often forget when thinking about securing our GraphQL API. Hot Chocolate, by default, will apply strict defaults so that APIs will only allow a certain amount of nodes per connection. While we set defaults, they might not be the right ones for your environment and might yield too much load.

[Learn more about pagination](/docs/hotchocolate/v14/fetching-data/pagination)

# Execution depth

You can limit the depth a user is able to query in a single request.

```csharp
builder.Services.AddGraphQLServer()
    .AddMaxExecutionDepthRule(5);
```

<Video videoId="PYZSSlVCuJc" />

# Execution timeout

The execution of a GraphQL request is automatically aborted after 30 seconds to prevent long-running queries affecting the performance of your GraphQL server.

This default can be overridden as shown below:

```csharp
builder.Services.AddGraphQLServer()
    .ModifyRequestOptions(o =>
    {
        o.ExecutionTimeout = TimeSpan.FromSeconds(60);
    });
```

The `ExecutionTimeout` is not honored, if a debugger is attached.

# Validation error limit

To protect against malicious queries that intentionally craft payloads, which would generate a large number of validation errors, Hot Chocolate limits the number of validation errors to 5 per default. As soon as the execution engine tries to produce a 6th validation error, the validation process is aborted and the previous 5 errors are returned.

The maximum number of validation errors can be overridden as shown below:

```csharp
builder.Services.AddGraphQLServer()
    .SetMaxAllowedValidationErrors(10);
```

# Nodes batch size

When building a [Relay.js compliant schema](/docs/hotchocolate/v14/defining-a-schema/relay#global-object-identification), our server also exposes a `nodes(ids: [ID])` field besides the `node(id: ID)` field, required by the Relay specification. This `nodes` field allows users to fetch multiple nodes at once. An attacker could exploit this and attempt to fetch a large quantity of nodes to degrade the performance of your GraphQL server. To prevent this, we limit the number of nodes that can be requested to 10.

You can change this default to suite the needs of your application as shown below:

```csharp
builder.Services.AddGraphQLServer()
    .ModifyOptions(o => o.MaxAllowedNodeBatchSize = 1);
```

# Cost analysis

With technologies like [REST](https://en.wikipedia.org/wiki/REST), it was easy to scale servers and measure the impact of a single request on our server infrastructure. With GraphQL, we need to do a bit more to enforce that requests have a consistent impact on our servers. Hot Chocolate can track the cost of fields and deny the execution of requests that exceed the allowed impact on our system.

[Learn more about cost analysis](/docs/hotchocolate/v14/security/cost-analysis)

# FIPS compliance

Per default Hot Chocolate uses MD5 to create a unique document hash. Since MD5 is not FIPS compliant, this might lead to issues, if you are trying to run Hot Chocolate on a device that is in FIPS compliance mode.

Fortunately, we offer the option to use the FIPS compliant SHA256 hashing algorithm to create document hashes.

```csharp
builder.Services.AddSha256DocumentHashProvider();
```

[Learn more about document hashing providers](/docs/hotchocolate/v14/performance/persisted-operations#hashing-algorithms)
