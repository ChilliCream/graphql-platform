---
title: Overview
---

In this section, you will learn how you can configure your GraphQL server and we will look at some transport protocol-related details.

# Endpoints

Hot Chocolate comes with ASP.NET Core endpoint middleware for accepting HTTP / WebSocket GraphQL requests, downloading the GraphQL schema, or serving the [Nitro](/products/nitro) GraphQL IDE.

[Learn more about endpoints](/docs/hotchocolate/v15/server/endpoints)

# HTTP transport

Hot Chocolate implements the GraphQL over HTTP specification.

[Learn more about the HTTP transport](/docs/hotchocolate/v15/server/http-transport)

# Interceptors

Interceptors allow you to intercept GraphQL requests before they are executed. There are interceptors for both GraphQL requests sent via HTTP as well as via WebSockets.

In the case of WebSockets, the interceptor also allows you to handle life cycle events, such as when a client first connects.

[Learn more about interceptors](/docs/hotchocolate/v15/server/interceptors)

# Dependency injection

Hot Chocolate allows you to access dependency injection services inside your resolvers. We will take a look at the different ways you can inject services and also how you can switch out the dependency injection provider.

[Learn more about Dependency Injection](/docs/hotchocolate/v15/server/dependency-injection)

# Global State

With Global State you can define properties on a per-request basis to be made available to all resolvers and middleware.

[Learn more about Global State](/docs/hotchocolate/v15/server/global-state)

# Introspection

Introspection allows you to query the type system of your GraphQL server using regular GraphQL queries. While this is a powerful feature, enabling all sorts of amazing developer tooling, it can also be used as an attack vector. We will take a look at how you can control who is allowed to issue introspection queries against your GraphQL server.

[Learn more about introspection](/docs/hotchocolate/v15/server/introspection)

# Files

Though not considered one of the responsibilities of a GraphQL server, for convenience, Hot Chocolate provides file upload support. We will also take a look at what other options you have when it comes to uploading and serving files.

[Learn more about handling files](/docs/hotchocolate/v15/server/files)

# Instrumentation

Hot Chocolate allows you to gather instrumentation data about your GraphQL server, by hooking into various events in the execution process of a GraphQL request. You will also learn how to set up our OpenTelemetry integration.

[Learn more about instrumentation](/docs/hotchocolate/v15/server/instrumentation)

# Batching

Batching allows you to send and execute a sequence of GraphQL operations in a single request.

[Learn more about batching](/docs/hotchocolate/v15/server/batching)
