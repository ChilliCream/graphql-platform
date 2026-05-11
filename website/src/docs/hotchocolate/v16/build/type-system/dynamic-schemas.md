---
title: "Dynamic Schemas"
---

Dynamic schemas in Hot Chocolate allow you to construct parts of the GraphQL type system based on metadata available at runtime. Choose this approach when the structure of your schema must adapt to changes, such as product attributes managed through an admin system, fields provided by plugins, tenant-specific domain models, or external catalogs that define typed fields.

Avoid using dynamic schemas for scenarios like per-request authorization, filtering, or field visibility. If your public type system remains consistent, keep the schema static and handle request-specific logic in resolvers, middleware, filtering, authorization, or global state.

A typical dynamic schema workflow looks like this:

```text
metadata source
  -> validation and normalization
  -> immutable metadata snapshot
  -> ITypeModule.CreateTypesAsync
  -> generated configuration objects
  -> type system members
  -> request executor
```

When metadata updates, the module triggers `TypesChanged`. Hot Chocolate then retires the current request executor and constructs a new one using the latest module output.

# What You Will Build

The examples in this guide demonstrate building a product catalog where custom product attributes are defined by trusted metadata. The compiled application can resolve a product, but the specific product attribute fields are determined by configuration.

```graphql
# Generated from metadata

type Product {
  id: ID!
  name: String!
  color: String
  weight: Float
  releaseDate: DateTime
}

input ProductPatchInput {
  color: String
  weight: Float
  releaseDate: DateTime
}

type Query {
  product(id: ID!): Product
}

type Mutation {
  patchProduct(id: ID!, input: ProductPatchInput!): Product
}
```

The same pattern works for generated object types, generated type extensions, generated input types, and generated field arguments.

# Use Dynamic Schemas Only When the Schema Shape Is Dynamic

Dynamic schemas offer significant flexibility by leveraging low-level type system APIs. However, you should prefer the standard schema APIs unless your type system structure genuinely depends on runtime metadata.

| Scenario                                               | Recommended Approach                                                                        | Reason                                                              |
| ------------------------------------------------------ | ------------------------------------------------------------------------------------------- | ------------------------------------------------------------------- |
| CLR model and fields are known at compile time         | [Object types](./object-types), [input object types](./input-object-types), and descriptors | Standard APIs are clearer and provide more schema validation.       |
| Add or replace a few fields on a known type            | [Type extensions](./extending-types)                                                        | Most schema authoring remains static.                               |
| Hide data per user, role, tenant, or request           | Authorization, resolvers, middleware, filtering, request state, or global state             | The schema should not be rebuilt for each request.                  |
| Register known generated CLR types                     | Source generation, auto-registration, or generated `AddTypes()` code                        | Types are known at build time or startup.                           |
| Combine several GraphQL services                       | [Fusion](/docs/hotchocolate/v16/_leagcy/fusion) or composition                              | This is a service composition problem, not runtime type generation. |
| Publish a stable schema version from external metadata | Dynamic schemas                                                                             | The type system changes when the metadata version changes.          |

# Define Runtime Metadata

Treat metadata as a source of schema definition. Always validate, normalize, version, and publish immutable snapshots of your metadata. Avoid exposing raw database column names, vendor-specific field names, or unreviewed tenant metadata directly. Always use explicit mappings.

```csharp
public sealed record ProductCatalogSnapshot(
    int Version,
    IReadOnlyList<AttributeDefinition> Attributes);

public sealed record AttributeDefinition(
    string FieldName,
    string? Description,
    string Scalar,
    bool IsNullable,
    bool IsList,
    string SourceKey,
    bool IsWritable);

public sealed class Product
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public IReadOnlyDictionary<string, object?> Attributes { get; init; } =
        new Dictionary<string, object?>();
}
```

A snapshot matching the SDL example above might look like this:

```csharp
var snapshot = new ProductCatalogSnapshot(
    Version: 42,
    Attributes:
    [
        new("color", "Display color.", "String", true, false, "color", true),
        new("weight", "Weight in kilograms.", "Float", true, false, "weight", true),
        new("releaseDate", "First availability date.", "DateTime", true, false, "release_date", true)
    ]);
```

