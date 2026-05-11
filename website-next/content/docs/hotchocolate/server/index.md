---
title: Overview
---

This section covers how you configure and operate a Hot Chocolate GraphQL server. You will find details on transport protocols, middleware, dependency injection, and runtime behavior.

# Endpoints

Hot Chocolate provides ASP.NET Core endpoint middleware for accepting HTTP and WebSocket GraphQL requests, downloading the schema, and serving the [Nitro](/products/nitro) GraphQL IDE.

[Learn more about endpoints](./endpoints.md)

# HTTP Transport

Hot Chocolate implements the GraphQL over HTTP specification. In v16, the default incremental delivery format changed from v0.1 to v0.2, and clients can select the format through the `Accept` header.

[Learn more about the HTTP transport](./http-transport.md)

# Cache Control

Cache control lets your GraphQL server emit `Cache-Control` and `Vary` response headers that CDNs, reverse proxies, and browsers can use for HTTP caching decisions.

[Learn more about cache control](./cache-control.md)

# Interceptors

Interceptors let you intercept GraphQL requests before execution. There are interceptors for both HTTP requests and WebSocket sessions.

For WebSockets, the interceptor also handles lifecycle events such as when a client first connects.

[Learn more about interceptors](./interceptors.md)

# Dependency Injection

Hot Chocolate recognizes services registered in your DI container and injects them into resolvers automatically. In v16, services are resolved implicitly without requiring the `[Service]` attribute.

[Learn more about dependency injection](../resolvers-and-data/dependency-injection.md)

# Warmup

Hot Chocolate constructs the schema eagerly at startup. You can go further by registering warmup tasks that pre-populate caches before the server begins accepting traffic.

[Learn more about warmup](./warmup.md)

# Global State

Global State lets you define properties on a per-request basis and makes them available to all resolvers and middleware.

[Learn more about global state](./global-state.md)

# Introspection

Introspection lets you query the type system of your GraphQL server using regular GraphQL queries. While this powers developer tooling, it can also be an attack vector. You can control who is allowed to issue introspection queries.

[Learn more about introspection](../securing-your-api/introspection.md)

# Files

Hot Chocolate provides file upload support through the `Upload` scalar, even though file handling is not traditionally a GraphQL server concern. You can also return presigned URLs for a hybrid approach.

[Learn more about handling files](./files.md)

# Instrumentation

Hot Chocolate exposes diagnostic events across the server, execution engine, and DataLoader layers. The built-in OpenTelemetry integration aligns with the proposed GraphQL semantic conventions.

[Learn more about instrumentation](./instrumentation.md)

# Batching

Batching lets you send and execute multiple GraphQL operations in a single request. In v16, batching is disabled by default and you enable it through the `AllowedBatching` flags enum.

[Learn more about batching](./batching.md)

# Command Line

The command-line interface lets you export your GraphQL schema from the terminal, which is useful for CI/CD pipelines.

[Learn more about the command line](./command-line.md)
