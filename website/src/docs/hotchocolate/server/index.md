---
title: Overview
---

In this section, we will learn how we can configure our GraphQL server and look at some transport protocol-related details.

# Endpoints

Hot Chocolate comes with ASP.NET Core middleware for accepting HTTP / WebSocket GraphQL requests, downloading the GraphQL schema, or serving the [Banana Cake Pop](/docs/bananacakepop) GraphQL IDE.

[Learn more about endpoints](/docs/hotchocolate/server/endpoints)

# Interceptors

Interceptors allow us to intercept GraphQL requests before they are executed. There are interceptors for both GraphQL requests sent via HTTP as well as via WebSockets.

In the case of WebSockets, the interceptor also allows us to handle life cycle events, such as when a client first connects.

[Learn more about interceptors](/docs/hotchocolate/server/interceptors)

# Global State

With Global State we can define properties on a per-request basis to be made available to all resolvers and middleware.

[Learn more about Global State](/docs/hotchocolate/server/global-state)

# Introspection

Introspection allows us to query the type system of our GraphQL server using regular GraphQL queries. While this is a powerful feature, enabling all sorts of amazing developer tooling, it can also be used as an attack vector. We will take a look at how we can control who is allowed to issue introspection queries to our GraphQL server.

[Learn more about introspection](/docs/hotchocolate/server/introspection)

# Files

Though not considered one of the responsibilities of a GraphQL server, for convenience, Hot Chocolate provides file upload support. We will also take a look at what other options we have when it comes to uploading and serving files.

[Learn more about handling files](/docs/hotchocolate/server/files)

# Instrumentation

We can gather instrumentation data about our GraphQL server, by hooking into various events in the execution process of a GraphQL request. As part of instrumentation we are also covering the usage of _Apollo Tracing_.

[Learn more about instrumentation](/docs/hotchocolate/server/instrumentation)
