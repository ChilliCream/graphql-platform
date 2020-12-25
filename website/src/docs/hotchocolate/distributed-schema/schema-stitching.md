---
title: "Schema Stitching"
---

In the centralized approach of schema distribution, all the configuration is done on the gateway.

HotChocolate uses the schema name as an identifier for schemas. This schema name is used to create HTTP clients and references the schema in various places. It is good practice to store these schema names as a constant.

You will need to add a package reference to `HotChocolate.Stitching` to your gateway:

```cli
dotnet add package HotChocolate.Stitching
```

```csharp
public static class WellKnownSchemaNames 
{
    public const string Accounts = "accounts";
    public const string Inventory = "inventory";
    public const string Products = "products";
    public const string Reviews = "reviews";
}
```

The schema names are used to create a HttpClient. You have to register the HttpClients of the schema with `AddHttpClient`.

```csharp
    services.AddHttpClient(Accounts, c => c.BaseAddress = new Uri("http://accounts.service.local/graphql"));
    services.AddHttpClient(Inventory, c => c.BaseAddress = new Uri("http://inventory.service.local/graphql"));
    services.AddHttpClient(Products, c => c.BaseAddress = new Uri("http://products.service.local/graphql"));
    services.AddHttpClient(Reviews, c => c.BaseAddress = new Uri("http://reviews.service.local/graphql"));
```

To make your schema aware of the downstream services you have to add them to the schema with `AddRemoteSchema`

```csharp
  services
      .AddGraphQLServer()
      .AddRemoteSchema(Accounts)
      .AddRemoteSchema(Inventory)
      .AddRemoteSchema(Products)
      .AddRemoteSchema(Reviews)
```

By default, all the fields that are declared on `Mutation` and `Query` are exposed on the gateway.
In case the schema you do not want to expose the root fields and prefer to define the extension points in an extension file, you can also ignore the root types for a schema.

```csharp
    services
        .AddGraphQLServer()
        .AddQueryType(d => d.Name("Query"))
        .AddRemoteSchema(Accounts, ignoreRootTypes: true)
        .AddRemoteSchema(Inventory, ignoreRootTypes: true)
        .AddRemoteSchema(Products, ignoreRootTypes: true)
        .AddRemoteSchema(Reviews, ignoreRootTypes: true)
        .AddTypeExtensionsFromFile("./Stitching.graphql");
```

For further configuration with extension files, have a look at [Delegation Configuration](/docs/hotchocolate/distributed-schema/delegation-configuration)

# Example
You can find a full schema stitching example here [Centralized Schema Stitching](https://github.com/ChilliCream/hotchocolate-examples/tree/master/misc/Stitching/centralized)
