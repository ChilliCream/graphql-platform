---
title: "Performance"
---

# Persisted queries and automatic persisted queries

**Improve performance by sending smaller requests and pre-compile queries**

The size of individual GraphQL requests can become a major pain point. This is not only true for the transport but also introduces inefficiencies for the server since large requests need to be parsed and validated. Hot Chocolate implements for this persisted queries and active persisted queries. With persisted queries we are able to store queries on the server in a key value store. When we want to execute a persisted query we can send the the key under which the query is stored instead of the query itself. This saves bandwidth and also execution time since the server is able to validate, parse and compile persisted queries once.

There are two flavors of persisted queries that Hot Chocolate server supports.

## Persisted queries

The first approach is to store queries ahead of time (ahead of deployment of your application).
This can be done by extracting the queries from you client application, at build time. This not only will reduce the size of the requests but also the bundle size of your application, since queries can be removed from the client code at build time and are replaced with query hashes. Apart from performance, persisted queries can also be used for security by configuring Hot Chocolate to only accept persisted queries on production.

Strawberry Shake, [Relay](https://relay.dev/docs/en/persisted-queries) and Apollo client all support this approach.

Read more on how to setup your server for persisted queries [here]().

## Automatic persisted queries

Automatic Persisted Queries allows to store queries dynamically on the server. This capability allows us to create flows where the client application may attempt to execute a persisted query with the query hash. If the persisted query does not exist we can send the query to the server and store it in the servers persisted query cache. With this approach only the first time a query is executed we have to send over the whole query. This means we will have the same network and execution performance improvements like with persisted queries but do not have to implement any special build logic to extract queries. However, we do not have the option to seal our server from queries that we do not know, so this approach has no security benefits. Also we do not have any bundle size improvements for our application.

Both Strawberry Shake, and Apollo client support this approach.

Read more on how to setup your server for automatic persisted queries [here](automatic-persisted-queries.md).