# Understand the Type Module

`ITypeModule` serves as the runtime hook for type generation.

```csharp
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

public interface ITypeModule
{
    event EventHandler<EventArgs>? TypesChanged;

    ValueTask<IReadOnlyCollection<ITypeSystemMember>> CreateTypesAsync(
        IDescriptorContext context,
        CancellationToken cancellationToken);
}
```

The `CreateTypesAsync` method returns the type system members for the current metadata version. This collection may include object types, object type extensions, input object types, input object type extensions, and other type system members.

You can implement `ITypeModule` directly or inherit from `TypeModule`, which provides a protected `OnTypesChanged()` method.

```csharp
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

public sealed class ProductCatalogTypeModule : TypeModule
{
    private readonly IProductCatalogMetadataProvider _metadata;

    public ProductCatalogTypeModule(IProductCatalogMetadataProvider metadata)
    {
        _metadata = metadata;
    }

    public override async ValueTask<IReadOnlyCollection<ITypeSystemMember>> CreateTypesAsync(
        IDescriptorContext context,
        CancellationToken cancellationToken)
    {
        ProductCatalogSnapshot snapshot =
            await _metadata.GetCurrentSnapshotAsync(cancellationToken);

        return ProductCatalogTypes.Create(snapshot);
    }

    public void NotifyCatalogChanged()
    {
        OnTypesChanged();
    }
}
```

Raise `TypesChanged` only after a new, validated snapshot is available. Never mutate a snapshot that might still be in use by the active request executor.

# Register a Type Module

Register your module with the request executor builder.

```csharp
// Program.cs
builder.Services.AddSingleton<IProductCatalogMetadataProvider, ProductCatalogMetadataProvider>();

builder
    .AddGraphQL()
    .AddQueryType()
    .AddMutationType()
    .AddTypeModule<ProductCatalogTypeModule>();
```

If your module requires custom construction, use the factory overload:

```csharp
builder
    .AddGraphQL()
    .AddTypeModule(sp => new ProductCatalogTypeModule(
        sp.GetRequiredService<IProductCatalogMetadataProvider>()));
```

# Generate an Object Type from Metadata

Dynamic type members are created from configuration objects in `HotChocolate.Types.Descriptors.Configurations`. The `CreateUnsafe` APIs are called unsafe because Hot Chocolate trusts you to provide valid names, valid type references, unique fields, correct resolvers, and stable metadata for the lifetime of the generated executor.

```csharp
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Descriptors.Configurations;

public static class ProductCatalogTypes
{
    public static IReadOnlyCollection<ITypeSystemMember> Create(
        ProductCatalogSnapshot snapshot)
    {
        var product = CreateProductType(snapshot);
        var patchInput = CreateProductPatchInput(snapshot);
        var query = CreateQueryExtension();
        var mutation = CreateMutationExtension();

        return [product, patchInput, query, mutation];
    }

    private static ObjectType CreateProductType(ProductCatalogSnapshot snapshot)
    {
        var configuration = new ObjectTypeConfiguration(
            "Product",
            "A product in the catalog.",
            typeof(Product));

        configuration.Fields.Add(new ObjectFieldConfiguration(
            "id",
            "The product identifier.",
            TypeReference.Parse("ID!"),
            pureResolver: context => context.Parent<Product>().Id));

        configuration.Fields.Add(new ObjectFieldConfiguration(
            "name",
            "The product name.",
            TypeReference.Parse("String!"),
            pureResolver: context => context.Parent<Product>().Name));

        foreach (AttributeDefinition attribute in snapshot.Attributes)
        {
            AttributeDefinition captured = attribute;

            configuration.Fields.Add(new ObjectFieldConfiguration(
                captured.FieldName,
                captured.Description,
                ToOutputTypeReference(captured),
                pureResolver: context => ResolveAttribute(context, captured)));
        }

        return ObjectType.CreateUnsafe(configuration);
    }

    private static object? ResolveAttribute(
        IResolverContext context,
        AttributeDefinition attribute)
    {
        Product product = context.Parent<Product>();

        return product.Attributes.TryGetValue(attribute.SourceKey, out object? value)
            ? value
            : null;
    }

    private static TypeReference ToOutputTypeReference(AttributeDefinition attribute)
        => TypeReference.Parse(ToTypeSyntax(attribute));

    private static string ToTypeSyntax(AttributeDefinition attribute)
    {
        string scalar = attribute.Scalar switch
        {
            "String" => "String",
            "Int" => "Int",
            "Float" => "Float",
            "Boolean" => "Boolean",
            "DateTime" => "DateTime",
            _ => throw new InvalidOperationException(
                $"The scalar '{attribute.Scalar}' is not allowed.")
        };

        if (attribute.IsList)
        {
            string listType = "[" + scalar + "!]";
            return attribute.IsNullable ? listType : listType + "!";
        }

        return attribute.IsNullable ? scalar : scalar + "!";
    }
}
```

