---
title: "Schema Federations"
---

In schema federations, the extension points of the gateway schema are defined on the downstream services.
Therefore you need to configure federations in two places: the gateway schema and the downstream service.

The schemas can either be pushed to a Redis cache and then pulled from the gateway or directly be pulled by the gateway from the downstream service.

# Federation with Redis
HotChocolate uses the Redis cache as a pub/sub system to signal changes on the downstream services. 
With a cache, the gateway schema is also more stable and faster in bootstrapping, because it does not require to call all downstream services on startup.

You will need to add a package reference to `HotChocolate.Stitching.Redis` to all your services:

```bash
dotnet add package HotChocolate.Stitching.Redis
```

## Configuration of a domain service
A domain service has to _publish the schema definition_. 
The schema is published on the initialization of the schema. 
By default, a schema is lazy and only initialized when the first request is sent. 
You can also initialize the schema on startup with `IntitializeOnStartup`.
Every schema requires a unique name. This name is used in several places to reference the schema. 
By calling `PublishSchemaDefinition` you can configure how the schema should be published.

Schemas are published to Redis under a configuration name. The gateway is subscribed to this configuration. 
All schemas that are registered under this name, will be discovered by the gateway

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        // This is the connection multiplexer that redis will use
        .AddSingleton(ConnectionMultiplexer.Connect("stitching-redis.services.local"))
        .AddGraphQLServer()
        .AddQueryType<Query>()
        // We initialize the schema on startup so it is published to the redis as soon as possible
        .InitializeOnStartup()
        // We configure the publish definition
        .PublishSchemaDefinition(c => c
            // The name of the schema. This name should be unique
            .SetName("accounts")
            .PublishToRedis(
                // The configuration name under which the schema should be published
                "Demo", 
                // The connection multiplexer that should be used for publishing
                sp => sp.GetRequiredService<ConnectionMultiplexer>()));
}
```

## Configuration of the gateway 
The gateway needs HttpClients to fetch the data from the domain services. 
You have to register them on the service collection. 
The name of the HttpClient has to be the same as the name of the schema it refers to.
As you may use the schema names in several places, it is good practise to store them as constant.

```csharp
public static class WellKnownSchemaNames 
{
    public const string Accounts = "accounts";
    public const string Inventory = "inventory";
    public const string Products = "products";
    public const string Reviews = "reviews";
}
```

```csharp
services.AddHttpClient(Accounts, c => c.BaseAddress = new Uri("http://accounts.service.local/graphql"));
services.AddHttpClient(Inventory, c => c.BaseAddress = new Uri("http://inventory.service.local/graphql"));
services.AddHttpClient(Products, c => c.BaseAddress = new Uri("http://products.service.local/graphql"));
services.AddHttpClient(Reviews, c => c.BaseAddress = new Uri("http://reviews.service.local/graphql"));
```

The gateway is subscribed to the Redis cache. 
As soon as the domain service has published its schema, the gateway grab the changes and update its own schema.

```csharp
services
    // This is the connection multiplexer that redis will use
    .AddSingleton(ConnectionMultiplexer.Connect("stitching-redis.services.local"))
    .AddGraphQLServer()
    .AddRemoteSchemasFromRedis("Demo", sp => sp.GetRequiredService<ConnectionMultiplexer>());
```

## Example
You can find a full schema federation example here [Federated Schema with Redis](https://github.com/ChilliCream/hotchocolate-examples/tree/master/misc/Stitching/federated-with-hot-reload)

# Federation with schema polling
You can also use federations without a Redis cache. In this case, you cannot hot reload the schema.
The configuration is very much the same as in Redis except the `PublishToRedis` part. 
Your schema will expose an additional field. This field is used by the Gateway to fetch the schema definition.

You will need to add a package reference to `HotChocolate.Stitching.Redis` to all your services:

```cli
dotnet add package HotChocolate.Stitching
```

## Configuration of a domain service
```csharp
public void ConfigureServices(IServiceCollection services)
{

    services
      .AddGraphQLServer()
      .AddQueryType<Query>()
      // We initialize the schema on startup so it is published to the redis as soon as possible
      .InitializeOnStartup()
      // We configure the publish definition
      .PublishSchemaDefinition(c => c
          // The name of the schema. This name should be unique
          .SetName("accounts"));
}
```

## Configuration of the gateway
With the polling approach, we need to make the schema aware of the domain services. 
We can just add the schema with `AddRemoteSchema`. 

```csharp
public static class WellKnownSchemaNames 
{
    public const string Accounts = "accounts";
    public const string Inventory = "inventory";
    public const string Products = "products";
    public const string Reviews = "reviews";
}
```

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // register the http clients th
    services.AddHttpClient(Accounts, c => c.BaseAddress = new Uri("http://accounts.service.local/graphql"));
    services.AddHttpClient(Inventory, c => c.BaseAddress = new Uri("http://inventory.service.local/graphql"));
    services.AddHttpClient(Products, c => c.BaseAddress = new Uri("http://products.service.local/graphql"));
    services.AddHttpClient(Reviews, c => c.BaseAddress = new Uri("http://reviews.service.local/graphql"));

    services
        .AddGraphQLServer()
        // add the remote schemas
        .AddRemoteSchema(Accounts)
        .AddRemoteSchema(Inventory)
        .AddRemoteSchema(Products)
        .AddRemoteSchema(Reviews);
```

## Example

You can find a full schema federation with polling example here [Federated Schema with polling](https://github.com/ChilliCream/hotchocolate-examples/tree/master/misc/Stitching/federated-with-pull)

# Configuration
By default, all the fields that are declared on `Mutation` and `Query` are exposed on the gateway.
In case the schema you do not want to expose the root fields and prefer to define the extension points in an extension file, you can also ignore the root types for a schema on the domain service.

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        // This is the connection multiplexer that redis will use
        .AddSingleton(ConnectionMultiplexer.Connect("stitching-redis.services.local"))
        .AddGraphQLServer()
        .AddQueryType<Query>()
        .PublishSchemaDefinition(c => c
            .SetName("accounts")
            // Ignore the root types of accounts
            .IgnoreRootTypes()
            // Declares where the type extension is used
            .AddTypeExtensionsFromFile("./Stitching.graphql")
            .PublishToRedis(
                // The configuration name under which the schema should be published
                "Demo", 
                // The connection multiplexer that should be used for publishing
                sp => sp.GetRequiredService<ConnectionMultiplexer>()));
}
```

In case you choose to ignore the root types, make sure to add a `Query` and `Mutation` type to the gateway.
If there are no root types registered on the gateway the schema will be invalid.

```csharp
services
    // This is the connection multiplexer that redis will use
    .AddSingleton(ConnectionMultiplexer.Connect("stitching-redis.services.local"))
    .AddGraphQLServer()
    .AddQueryType(d => d.Name("Query"))
    .AddRemoteSchemasFromRedis("Demo", sp => sp.GetRequiredService<ConnectionMultiplexer>());
```

For further configuration with extension files, have a look at [Schema Configuration](/docs/hotchocolate/distributed-schema/schema-configuration)
