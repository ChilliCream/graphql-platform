---
title: "Dynamic Schemas"
---

In multi-tenant or CMS-like applications, the GraphQL schema may need to change at runtime based on configuration, database structure, or per-tenant requirements. Hot Chocolate supports dynamic schemas through the `ITypeModule` interface, which lets you provide types programmatically and trigger schema reloads when the underlying data changes.

# The ITypeModule Interface

`ITypeModule` is the entry point for dynamically providing types to the schema building process. It has two members:

- `TypesChanged`: An event that signals when types have changed and the current schema should be replaced.
- `CreateTypesAsync`: A method called during schema construction to create types for the new schema instance.

When you fire the `TypesChanged` event, Hot Chocolate phases out the old schema and builds a new one using the updated types from your module. This gives you hot-reload behavior without restarting the application.

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddTypeModule<MyTypeModule>();
```

# Creating Types from a JSON File

This example reads type definitions from a JSON file. In a real application, the JSON might come from a database, an admin UI, or an external configuration service.

```csharp
// Infrastructure/JsonTypeModule.cs
public class JsonTypeModule : ITypeModule
{
    private readonly string _file;

    public JsonTypeModule(string file)
    {
        _file = file;
    }

    public event EventHandler<EventArgs>? TypesChanged;

    public async ValueTask<IReadOnlyCollection<ITypeSystemMember>> CreateTypesAsync(
        IDescriptorContext context,
        CancellationToken cancellationToken)
    {
        var types = new List<ITypeSystemMember>();

        await using var file = File.OpenRead(_file);
        using var json = await JsonDocument.ParseAsync(
            file, cancellationToken: cancellationToken);

        foreach (var type in json.RootElement.EnumerateArray())
        {
            var typeDefinition = new ObjectTypeDefinition(
                type.GetProperty("name").GetString()!);

            foreach (var field in type.GetProperty("fields").EnumerateArray())
            {
                typeDefinition.Fields.Add(
                    new ObjectFieldDefinition(
                        field.GetString()!,
                        type: TypeReference.Parse("String!"),
                        pureResolver: ctx => "foo"));
            }

            types.Add(
                type.GetProperty("extension").GetBoolean()
                    ? ObjectTypeExtension.CreateUnsafe(typeDefinition)
                    : ObjectType.CreateUnsafe(typeDefinition));
        }

        return types;
    }
}
```

When the JSON file changes, call `TypesChanged` to trigger a schema rebuild. You could use a file watcher or a polling mechanism to detect changes.

# Unsafe Type Creation

The `CreateUnsafe` method creates types directly from definition objects, bypassing the standard descriptor API. This gives you full control over the type structure but requires understanding the Hot Chocolate type system internals.

## Creating an Object Type

```csharp
var objectTypeDef = new ObjectTypeDefinition("Product")
{
    Description = "Represents a product in the catalog.",
    RuntimeType = typeof(Dictionary<string, object>)
};

var idField = new ObjectFieldDefinition(
    "id",
    "Unique identifier for the product.",
    TypeReference.Parse("ID!"),
    pureResolver: ctx => ctx.Parent<Dictionary<string, object>>()["id"]);

var nameField = new ObjectFieldDefinition(
    "name",
    "Name of the product.",
    TypeReference.Parse("String!"),
    pureResolver: ctx => ctx.Parent<Dictionary<string, object>>()["name"]);

objectTypeDef.Fields.Add(idField);
objectTypeDef.Fields.Add(nameField);

var productType = ObjectType.CreateUnsafe(objectTypeDef);
```

## Adding Fields with Arguments

```csharp
var discountArg = new ArgumentDefinition(
    "discount",
    "Discount percentage to apply.",
    TypeReference.Parse("Float!"));

var discountPriceField = new ObjectFieldDefinition(
    "discountPrice",
    "Price after discount.",
    TypeReference.Parse("Float!"),
    pureResolver: ctx =>
    {
        var product = ctx.Parent<Dictionary<string, object>>();
        var discountPct = ctx.ArgumentValue<float>("discount");
        var price = (float)product["price"];
        return price * (1 - discountPct / 100);
    })
{
    Arguments = { discountArg }
};

objectTypeDef.Fields.Add(discountPriceField);
```

## Creating an Input Object Type

```csharp
var inputTypeDef = new InputObjectTypeDefinition("ProductInput")
{
    Description = "Input for creating or updating a product.",
    RuntimeType = typeof(Dictionary<string, object>)
};

inputTypeDef.Fields.Add(new InputFieldDefinition(
    "name", "Name of the product.", TypeReference.Parse("String!")));

inputTypeDef.Fields.Add(new InputFieldDefinition(
    "price", "Price of the product.", TypeReference.Parse("Float!")));

var productInputType = InputObjectType.CreateUnsafe(inputTypeDef);
```

## Resolver Types

Hot Chocolate supports two resolver delegate types for dynamically created fields:

**Async resolvers** handle asynchronous operations like database queries or service calls:

```csharp
var reviewsField = new ObjectFieldDefinition(
    "reviews",
    "Reviews for the product.",
    TypeReference.Parse("[Review!]"),
    resolver: async ctx =>
    {
        var productId = ctx.Parent<Dictionary<string, object>>()["id"];
        var service = ctx.Service<IReviewService>();
        return await service.GetReviewsAsync(productId);
    });
```

**Pure resolvers** handle synchronous, side-effect-free operations. The execution engine optimizes these for better performance:

```csharp
var nameField = new ObjectFieldDefinition(
    "name",
    "Name of the product.",
    TypeReference.Parse("String!"),
    pureResolver: ctx => ctx.Parent<Dictionary<string, object>>()["name"]);
```

Use pure resolvers when you do not need async operations or service access. Use async resolvers when you need to call services, databases, or perform any I/O.

## Combining Types in a Mutation

```csharp
var createProductField = new ObjectFieldDefinition(
    "createProduct",
    "Creates a new product.",
    TypeReference.Parse("Product!"),
    resolver: async ctx =>
    {
        var input = ctx.ArgumentValue<Dictionary<string, object>>("input");
        var service = ctx.Service<IProductService>();
        return await service.CreateProductAsync(input);
    })
{
    Arguments =
    {
        new ArgumentDefinition(
            "input",
            "Input for creating the product.",
            TypeReference.Parse("ProductInput!"))
    }
};

var mutationDef = new ObjectTypeDefinition("Mutation")
{
    RuntimeType = typeof(object)
};
mutationDef.Fields.Add(createProductField);

var mutationType = ObjectType.CreateUnsafe(mutationDef);

builder
    .AddGraphQL()
    .AddQueryType()
    .AddMutationType(mutationType)
    .AddType(productInputType)
    .AddType(productType);
```

# Next Steps

- **Need to extend existing types?** See [Extending Types](/docs/hotchocolate/v16/defining-a-schema/extending-types).
- **Need to define types with the descriptor API?** See [Object Types](/docs/hotchocolate/v16/defining-a-schema/object-types).
- **Need to understand type modules in depth?** Explore the `ITypeModule` interface in the Hot Chocolate source code under `src/HotChocolate/Core/src/Types/`.