The scalar mapping is an allowlist. If metadata can reference custom scalars, register those scalars before a generated field references them.

Common `TypeReference.Parse(...)` strings look like this:

| Metadata                          | GraphQL type syntax |
| --------------------------------- | ------------------- |
| Nullable string                   | `String`            |
| Required string                   | `String!`           |
| Nullable list of required strings | `[String!]`         |
| Required list of required strings | `[String!]!`        |
| Nullable custom scalar            | `DateTime`          |

# Extend an Existing Type from a Type Module

If most of the `Product` type remains unchanged, keep it static and generate only the metadata-driven fields as an extension.

```csharp
private static ObjectTypeExtension CreateProductExtension(
    ProductCatalogSnapshot snapshot)
{
    var configuration = new ObjectTypeConfiguration("Product");

    foreach (AttributeDefinition attribute in snapshot.Attributes)
    {
        AttributeDefinition captured = attribute;

        configuration.Fields.Add(new ObjectFieldConfiguration(
            captured.FieldName,
            captured.Description,
            ToOutputTypeReference(captured),
            pureResolver: context => ResolveAttribute(context, captured)));
    }

    return ObjectTypeExtension.CreateUnsafe(configuration);
}
```

Return this extension from `CreateTypesAsync` along with any generated input types or root type extensions. This approach is helpful when the object’s identity, core fields, and most resolvers do not change.

# Wire Dynamic Field Resolvers

Dynamic fields use the same resolver concepts as static fields. For a complete overview of resolver behavior, see [Resolvers](/docs/hotchocolate/v16/build/resolvers).

Use a pure resolver for in-memory operations, such as dictionary lookups. Use an async resolver for service calls, I/O, batching, or when you need cancellation support.

```csharp
private static ObjectFieldConfiguration CreateInventoryField(
    AttributeDefinition attribute)
{
    AttributeDefinition captured = attribute;

    var field = new ObjectFieldConfiguration(
        "inventory",
        "Inventory for a warehouse.",
        TypeReference.Parse("Int"),
        resolver: async context =>
        {
            Product product = context.Parent<Product>();
            string warehouse = context.ArgumentValue<string>("warehouse");
            IInventoryService inventory = context.Service<IInventoryService>();
            CancellationToken cancellationToken = context.RequestAborted;

            return await inventory.GetInventoryAsync(
                product.Id,
                captured.SourceKey,
                warehouse,
                cancellationToken);
        });

    field.Arguments.Add(new ArgumentConfiguration(
        "warehouse",
        "The warehouse code.",
        TypeReference.Parse("String!")));

    return field;
}
```

Always capture immutable, per-version metadata in each delegate. Avoid closing over mutable lists, providers, or objects that may change when a new schema version is published. If a dynamic field calls the same external service for many parent objects, use [DataLoader](/docs/hotchocolate/v16/build/dataloader) patterns within the resolver.

# Generate Input Types and Arguments

