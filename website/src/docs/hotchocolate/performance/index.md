---
title: "Overview"
---

# Persisted queries and automatic persisted queries

**Improve performance by sending smaller requests and pre-compile queries**

The size of individual GraphQL requests can become a major pain point. This is not only true for the transport but also introduces inefficiencies for the server since large requests need to be parsed and validated. Hot Chocolate implements for this problem persisted queries. With persisted queries, we can store queries on the server in a key-value store. When we want to execute a persisted query, we can send the key under which the query is stored instead of the query itself. This saves precious bandwidth and also improves execution time since the server will validate, parse, and compile persisted queries just once.

There are two flavors of persisted queries that Hot Chocolate server supports.

## Persisted queries

The first approach is to store queries ahead of time (ahead of deployment).
This can be done by extracting the queries from your client application at build time. This will reduce the size of the requests and the bundle size of your application since queries can be removed from the client code at build time and are replaced with query hashes. Apart from performance, persisted queries can also be used for security by configuring Hot Chocolate to only accept persisted queries on production.

Strawberry Shake, [Relay](https://relay.dev/docs/guides/persisted-queries/), and [Apollo](https://www.apollographql.com/docs/react/api/link/persisted-queries/) client all support this approach.

Read more on how to set up your server for persisted queries [here](/docs/hotchocolate/performance/persisted-queries).

## Automatic persisted queries

Automatic persisted queries allow us to store queries dynamically on the server at runtime. With this approach, we can give our application the same performance benefits as with persisted queries without having to opt in to a more complex build process.

However, we do not have the option to seal our server from queries that we do not know, so this approach has **no** security benefits. We do not have any bundle size improvements for our application since the query is still needed at runtime.

Both Strawberry Shake and [Apollo](https://www.apollographql.com/docs/apollo-server/performance/apq/) client support this approach.

Read more on how to set up your server for automatic persisted queries [here](/docs/hotchocolate/performance/automatic-persisted-queries).
