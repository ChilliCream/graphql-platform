---
title: "Overview"
---

In this section we will learn how to secure our GraphQL endpoint.

When we think about API security, we, in many cases, only think about authorization. With GraphQL, we need to think further about security concepts to ensure that our GraphQL API performs predictably and malicious actors do not bring our service down or degrade performance for others.

# Authentication

Authentication in Hot Chocolate is built around the official authentication mechanisms in ASP.NET Core, allowing us to fully embrace their customizability and variety of authentication providers.

[Learn more about authentication](/docs/hotchocolate/v13/security/authentication)

# Authorization

Authorization is one of the most basic security concepts. It builds on top of authentication and allows us to restrict access to types and fields, based on whether a user is authenticated, assigned specific roles or satisfies one or more policies. Hot Chocolate closely matches and nicely integrates with the official ASP.NET Core authorization APIs.

[Learn more about authorization](/docs/hotchocolate/v13/security/authorization)

# Persisted Queries

Depending on our setup and requirements, the simplest way to make our server secure and control the request impact is to use persisted queries. With this approach, we can export the request from our client applications at development time and only allow the set of known queries to be executed in our production environment.

[Learn more about persisted queries](/docs/hotchocolate/v13/performance/persisted-queries)

# Introspection

Introspection is one of the GraphQL's core features and powers many GraphQL IDEs and developer tools. But introspection can also produce large results, which can degrade the performance of our server. Apart from the performance aspect, we might want to limit who can introspect our GraphQL server. Hot Chocolate allows us to control who can access introspection fields by using query validation rules.

[Learn more about restricting introspection](/docs/hotchocolate/v13/server/introspection#disabling-introspection).

# Pagination

Pagination is another topic we often forget when thinking about securing our GraphQL API. Hot Chocolate, by default, will apply strict defaults so that APIs will only allow a certain amount of nodes per connection. While we set defaults, they might not be the right ones for your environment and might yield too much load.

[Learn more about pagination](/docs/hotchocolate/v13/fetching-data/pagination)

<!-- # Execution Timeout

By default, Hot Chocolate has an internal execution timeout of 30 seconds. This is to ensure that requests do not occupy server resources for an extended amount of time. Make sure that the execution options are correctly covering your use case.-->

<!-- # Query Depth

With GraphQL, we give the consumer of our API the ability to drill into our data graph arbitrarily. The user can pick and choose what data he or she needs. This is one of the powerful concepts with GraphQL. It also is one of its vulnerabilities. We need to control how deep a user can drill into our data graph to ensure that requests perform consistently.

[Learn more about query depth validation rules](/docs/hotchocolate/v13/security/query-depth). -->

# Operation complexity

With technologies like REST, it was easy to scale servers and measure the impact of a single request on our server infrastructure. With GraphQL, we need to do a bit more to enforce that requests have a consistent impact on our servers. Hot Chocolate can track the cost of fields and deny the execution of requests that exceed the allowed impact on our system.

[Learn more about the operation complexity analyzer](/docs/hotchocolate/v13/security/operation-complexity).

# FIPS compliance

Per default Hot Chocolate uses MD5 to create a unique document hash. Since MD5 is not FIPS compliant, this might lead to issues, if you are trying to run Hot Chocolate on a device that is in FIPS compliance mode.

Fortunately, we offer the option to use the FIPS compliant SHA256 hashing algorithm to create document hashes.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddSha256DocumentHashProvider();
}
```

[Learn more about document hashing providers](/docs/hotchocolate/v13/performance/persisted-queries#hashing-algorithms)
