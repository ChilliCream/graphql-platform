---
title: "Performance"
---

> We are still working on the documentation for Strawberry Shake so help us by finding typos, missing things or write some additional docs with us.

In this section we will give you an overview on how to optimize the performance of your GraphQL client. We will put an emphasis on network performance since this is the most critical performance obstacle you are facing when developing fluent applications that also have to work over bad mobile network connections or even when the application becomes fully offline.

# Persisted queries and automatic persisted queries

**Improve performance by sending smaller requests and pre-compile queries**

The size of individual GraphQL requests can become a major pain point. This is not only true for the transport but also introduces inefficiencies for the server since large requests need to be parsed and validated. Hot Chocolate implements for this problem persisted queries. With persisted queries, we can store queries on the server in a key-value store. When we want to execute a persisted query, we can send the key under which the query is stored instead of the query itself. This saves precious bandwidth and also improves execution time since the server will validate, parse, and compile persisted queries just once.

There are two flavors of persisted queries that Strawberry Shake supports and that is also supported by our GraphQL server Hot Chocolate.

## Persisted queries

The first approach is to store queries ahead of time (ahead of deployment).
This is done by extracting the queries from your client application at build time. It will reduce the size of the requests and the bundle size of your application since queries can be completely removed from the client code at build time and are replaced with query hashes. Apart from performance, persisted queries can also be used for security by configuring Hot Chocolate to only accept persisted queries on production environments.

Read more on how to set up Strawberry Shake for persisted queries [here](/docs/strawberryshake/performance/persisted-queries).

## Automatic persisted queries

Automatic persisted queries allow us to store queries dynamically on the server at runtime. With this approach, we can give our application the same performance benefits as with persisted queries without having to opt in to a more complex build process.

**We are currently still working on this feature so stay tuned on this one.**

# Persisted State

Apart from focusing on reducing network request size we also can optimize using the network less by using the stores more efficiently. If you are not yet familiar with the store concepts first head over to [here](../caching).

One thing particular here is to persist the state that is aggregated in the stores to either the browsers IndexDB or to some small database like SQLite or LiteDB. When the user leaves the app and later returns to (closes the browser and reopens it) we can fill the state from our storage and have immediately data for the user while our store at the same time will start refreshing that data over the network if that is available.

Read more on how to set up Strawberry Shake for persisted state [here](/docs/strawberryshake/performance/persisted-state).
