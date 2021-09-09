---
title: Overview
---

In this section we will learn how we can configure our GraphQL server and look at some transport protocol related details.

# Middlewares

Hot Chocolate comes with ASP.NET Core middlewares for accepting HTTP / WebSocket GraphQL requests, downloading the GraphQL schema or serving the [Banana Cake Pop](/docs/bananacakepop) GraphQL IDE.

[Learn more about middlewares](/docs/hotchocolate/server/middlewares)

# Interceptors

Interceptors allow us to intercept GraphQL requests before they are executed. There are interceptors for both GraphQL requests sent via HTTP as well as via WebSockets.

In the case of WebSockets, the interceptor also allows us to handle life cycle events, such as when a client first connects.

[Learn more about interceptors](/docs/hotchocolate/server/interceptors)

# Uploading files

Though not considered one of the responsibilities of a GraphQL server, for convenience, Hot Chocolate provides file upload support.

[Learn more about uploading files](/docs/hotchocolate/server/uploading-files)
