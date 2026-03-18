---
title: Overview
---

This section covers how you configure and operate a Hot Chocolate GraphQL server. You will find details on transport protocols, middleware, dependency injection, and runtime behavior.

# Endpoints

Hot Chocolate provides ASP.NET Core endpoint middleware for accepting HTTP and WebSocket GraphQL requests, downloading the schema, and serving the [Nitro](/products/nitro) GraphQL IDE.

[Learn more about endpoints](/docs/hotchocolate/v16/server/endpoints)

# HTTP Transport

Hot Chocolate implements the GraphQL over HTTP specification. In v16, the default incremental delivery format changed from v0.1 to v0.2, and clients can select the format through the `Accept` header.

[Learn more about the HTTP transport](/docs/hotchocolate/v16/server/http-transport)

# Interceptors

Interceptors let you intercept GraphQL requests before execution. There are interceptors for both HTTP requests and WebSocket sessions.

For WebSockets, the interceptor also handles lifecycle events such as when a client first connects.

[Learn more about interceptors](/docs/hotchocolate/v16/server/interceptors)

# Dependency Injection

Hot Chocolate recognizes services registered in your DI container and injects them into resolvers automatically. In v16, services are resolved implicitly without requiring the `[Service]` attribute.

[Learn more about dependency injection](/docs/hotchocolate/v16/server/dependency-injection)

# Warmup

Hot Chocolate constructs the schema eagerly at startup. You can go further by registering warmup tasks that pre-populate caches before the server begins accepting traffic.

[Learn more about warmup](/docs/hotchocolate/v16/server/warmup)

# Global State

Global State lets you define properties on a per-request basis and makes them available to all resolvers and middleware.

[Learn more about global state](/docs/hotchocolate/v16/server/global-state)

# Introspection

Introspection lets you query the type system of your GraphQL server using regular GraphQL queries. While this powers developer tooling, it can also be an attack vector. You can control who is allowed to issue introspection queries.

[Learn more about introspection](/docs/hotchocolate/v16/server/introspection)

# Files

Hot Chocolate provides file upload support through the `Upload` scalar, even though file handling is not traditionally a GraphQL server concern. You can also return presigned URLs for a hybrid approach.

[Learn more about handling files](/docs/hotchocolate/v16/server/files)

# Instrumentation

Hot Chocolate exposes diagnostic events across the server, execution engine, and DataLoader layers. The built-in OpenTelemetry integration aligns with the proposed GraphQL semantic conventions.

[Learn more about instrumentation](/docs/hotchocolate/v16/server/instrumentation)

# Batching

Batching lets you send and execute multiple GraphQL operations in a single request. In v16, batching is disabled by default and you enable it through the `AllowedBatching` flags enum.

[Learn more about batching](/docs/hotchocolate/v16/server/batching)

# Command Line

The command-line interface lets you export your GraphQL schema from the terminal, which is useful for CI/CD pipelines.

[Learn more about the command line](/docs/hotchocolate/v16/server/command-line)
