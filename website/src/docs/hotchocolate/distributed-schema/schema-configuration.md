---
title: "Schema Configuration"
---

Schema stitching and federations do have a lot more potential than just merging root types.
You can remove and rename types and fields, extend types with new resolvers and delegate these resolvers to a domain service.

# Schema Transformation

## Rename Types

The name of a GraphQL type has to be unique.
When you build a standalone GraphQL server, the schema validation will make sure that no name is duplicated.
In case a name is duplicated, an exception is thrown and the schema will not compile.

This behaviour is good for the standalone server but can be an issue in distributed schemas.
Even with domain services covering domain-specific topics, a type may be duplicated.

To avoid an invalid schema, HotChocolate will prefix duplicated types with the schema name and auto resolves name collisions if they are not structurally equal.

Let us assume we have a product and an inventory service. Both define a type called `Category`:

```sdl
type Category {
  name: String
}
```

```sdl
type Category {
  name: String
  subCategories: [Category!]!
}
```

The collision resolver of HotChocolate will resolve the following on the stitching layer:

```sdl
type Category @source(name: "Category", schema: "products") {
  name: String!
  subCategories: [Category!]!
}

type inventory_Category @source(name: "Category", schema: "inventory") {
  name: String!
}
```

HotChocolate allows you to rename types to avoid collision auto resolving:

```sdl
type Category @source(name: "Category", schema: "inventory") {
  name: String!
}

type ProductCategory @source(name: "Category", schema: "products") {
  name: String!
  subCategories: [ProductCategory!]!
}
```

### Schema Stitching

In schema stitching type renames can be defined on the gateway:

```csharp{5}
services
    .AddGraphQLServer()
    .AddRemoteSchema(Products)
    .AddRemoteSchema(Inventiory)
    .RenameType("Category","ProductCategory", Products);
```

##Schema Federations
In a federated approach, type renames can be done on the domain service:

```csharp{9}
services
    .AddSingleton(ConnectionMultiplexer.Connect("stitching-redis.services.local"))
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .InitializeOnStartup()
    .PublishSchemaDefinition(
        c => c
            .SetName("products")
            .RenameType("Category", "ProductCategory")
            .AddTypeExtensionsFromFile("./Stitching.graphql")
            .PublishToRedis(
                "Demo",
                sp => sp.GetRequiredService<ConnectionMultiplexer>()));
```

## Rename Fields

Similar to type names, also fields can collide. A type can only declare a field once.
When you bundle domain services together, multiple domain services may declare the same field on the query type.

Let us assume we have a product and an inventory service. Both define a type field called `categories`:

```sdl
type Query {
  categories: [Category!]!
}
```

```sdl
type Query {
  categories: [ProductCategory!]!
}
```

HotChocolate will autoresolve the nameing conflict by prefixing the field with the schema name:

```sdl
type Query {
  categories: [ProductCategory!]! @delegate(schema: "products")
  inventory_categories: [Category!]! @delegate(schema: "inventory", path: "categories")
}
```

HotChocolate allows you to rename fields to avoid collision auto resolving:

```sdl
type Query {
  productCategories: [ProductCategory!]! @source(name: "categories", schema: "products") @delegate(schema: "products")
  categories: [Category!]! @delegate(schema: "inventory")
}
```

### Schema Stitching

In schema stitching field renames can be defined on the gateway:

```csharp{5}
services
    .AddGraphQLServer()
    .AddRemoteSchema(Products)
    .AddRemoteSchema(Inventiory)
    .RenameField("Query", "categories", "productCategories", schemaName: Products)
```

### Schema Federations

In a federated approach, type renames can be done on the domain service:

```csharp{9}
services
    .AddSingleton(ConnectionMultiplexer.Connect("stitching-redis.services.local"))
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .InitializeOnStartup()
    .PublishSchemaDefinition(
        c => c
            .SetName("products")
            .RenameField("Query", "categories", "productCategories")
            .AddTypeExtensionsFromFile("./Stitching.graphql")
            .PublishToRedis(
                "Demo",
                sp => sp.GetRequiredService<ConnectionMultiplexer>()));
```

## Ignore Types

By default, all types of remote schemas are added to the gateway schema.
This can produce types that are not reachable.
You can remove all not reachable types on the gateway:

```csharp{5}
services
    .AddGraphQLServer()
    .AddQueryType(d => d.Name("Query"))
    .AddRemoteSchemasFromRedis("Demo", sp => sp.GetRequiredService<ConnectionMultiplexer>())
    .ModifyOptions(x => x.RemoveUnreachableTypes = true)
```

