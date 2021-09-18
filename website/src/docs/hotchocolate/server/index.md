---
title: Overview
---

In this section, we will learn how we can configure our GraphQL server and look at some transport protocol-related details.

# Middleware

Hot Chocolate comes with ASP.NET Core middleware for accepting HTTP / WebSocket GraphQL requests, downloading the GraphQL schema, or serving the [Banana Cake Pop](/docs/bananacakepop) GraphQL IDE.

[Learn more about middleware](/docs/hotchocolate/server/middleware)

# Interceptors

Interceptors allow us to intercept GraphQL requests before they are executed. There are interceptors for both GraphQL requests sent via HTTP as well as via WebSockets.

In the case of WebSockets, the interceptor also allows us to handle life cycle events, such as when a client first connects.

[Learn more about interceptors](/docs/hotchocolate/server/interceptors)

# Global State

With Global State we can define properties on a per-request basis to be made available to all resolvers and middleware.

[Learn more about Global State](/docs/hotchocolate/server/global-state)

# Uploading files

Though not considered one of the responsibilities of a GraphQL server, for convenience, Hot Chocolate provides file upload support.

[Learn more about uploading files](/docs/hotchocolate/server/uploading-files)

# Instrumentation

We can gather instrumentation data about our GraphQL server, by hooking into various events in the execution process of a GraphQL request. As part of instrumentation we are also covering the usage of _Apollo Tracing_.

[Learn more about instrumentation](/docs/hotchocolate/server/instrumentation)