Generated input types define GraphQL syntax and input coercion. Domain validation should remain in resolvers or application services, and authorization must always be explicit. If a generated input needs to bind to a runtime value such as a dictionary, configure the input object’s runtime type and instance factory accordingly.

```csharp
private static InputObjectType CreateProductPatchInput(
    ProductCatalogSnapshot snapshot)
{
    AttributeDefinition[] writableAttributes = snapshot.Attributes
        .Where(t => t.IsWritable)
        .ToArray();

    var configuration = new InputObjectTypeConfiguration(
        "ProductPatchInput",
        "Writable product attributes.",
        typeof(Dictionary<string, object?>))
    {
        CreateInstance = values =>
        {
            var input = new Dictionary<string, object?>(StringComparer.Ordinal);

            for (var i = 0; i < writableAttributes.Length; i++)
            {
                input[writableAttributes[i].SourceKey] = values[i];
            }

            return input;
        }
    };

    foreach (AttributeDefinition attribute in writableAttributes)
    {
        configuration.Fields.Add(new InputFieldConfiguration(
            attribute.FieldName,
            attribute.Description,
            TypeReference.Parse(ToTypeSyntax(attribute))));
    }

    return InputObjectType.CreateUnsafe(configuration);
}
```

Generated input types become useful when they are referenced by generated field arguments.

```csharp
private static ObjectTypeExtension CreateMutationExtension()
{
    var configuration = new ObjectTypeConfiguration("Mutation");

    var field = new ObjectFieldConfiguration(
        "patchProduct",
        "Updates writable product attributes.",
        TypeReference.Parse("Product"),
        resolver: async context =>
        {
            string id = context.ArgumentValue<string>("id");
            Dictionary<string, object?> input =
                context.ArgumentValue<Dictionary<string, object?>>("input");
            IProductService products = context.Service<IProductService>();

            return await products.PatchProductAsync(
                id,
                input,
                context.RequestAborted);
        });

    field.Arguments.Add(new ArgumentConfiguration(
        "id",
        "The product identifier.",
        TypeReference.Parse("ID!")));

    field.Arguments.Add(new ArgumentConfiguration(
        "input",
        "Attributes to update.",
        TypeReference.Parse("ProductPatchInput!")));

    configuration.Fields.Add(field);

    return ObjectTypeExtension.CreateUnsafe(configuration);
}
```

For static input modeling, default values, `Optional<T>`, records, and `@oneOf`, see [Input Object Types](./input-object-types).

# Add Generated Root Fields

A type module can also provide extensions for root operation types. The following example adds the `product(id: ID!): Product` field as shown in the generated SDL.

```csharp
private static ObjectTypeExtension CreateQueryExtension()
{
    var configuration = new ObjectTypeConfiguration("Query");

    var field = new ObjectFieldConfiguration(
        "product",
        "Gets a product by id.",
        TypeReference.Parse("Product"),
        resolver: async context =>
        {
            string id = context.ArgumentValue<string>("id");
            IProductService products = context.Service<IProductService>();

            return await products.GetProductAsync(id, context.RequestAborted);
        });

    field.Arguments.Add(new ArgumentConfiguration(
        "id",
        "The product identifier.",
        TypeReference.Parse("ID!")));

    configuration.Fields.Add(field);

    return ObjectTypeExtension.CreateUnsafe(configuration);
}
```

If you generate a root type extension, ensure the corresponding root type exists. The earlier registration uses `.AddQueryType()` and `.AddMutationType()` to create the `Query` and `Mutation` root types.

# Update the Schema When Metadata Changes

Always publish a new immutable snapshot before raising `TypesChanged`. The following expanded module subscribes to provider notifications and calls `OnTypesChanged()` when a change occurs.