If you want to remove a specific type from the schema you can also use `IgnoreType`

### Schema Stitching

```csharp{5}
services
    .AddGraphQLServer()
    .AddRemoteSchema(Products)
    .AddRemoteSchema(Inventiory)
    .IgnoreType("Category", schemaName: Products);
```

### Schema Federations

```csharp{9}
services
    .AddSingleton(ConnectionMultiplexer.Connect("stitching-redis.services.local"))
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .InitializeOnStartup()
    .PublishSchemaDefinition(
        c => c
            .SetName("products")
            .IgnoreType("Category")
            .AddTypeExtensionsFromFile("./Stitching.graphql")
            .PublishToRedis(
                "Demo",
                sp => sp.GetRequiredService<ConnectionMultiplexer>()));
```

## Ignore Field

HotChocolate has a convenience API to ignore fields of types.
This can be useful when you want to merge root fields of domain services, but ignore some specific fields

### Schema Stitching

```csharp{5-6}
services
    .AddGraphQLServer()
    .AddRemoteSchema(Products)
    .AddRemoteSchema(Inventiory)
    .IgnoreField("Query", "categories", Products)
    .IgnoreField("Query", "categories", Inventory);
```

# Delegation of Resolvers

The real power of schema stitching is the delegation of resolvers.
You can extend types with fields and redirect calls to a domain service

Let us assume we have a product and an inventory service.

The product service defines the following types

```sdl
type Product {
  upc: Int!
  name: String!
  price: Int!
  weight: Int!
}

type Query {
  products: [Product!]!
}
```

The inventory service defines the following types

```sdl
type InventoryInfo {
  upc: Int!
  isInStock: bool
}

type Query {
  inventoryInfo(upc: Int!): InventoryInfo!
  shippingEsitmate(price: Int!, weight: Int!): InventoryInfo!
}
```

Resolver delegation allows us to combine these schemas into one cohesive schema.

We can extend the product type with `inStock` and `shippingEstimate`

```sdl
extend type Product {
  inStock: Boolean @delegate(schema:"inventory", path: "inventoryInfo(upc: $fields:upc).isInStock")
  shippingEstimate: Int @delegate(schema:"inventory", path: "shippingEstimate(price: $fields:price weight: $fields:weight)")
}
```

This results in the following schema:

```sdl
type Product {
  upc: Int!
  name: String!
  price: Int!
  weight: Int!
  inStock: Boolean
  shippingEstimate: Int
}

type Query {
  products: [Product!]!
}
```

## Delegate Directive

The `@delegate` directive describes where the remote data is found.

```sdl
directive @delegate(
  "The name of the schema to which this field shall be delegated to"
  schema: String
  "The path on the schema where delegation points to"
  path: String!
) on FIELD_DEFINITION
```

The `path` argument can contain references to context data or fields.

### Field Reference ($fields)

```sdl
@delegate(path: "inventoryInfo(upc: $fields:upc).isInStock")
```

With the `$fields` variable, you can access fields of the type you extend.

```sdl{2,7}
type Product {
  upc: Int!
  name: String!
}

extend type Product {
  inStock: Boolean @delegate(schema:"inventory", path: "inventoryInfo(upc: $fields:upc).isInStock")
}
```

### Argument Reference ($arguments)

```sdl
@delegate(path: "inventoryInfo(upc: $arguments:sku).isInStock")
```

With the `$fields` variable you can access fields of the type you extend.

```sdl{2,7}
extend type Query {
  isProductInStock(sku:String!): Boolean @delegate(schema:"inventory", path: "inventoryInfo(upc: $arguments:upc)")
}
```

### Context Data Reference ($contextData)

Every request contains context data. Context data can be set in resolvers or with a `IHttpRequestInterceptor`

```sdl
extend type Query {
  me: User! @delegate(schema: "users", path: "user(id: $contextData:UserId)")
}
```

**UseRequest**

```csharp
services
    .AddGraphQLServer()
    .UseRequest(next => context =>
    {
        context.ContextData["UserId"] = context.GetLoggedInUserId();
        return next(context);
    })
    ...
```

**RequestInterceptor**

