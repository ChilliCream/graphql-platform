---
title: "Overview"
---

This section covers ways to improve the performance of your Hot Chocolate GraphQL server.

# Startup Performance

Hot Chocolate constructs the schema eagerly at startup by default. You can register warmup tasks to pre-populate in-memory caches before the server begins accepting requests.

[Learn more about server warmup](/docs/hotchocolate/v16/server/warmup)

# Trusted Documents

The size of individual GraphQL requests can become a significant bottleneck. This affects both the transport layer and the server, since large requests need to be parsed and validated repeatedly. Hot Chocolate supports persisted operations (also known as trusted documents) to address this. With persisted operations, you store operation documents on the server in a key-value store. When executing a persisted operation, the client sends the key under which the operation document is stored instead of the full document. This saves bandwidth and improves execution time because the server validates, parses, and compiles persisted operation documents only once.

Hot Chocolate supports two flavors of persisted operations.

## Regular Persisted Operations

The first approach stores operation documents ahead of time (before deployment). You extract the operations from your client applications at build time. This reduces the size of requests and the bundle size of your application because operations can be removed from the client code at build time and replaced with operation document hashes.

Strawberry Shake, [Relay](https://relay.dev/docs/guides/persisted-queries/), and [Apollo](https://www.apollographql.com/docs/react/api/link/persisted-queries/) client all support this approach.

[Learn more about persisted operations](/docs/hotchocolate/v16/performance/persisted-operations)

## Automatic Persisted Operations

Automatic persisted operations let you store operation documents dynamically on the server at runtime. This approach gives your applications the same performance benefits as regular persisted operations without requiring a more complex build process.

However, you do not get bundle size improvements for your applications because the operations are still needed at runtime.

Both Strawberry Shake and [Apollo](https://www.apollographql.com/docs/apollo-server/performance/apq/) client support this approach.

[Learn more about automatic persisted operations](/docs/hotchocolate/v16/performance/automatic-persisted-operations)