```csharp
public interface IProductCatalogMetadataProvider
{
    ValueTask<ProductCatalogSnapshot> GetCurrentSnapshotAsync(
        CancellationToken cancellationToken);

    event EventHandler? SnapshotChanged;
}

public sealed class ProductCatalogTypeModule : TypeModule, IDisposable
{
    private readonly IProductCatalogMetadataProvider _metadata;

    public ProductCatalogTypeModule(IProductCatalogMetadataProvider metadata)
    {
        _metadata = metadata;
        _metadata.SnapshotChanged += OnSnapshotChanged;
    }

    public override async ValueTask<IReadOnlyCollection<ITypeSystemMember>> CreateTypesAsync(
        IDescriptorContext context,
        CancellationToken cancellationToken)
    {
        ProductCatalogSnapshot snapshot =
            await _metadata.GetCurrentSnapshotAsync(cancellationToken);

        return ProductCatalogTypes.Create(snapshot);
    }

    private void OnSnapshotChanged(object? sender, EventArgs args)
    {
        OnTypesChanged();
    }

    public void Dispose()
    {
        _metadata.SnapshotChanged -= OnSnapshotChanged;
    }
}
```

Debounce file watchers, database notifications, and external events so that each metadata version triggers only one rebuild. Log the metadata version, the number of generated types and fields, rebuild start and completion, and any validation errors.

If warmup tasks are configured, they will run again when the schema is rebuilt. During background warmup after a runtime rebuild, requests continue to use the old request executor until the new, warmed executor is ready. See [Warmup](/docs/hotchocolate/v16/build/performance/warmup) for operational details.

# Validate and Protect Generated Schemas

Always validate metadata before creating type system members.

| Risk                                    | Validation or Design Rule                                 | Failure Mode                                            |
| --------------------------------------- | --------------------------------------------------------- | ------------------------------------------------------- |
| Duplicate generated field name          | Normalize and check names before type creation.           | Schema build error or unintended field conflict.        |
| Invalid GraphQL name                    | Validate type, field, argument, and input field names.    | Startup or rebuild failure.                             |
| Invalid `TypeReference.Parse` string    | Centralize type mapping and test every mapping.           | Startup or rebuild failure.                             |
| Unregistered custom scalar              | Allowlist scalars and register custom scalars first.      | Type completion failure.                                |
| Raw external names exposed              | Use explicit GraphQL names and descriptions.              | Internal schema or vendor details leak to clients.      |
| Metadata changes too frequently         | Debounce and publish versioned snapshots.                 | Rebuild storms.                                         |
| Mutable metadata captured by a resolver | Capture immutable per-version values.                     | Old executors resolve with new metadata by mistake.     |
| Tenant-specific shape shared globally   | Isolate schema shape and metadata snapshots per executor. | Fields from one tenant appear in another tenant schema. |

Set limits on the number of generated types, fields, and arguments. Apply authorization and filtering rules to generated fields or resolvers. Treat tenant metadata as input to a schema version, not as a request-time permission system.

# Test a Dynamic Schema

Test schema generation as a contract, not by string concatenation.

```csharp
[Fact]
public async Task Schema_Should_Contain_Generated_Product_Fields()
{
    // arrange
    var snapshot = ProductCatalogFixtures.Version42();

    // act
    var schema = await new ServiceCollection()
        .AddSingleton<IProductCatalogMetadataProvider>(
            new StubProductCatalogMetadataProvider(snapshot))
        .AddGraphQL()
        .AddQueryType()
        .AddMutationType()
        .AddTypeModule<ProductCatalogTypeModule>()
        .BuildSchemaAsync();

    // assert
    schema.ToString().MatchInlineSnapshot(
        """
        schema {
          query: Query
          mutation: Mutation
        }

        type Product {
          id: ID!
          name: String!
          color: String
          weight: Float
          releaseDate: DateTime
        }
        """);
}
```

Add execution tests for representative queries and mutations, invalid metadata, input coercion, and a rebuild test that publishes a new snapshot and raises `TypesChanged`. Test dynamic field resolver logic separately when possible.

# Troubleshooting Dynamic Schemas

## My New Fields Do Not Appear

- Ensure the module is registered with `.AddTypeModule<T>()`.
- Confirm that `OnTypesChanged()` or `TypesChanged` is raised after publishing the new snapshot.
- Make sure `CreateTypesAsync` reads the latest metadata version, not a cached one.
- Check that the generated field is included in the returned type or extension.