```csharp{10}
public class RequestInterceptor : DefaultHttpRequestInterceptor
{
    public ValueTask OnCreateAsync(
        HttpContext context,
        IRequestExecutor requestExecutor,
        IQueryRequestBuilder requestBuilder,
        CancellationToken cancellationToken)
    {
        string userId = context.GetLoggedInUserId();
        requestBuilder.SetProperty("UserId", userId);

        return base.OnCreateAsync(
          context, requestExecutor, requestBuilder, cancellationToken);
    }
}
```

```csharp
services
    .AddGraphQLServer()
    .AddHttpRequestInterceptor<RequestInterceptor>()
    ...
```

**Static Context Data**
Context data can also be set directly on the schema builder.

```csharp
services
    .AddGraphQLServer()
    .SetContextData("foo", "bar")
    ...
```

### Scoped Context Data Reference ($scopedContext)

Scoped context data can be set in a resolver and will be available in all resolvers in the subtree.
You have to use scoped context data when a resolver depends on a field that is higher up than just the parent.
You can use field middlewares to set scoped context data.

Let's assume you have a message and account service.
The message holds a field `messageInfo` and knows the id of the creator of the message.
You want to extend the `messageInfo` with the user from the account service.

**Schema**

```sdl
type Message {
  content: String!
  createdById: ID!
  messageInfo: MessageInfo!
}

type MessageInfo {
  createdAt: DateTime!
}
```

**Extensions**

```sdl
extend type MessageInfo {
  createdBy: User @delegate(schema:"accounts", path: "userById(upc: $scopedContextData:upc).isInStock")
}
```

**UseField**

This middleware is executed for each field.

```csharp
services
    .AddGraphQLServer()
    .UseField(next => async context =>
        {
            if(context.Field.Type.NamedType() is ObjectType objectType &&
              objectType.Name.Equals("Message") &&
              context.Result is IDictionary<string, object> data &&
              data.TryGetValue("createdById", out object value))
            {
                context.ScopedContextData =
                    context.ScopedContextData.SetItem("createdById", value);
            }

            await next.Invoke(context);
        })
```

**Type Interceptor**

The middleware of `UseField` is executed on each field and created overhead.
It would be better if the middleware is only applied to the field that needs it.
You can use a schema interceptor to apply the middleware to the fields that use it.

```csharp
public class MessageMiddlwareInterceptor : TypeInterceptor
{
    public override bool CanHandle(ITypeSystemObjectContext context)
    {
        return context.Type is INamedType { Name: { Value: "Message" } };
    }
    public override void OnBeforeCompleteType(
        ITypeCompletionContext completionContext,
        DefinitionBase? definition,
        IDictionary<string, object?> contextData)
    {
        if (definition is ObjectTypeDefinition otd)
        {
            var field = otd.Fields
                .FirstOrDefault(x => x.Name.Value == "messageInfo");
            if (field is { } messageInfo)
            {
                messageInfo.MiddlewareComponents.Insert(
                    0,
                    next => async context =>
                    {
                        if(context.Result is IDictionary<string, object> data &&
                          data.TryGetValue("createdById", out object value))
                        {
                            context.ScopedContextData =
                                context.ScopedContextData.SetItem("createdById", value);
                        }

                        await next.Invoke(context);
                    });
            }
        }
    }
}
```

## Configuration

You can configure the schema extensions either on the gateway or on the domain service if you use federations.
Type extensions can either be strings, files or resources

- `AddTypeExtensionFromFile("./Stitching.graphql");`
- `AddTypeExtensionFromResource(assembly, key);`
- `AddTypeExtensionFromString("extend type Product {foo : String}");`

### Schema Stitching

**Gateway:**

```csharp
services
    .AddGraphQLServer()
    .AddRemoteSchema(Products)
    .AddRemoteSchema(Inventory)
    // Adds a type extension.
    .AddTypeExtensionsFromFile("./Stitching.graphql")
```

### Schema Federations

**Inventory Domain Service:**

```csharp
services
    .AddSingleton(ConnectionMultiplexer.Connect("stitching-redis.services.local"))
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .InitializeOnStartup()
    .PublishSchemaDefinition(
        c => c
            .SetName("inventory")
            // Ignores the root types. This removes `inStock` and `shippingEsitmate`
            // from the `Query` type of the Gateway
            .IgnoreRootTypes()
            // Adds a type extension.
            .AddTypeExtensionsFromFile("./Stitching.graphql")
            .PublishToRedis(
                "Demo",
                sp => sp.GetRequiredService<ConnectionMultiplexer>()));
```

If you use the `@delegate` directive in federations you can omit the `schema:` argument.
