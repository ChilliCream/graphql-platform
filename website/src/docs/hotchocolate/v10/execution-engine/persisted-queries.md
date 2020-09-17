---
title: Persisted Queries
---

Persisted queries are a great way to improve the performance of your GraphQL server.

Persisted queries are validated once no matter if your server restarts or your cache is cleared.

Persisted queries are stored close to your server either in the file system or in a _Redis_ cache this helps to reduce request sizes since your application can send in a query key instead of the whole query.

Hot Chocolate supports out of the box two flows how to handle persisted queries.

# Ahead of Time Query Persistence

The first approach is to store queries ahead of time (ahead of deployment of your application). This can be done by extracting the queries from you client application, hashing them and pushing them to the query storage.

Both _Relay_ and _Apollo_ support this use-case and will provide the relevant hashes.

**How do we setup Hot Chocolate for that?**

Persisted queries are by default disabled, or even more so the middleware that handles all of this is not even plugged in.

In order to enable this, we will use the query execution builder to opt into a different execution pipeline.

```csharp
services.AddGraphQL(
    s => SchemaBuilder.New()
        ...
        ...
        .Create(),
    b =>  b.UsePersistedQueryPipeline()
        .AddSha256DocumentHashProvider());
```

So, with two extra lines in our schema initialization we have opted into the persisted query pipeline and opted to use SHA-256 to hash our query document. Hot Chocolate supports out of the box MD5, SHA-1, SHA-256.

OK, Next, we need to add our query storage, since we only need to read from our query storage, we can opt to use a read-only query storage like the following:

```csharp
services.AddReadOnlyRedisQueryStorage(s => s.GetRequiredService<ConnectionMultiplexer>().GetDatabase());
```

Alternatively, to _Redis_ we can also use the local file system to load queries:

```csharp
services.AddReadOnlyFileSystemQueryStorage("/usr/temp/queries");
```

With that we have pretty much configured our server to use query persistence. The next thing you would have to do is to rework your _Relay_ or _Apollo_ build scripts in order to export your query to your chosen storage. The queries are stored as plain document and have to have the hash as the name without any extension. In case of the file system the hash is saved as URL compliant base64, in case of the _Redis_ storage the key is the hash as standard base64.

In your requests to the server you usually send the query wrapped in the JSON request like the following:

```json
{
  "query": "{ foo { bar } }"
}
```

Instead of doing that we can now just specify the request like the following:

```json
{
  "id": "W5vrrAIypCbniaIYeroNnw=="
}
```

> Read more about how to do persisted queries with [Relay](https://relay.dev/docs/en/persisted-queries.html).
> Read more about how to do persisted queries with [Apollo](https://blog.apollographql.com/persisted-graphql-queries-with-apollo-client-119fd7e6bba5).

# Active Query Persistence

Active query persistence builds upon the query persistence pipeline and adds the ability to store queries on the fly.

**How does this work?**

The client would have a flow that would always first ask the server for the query with the query hash.

If the server can find the query in the query storage the server will execute it and return the result just like the _ahead of time persistence_ and if the server could not find the query then again like the _ahead of time persistence_ the server would return a GraphQL error that the query was not found.

```json
{
  "errors": [
    {
      "message": "PersistedQueryNotFound",
      "extensions": {
        "code": "PERSISTED_QUERY_NOT_FOUND"
      }
    }
  ]
}
```

> The error message and properties can be modified by adding a `IErrorFilter` that handles the specified error-code.

When the client receives this error message, the client will issue a full JSON request with the GraphQL query and the query hash.

```json
{
  "query": "{ foo { bar } }",
  "extensions": {
    "persistedQuery": {
      "sha256Hash": "W5vrrAIypCbniaIYeroNnw=="
    }
  }
}
```

If the query matches the server query hash the server will store the query in the query persistence storage and execute it. The server response would look like the following:

```json
{
  "data": {
    "foo": {
      "bar": "baz"
    }
  },
  "extensions": {
    "persistedQuery": {
      "sha256Hash": "W5vrrAIypCbniaIYeroNnw==",
      "persisted": true
    }
  }
}
```

All calls after that will use again only the hash. Since we only ever once per query will have to issue two calls, we will have over the application lifetime no overhead at all.

**How do we set this up?**

Again, we have to divert from the default query pipeline, this time we will use the `UseActivePersistedQueryPipeline` that can also handle storing queries into our query storage.

```csharp
services.AddGraphQL(
    s => SchemaBuilder.New()
        ...
        ...
        .Create(),
    b =>  b.UseActivePersistedQueryPipeline()
        .AddSha256DocumentHashProvider());
```

So, with two extra lines in our schema initialization we have opted into the active persisted query pipeline and opted to use SHA-256 to hash our query document. Hot Chocolate supports out of the box MD5, SHA-1, SHA-256.

Next, we need to add our query storage, this time we need a storage to which we can write to.

```csharp
services.AddRedisQueryStorage(s => s.GetRequiredService<ConnectionMultiplexer>().GetDatabase());
```

Alternatively, to _Redis_ we can also use the local file system to load queries:

```csharp
services.AddFileSystemQueryStorage("/usr/temp/queries");
```

OK that’s it.

> We are currently working on enabling this flow with _Relay_. Stay tuned for updates on this one.
> Read more about how to do active persisted queries with [Apollo](https://medium.com/open-graphql/graphql-dynamic-persisted-queries-eb259700f1d3).