## The Schema Fails to Rebuild

- Look for duplicate type, field, argument, or input field names.
- Check for invalid GraphQL names after normalization.
- Validate `TypeReference.Parse(...)` strings and referenced custom scalars.
- Ensure generated extensions target types that exist in the schema.
- Review schema validation options if strict validation is enabled.

## Schema Rebuilds Happen Too Often

- Do not raise `TypesChanged` for every request.
- Debounce file watchers and external notifications.
- Publish immutable, versioned snapshots only after metadata is valid.
- Log version numbers to detect repeated notifications for the same metadata.

## Fields Resolve Null or Wrong Values

- Check the parent runtime type and source key mapping.
- Ensure resolver closures capture the correct immutable field metadata.
- Verify argument names and type references match the generated field configuration.
- Check for missing DI services or incorrect service lifetimes for async resolvers.
- Use DataLoader or batching when dynamic fields call external systems for many parent objects.

## Tenant-Specific Schemas Leak Fields

- Separate schema shape decisions from request authorization.
- Validate tenant metadata before publishing a schema version.
- Avoid sharing mutable metadata across tenants or executors.
- Prefer request authorization and filtering when the type system should remain the same for every tenant.

# API Reference Summary

| API                                          | Purpose                                                 | Notes                                                    |
| -------------------------------------------- | ------------------------------------------------------- | -------------------------------------------------------- |
| `ITypeModule`                                | Contract for generated type system members.             | Primary abstraction.                                     |
| `TypeModule`                                 | Convenience base class.                                 | Optional, exposes `OnTypesChanged()`.                    |
| `CreateTypesAsync`                           | Creates current dynamic types and extensions.           | Receives `IDescriptorContext` and `CancellationToken`.   |
| `TypesChanged`                               | Signals that schema metadata changed.                   | Raise after publishing a valid new snapshot.             |
| `.AddTypeModule<T>()`                        | Registers a module.                                     | Use on the GraphQL request executor builder.             |
| `ObjectTypeConfiguration`                    | Configures generated object types and extensions.       | Configuration type for generated schemas.                |
| `InputObjectTypeConfiguration`               | Configures generated input object types and extensions. | Configuration type for generated schemas.                |
| `ObjectFieldConfiguration`                   | Configures generated object fields.                     | Supports async and pure resolver delegates.              |
| `InputFieldConfiguration`                    | Configures generated input fields.                      | Inherits argument configuration behavior.                |
| `ArgumentConfiguration`                      | Configures generated field arguments.                   | Use for generated input type arguments.                  |
| `TypeReference.Parse(...)`                   | Parses GraphQL type syntax.                             | Validate all strings before publishing.                  |
| `ObjectType.CreateUnsafe(...)`               | Creates an object type from configuration.              | Low-level API.                                           |
| `ObjectTypeExtension.CreateUnsafe(...)`      | Creates an object type extension from configuration.    | Useful for generated fields on known types.              |
| `InputObjectType.CreateUnsafe(...)`          | Creates an input type from configuration.               | Low-level API.                                           |
| `InputObjectTypeExtension.CreateUnsafe(...)` | Creates an input type extension from configuration.     | Use when extending an existing input type from metadata. |

# Next Steps

- Refer to [Object Types](./object-types) for descriptor-based output schemas.
- See [Input Object Types](./input-object-types) for static input modeling and validation patterns.
- Use [Arguments](./arguments) for static argument definitions.
- Explore [Resolvers](/docs/hotchocolate/v16/build/resolvers) for parent values, dependency injection, arguments, cancellation, errors, and DataLoader patterns.
- Review [Options](/docs/hotchocolate/v16/build/server-configuration/schema-options) for strict validation and initialization behavior.
- Learn about [Warmup](/docs/hotchocolate/v16/build/performance/warmup) for executor rebuild warmup, readiness, and operational performance.
- Use [Fusion](/docs/hotchocolate/v16/_leagcy/fusion) to compose multiple GraphQL services.
