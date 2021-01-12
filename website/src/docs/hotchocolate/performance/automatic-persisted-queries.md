---
title: "Automatic persisted queries"
---

**Improve performance by sending smaller requests and pre-compile queries**

Automatic Persisted Queries allows us to store queries dynamically on the server. This capability allows us to create flows where the client application may attempt to execute a persisted query with the query hash. If the persisted query does not exist we can send the query to the server and store it in the servers persisted query cache. With this approach only the first time a query is executed we have to send over the whole query. This means we will have the same network and execution performance improvements like with persisted queries but do not have to implement any special build logic to extract queries. However, we do not have the option to seal our server from queries that we do not know, so this approach has no security benefits. Also we do not have any bundle size improvements for our application.

Both Strawberry Shake, and Apollo client support this approach.
