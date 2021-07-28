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

### Shorthand for specifying the IdType

If you have previously used the `IDAttribute` as a shorthand for the `ID` scalar (`IdType`), but didn't actually need the Relay specific encoding of the Ids, you can create your own attribute and use it as shorthand:

```csharp
public class IdAttribute : ObjectFieldDescriptorAttribute
{
    public override void OnConfigure(IDescriptorContext context,
        IObjectFieldDescriptor descriptor, MemberInfo member)
    {
        descriptor.Type<NonNullType<IdType>>();
    }
}

public class Foo
{
    [Id]
    public string Id { get; set; }
}
```
