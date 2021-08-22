---
title: Migrate from Hot Chocolate GraphQL server 11 to 12
---

This guide will walk you through the manual migration steps to get your Hot Chocolate GraphQL server to version 12.

# Scalars

We changed some defaults around scalars. These new defaults can break your existing schema but are, in general, better for newcomers and align better with the overall GraphQL ecosystem. Of course, you can naturally opt out of these new defaults to preserve your current schema's integrity.

## UUID

We changed the name of the UUID scalar from `Uuid` to `UUID`. To maintain the old name, register the type manually like the following:

```csharp
services
    .AddGraphQLServer()
    .AddType(() => new UuidType("Uuid"));
```

Further, we changed the default serialization of UUID values from format `N` (`nnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnn`) to format `D` (`nnnnnnnn-nnnn-nnnn-nnnn-nnnnnnnnnnnn`). While the format `N` saved a few payload characters, new users, in general, often had issues with that format and some other tooling. New users will now, by default, have a better experience when using non-ChilliCream tooling.

To preserve the old format, you can directly provide the format in the scalar.

```csharp
services
    .AddGraphQLServer()
    .AddType(() => new UuidType(defaultFormat: 'N'));
```

In order to fully preserve version 11 behavior do:

```csharp
services
    .AddGraphQLServer()
    .AddType(() => new UuidType("Uuid", defaultFormat: 'N'));
```

## URL

We changed the name of the URL scalar from `Url` to `URL`. To maintain the old name, register the type manually like the following:

```csharp
services
    .AddGraphQLServer()
    .AddType(() => new UrlType("Url"));
```

# Pagination

## ConnectionType

We have changed the way we infer the name for the connection type when using cursor-based pagination. By default, the connection name is now inferred from the field name instead of the type name.

```SDL
type Person {
  friends: [Person]
}
```

In version 11, we would have created a connection named `PersonConnection`.

```SDL
type Person {
  friends(first: Int, last: Int, after: String, before: String): PersonConnection
}
```

In version 12, we now will infer the connection name as `FriendsConnection`.

```SDL
type Person {
  friends(first: Int, last: Int, after: String, before: String): FriendsConnection
}
```

To keep your schema stable when you migrate, you can switch the behavior back to how you did in version 11.

```csharp
services
    .AddGraphQLServer()
    .SetPagingOptions(new PagingOptions{ InferConnectionNameFromField = false })
    ...
```

Moreover, you now can explicitly define the connection name per field.

```csharp
public class Person
{
    [UsePaging(ConnectionName = "Persons")]
    public IQueryable<Person> GetFriends() => ...
}
```

## MongoDB Paging

In version 11 we had the `UseMongoDbPagingAttribute` and the `UseMongoDbOffsetPagingAttribute`, which we removed with version 11. In version 12 you now can use the standard attributes `UsePagingAttribute` and `UseOffsetPagingAttribute`.

To use these attributes with mongo, you need to register the mongo paging provider with your GraphQL configuration:

```csharp
services
    .AddGraphQLServer()
    .AddMongoDbPagingProviders()
    ...
```

# Records

With version 11, we added support for records and added the ability to infer attributes from parameters. This, in the end, leads to more errors than benefits. With version 12, we removed this feature. Use the official' property' keyword to write records in C# short-hand syntax when annotating properties.

```csharp
public record Foo([property: ID] string Id);
```

# Relay

## Configuration

Previously the configuration of the Relay integration was focused around the `EnableRelaySupport()` method. It allowed you to enable Global Object Identification and automatically adding a query field to mutation payloads.

The problem is that `EnableRelaySupport()` always enables the Global Object Identification feature. This is not obviously implied by the name and also prevents you from using the other feature in isolation.

Therefore we introduced two separate APIs to give you more explicit control over which parts of the Relay integration you want to enable.

### Global Object Identification

**OLD**

```csharp
services
    .AddGraphQLServer()
    .EnableRelaySupport();
```

**NEW**

```csharp
services
    .AddGraphQLServer()
    .AddGlobalObjectIdentification();
```

### Query field in Mutation payloads

**OLD**

```csharp
services
    .AddGraphQLServer()
    .EnableRelaySupport(new RelayOptions
    {
        AddQueryFieldToMutationPayloads = true,
        QueryFieldName = "rootQuery",
        MutationPayloadPredicate = type => type.Name.Value.EndsWith("Result")
    });
```

**NEW**

```csharp
sevices
    .AddGraphQL()
    .AddQueryFieldToMutationPayloads(options =>
    {
        options.QueryFieldName = "rootQuery";
        options.MutationPayloadPredicate =
            type => type.Name.Value.EndsWith("Result");
    });
```

If you just want to enable the feature without further configuration, you can omit the `options =>` action.

> ⚠️ Note: Since `EnableRelaySupport()` previously always implied the usage of Global Object Identification, you might have to enable Global Object Identification separately as well.

## ID

We renamed the `ID` attribute and extension methods to `GlobalId`, in order to more accurately reflect their connection to the Global Object Identification feature.

**OLD**

```csharp
[ID]
public string Id { get; set; }
```

```csharp
descriptor.Field(f => f.Id).ID();
```

**NEW**

```csharp
[GlobalId]
public string Id { get; set; }
```

```csharp
descriptor.Field(f => f.Id).GlobalId();
```
