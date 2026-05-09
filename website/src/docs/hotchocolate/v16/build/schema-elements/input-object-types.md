---
title: "Input Object Types"
---

Input object types represent structured values that clients send to your schema. Use them when a single argument must include several related fields, such as product creation details, checkout information, or selectors with multiple possible keys.

```graphql
input CreateProductInput {
  name: String!
  description: String
  price: Decimal!
  brandId: ID!
}

type Mutation {
  createProduct(input: CreateProductInput!): CreateProductPayload!
}
```

This page explains input object types as public schema contracts. For details on resolver argument binding, see [Arguments](./arguments). For mutation root fields, payloads, and mutation conventions, see [Mutations](./operations-mutations).

# When to Use an Input Object Type

Choose an input object when multiple values together describe a single operation or a cohesive value. Use specific names that reflect the use case, such as `CreateProductInput`, `UpdateProductInput`, `ProductSelectorInput`, or `CreateOrderInput`.

Do not expose persistence models, EF entities, service request types, or output object types directly as input objects. Input objects are contracts for clients. Design them based on what the client needs to provide, not on your server's storage model.

| Need                                      | Prefer                                     | Notes                                                                                          |
| ----------------------------------------- | ------------------------------------------ | ---------------------------------------------------------------------------------------------- |
| One independent value                     | Scalar or enum argument                    | See [Arguments](./arguments).                                                                  |
| Several values for one operation or value | Input object type                          | Use this page.                                                                                 |
| Values returned to clients                | Output object type                         | See [Object Types](./object-types).                                                            |
| Data filtering or sorting operators       | `FilterInputType<T>` or `SortInputType<T>` | See [Filtering](../resolvers-and-data/filtering) and [Sorting](../resolvers-and-data/sorting). |

# How Input Objects Differ from Output Objects

Input objects are sent from the client to the server, while output objects are sent from the server to the client.

GraphQL enforces specific rules for input objects:

- Input fields represent values, not resolver fields.
- Input fields cannot have arguments.
- Input fields may only use input-compatible types: scalars, enums, lists, and other input objects.
- Output-only types, interfaces, and unions are not allowed within input objects.

If you need mutually exclusive input options, use [`@oneOf`](#use-oneof-for-mutually-exclusive-input-choices). Do not use `@oneOf` for output polymorphism; output polymorphism is handled by output interfaces and unions.

# Define an Input Object from a C# Class or Record

Any C# class or record used as a resolver parameter becomes an input object type, unless Hot Chocolate treats it as a scalar, enum, or service.

```csharp
// Types/CreateProductInput.cs
public sealed record CreateProductInput(
    string Name,
    string? Description,
    decimal Price,
    [property: ID<Brand>] int BrandId);

// Types/ProductMutations.cs
[MutationType]
public static partial class ProductMutations
{
    public static async Task<CreateProductPayload> CreateProductAsync(
        CreateProductInput input,
        ProductService products,
        CancellationToken cancellationToken)
    {
        var product = await products.CreateAsync(
            input.Name,
            input.Description,
            input.Price,
            input.BrandId,
            cancellationToken);

        return new CreateProductPayload(product);
    }
}
```

Hot Chocolate exposes the input parameter as a GraphQL argument and infers fields from the public properties of the input type.

```graphql
input CreateProductInput {
  name: String!
  description: String
  price: Decimal!
  brandId: ID!
}

type Mutation {
  createProduct(input: CreateProductInput!): CreateProductPayload!
}
```

The request flow is as follows:

```text
client variables -> GraphQL argument -> input object coercion -> C# input instance -> resolver -> application code
```

Key inference rules:

- If the CLR type name does not end with `Input`, Hot Chocolate appends `Input` to the GraphQL type name.
- Public properties are mapped to input fields by convention.
- Methods are ignored, so you can include mapping helpers like `ToRequest()` on the CLR type without exposing them in the schema.
- Service parameters, data loaders, and `CancellationToken` should be placed on the resolver method, not inside the input object.

# Use Records and Immutable Input Types Safely

Records and immutable classes are supported. Hot Chocolate uses constructor binding rather than setting writable properties.

```csharp
public sealed record CreateProductInput(
    string Name,
    string? Description,
    decimal Price);
```

For immutable classes, ensure constructor parameters align with properties:

```csharp
public sealed class CreateProductInput
{
    public CreateProductInput(string name, string? description, decimal price)
    {
        Name = name;
        Description = description;
        Price = price;
    }

    public string Name { get; }
    public string? Description { get; }
    public decimal Price { get; }
}
```

Constructor binding checklist:

- Each constructor parameter name matches a property name, with the parameter starting lowercase.
- Each constructor parameter type matches the property type.
- Every get-only property that must be initialized has a matching constructor parameter.
- Mismatches are validated during schema build.

This input fails because the constructor parameter does not match the property name:

```csharp
public sealed class CreateProductInput
{
    public CreateProductInput(string productName)
    {
        Name = productName;
    }

    public string Name { get; }
}
```

If an immutable input fails during application startup, first check that constructor names and types match the properties.

# Configure an Input Explicitly with `InputObjectType<T>`

Use `InputObjectType<T>` when convention-based inference does not meet your needs. Explicit configuration is helpful for renaming the type, binding only selected fields, ignoring internal members, setting descriptions, specifying exact GraphQL types, configuring IDs, adding defaults, or enabling `@oneOf`.

```csharp
// Types/CreateProductInput.cs
public sealed record CreateProductInput(
    string Name,
    string? Description,
    decimal Price,
    int BrandId,
    string? InternalNote);

// Types/CreateProductInputType.cs
public sealed class CreateProductInputType : InputObjectType<CreateProductInput>
{
    protected override void Configure(
        IInputObjectTypeDescriptor<CreateProductInput> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Description("Values required to create a product.");

        descriptor
            .Field(t => t.Name)
            .Type<NonNullType<StringType>>()
            .Description("The display name shown to customers.");

        descriptor.Field(t => t.Description);
        descriptor.Field(t => t.Price);
        descriptor.Field(t => t.BrandId).ID<Brand>();
    }
}
```

Because the descriptor binds fields explicitly, `InternalNote` is excluded from the schema.

```graphql
"""
Values required to create a product.
"""
input CreateProductInput {
  """
  The display name shown to customers.
  """
  name: String!
  description: String
  price: Decimal!
  brandId: ID!
}
```

| Goal                          | API                                 | Notes                                                 |
| ----------------------------- | ----------------------------------- | ----------------------------------------------------- |
| Bind only configured fields   | `descriptor.BindFieldsExplicitly()` | Use for stable contracts with internal CLR members.   |
| Configure a field             | `descriptor.Field(t => t.Name)`     | Chain type, description, default, ID, or ignore APIs. |
| Set an exact GraphQL type     | `.Type<NonNullType<StringType>>()`  | Use when inference does not express the contract.     |
| Configure an ID field         | `.ID()` or `.ID<T>()`               | Applies ID scalar behavior to an input field.         |
| Set a runtime default         | `.DefaultValue(...)`                | Applies when the client omits the field.              |
| Set a GraphQL literal default | `.DefaultValueSyntax(...)`          | Use for object or list default literals.              |
| Enable one-of                 | `descriptor.OneOf()`                | All fields must be nullable and have no defaults.     |

# Design Mutation Inputs as Use-Case Contracts

Mutation inputs are most effective when each mutation has its own input contract. Use types like `CreateProductInput`, `UpdateProductInput`, and `ArchiveProductInput` rather than sharing a single `ProductInput` across multiple workflows.

You can use a mapping method to keep the schema contract separate from your application command:

```csharp
public sealed record CreateProductRequest(
    string Name,
    string? Description,
    decimal Price,
    int TypeId,
    int BrandId);

public sealed record CreateProductInput(
    string Name,
    string? Description,
    decimal Price,
    [property: ID<ProductType>] int TypeId,
    [property: ID<Brand>] int BrandId)
{
    public CreateProductRequest ToRequest()
        => new(Name, Description, Price, TypeId, BrandId);
}
```

Hot Chocolate ignores methods when inferring input fields, so `ToRequest()` is not included in the schema.

```graphql
input CreateProductInput {
  name: String!
  description: String
  price: Decimal!
  typeId: ID!
  brandId: ID!
}
```

Keep resolver services, data loaders, and cancellation tokens as resolver parameters. They are not client input.

# Model Nested Input Objects and List Fields

Use nested input objects when the nested value is meaningful to the client. For example, a checkout input can group address, payment, and item values without exposing database relationships.

```csharp
public sealed record CreateOrderInput(
    AddressInput Address,
    PaymentMethodInput PaymentMethod,
    IReadOnlyList<OrderItemInput> Items);

public sealed record AddressInput(
    string Street,
    string City,
    string PostalCode,
    string Country);

public sealed record PaymentMethodInput(
    string CardToken);

public sealed record OrderItemInput(
    [property: ID<Product>] int ProductId,
    int Quantity);
```

With nullable reference types enabled, this results in non-null nested values and a non-null list with non-null items:

```graphql
input CreateOrderInput {
  address: AddressInput!
  paymentMethod: PaymentMethodInput!
  items: [OrderItemInput!]!
}

input AddressInput {
  street: String!
  city: String!
  postalCode: String!
  country: String!
}

input PaymentMethodInput {
  cardToken: String!
}

input OrderItemInput {
  productId: ID!
  quantity: Int!
}
```

List nullability has two layers: the list itself and the items within it. See [Lists](./lists) for the full modifier model. For input contracts, make intentional choices:

| C# declaration                          | SDL shape                   | Meaning                                                  |
| --------------------------------------- | --------------------------- | -------------------------------------------------------- |
| `IReadOnlyList<OrderItemInput> Items`   | `items: [OrderItemInput!]!` | The list is required and every item is required.         |
| `IReadOnlyList<OrderItemInput>? Items`  | `items: [OrderItemInput!]`  | The list can be null, but provided items cannot be null. |
| `IReadOnlyList<OrderItemInput?> Items`  | `items: [OrderItemInput]!`  | The list is required, but items can be null.             |
| `IReadOnlyList<OrderItemInput?>? Items` | `items: [OrderItemInput]`   | Both the list and its items can be null.                 |

# Nullability, Required Fields, Defaults, and Omitted Values

Nullable reference types allow Hot Chocolate to infer whether input fields are nullable. Enable nullable reference types in your project and use C# nullability to shape your schema.

| C# declaration                    | SDL shape             | Meaning                                                                                                       |
| --------------------------------- | --------------------- | ------------------------------------------------------------------------------------------------------------- |
| `string Name`                     | `name: String!`       | Client must provide a non-null string.                                                                        |
| `string? Description`             | `description: String` | Client may provide a string, provide `null`, or omit the field if the input object can be coerced without it. |
| `int Quantity`                    | `quantity: Int!`      | Client must provide a non-null integer.                                                                       |
| `int? Quantity`                   | `quantity: Int`       | Client may provide an integer, provide `null`, or omit the field.                                             |
| `[DefaultValue(20)] int PageSize` | `pageSize: Int! = 20` | Client may omit the field and GraphQL will use `20`.                                                          |
| `Optional<string?> Description`   | `description: String` | Resolver can distinguish omitted from explicit `null`.                                                        |

Required fields are non-null fields without defaults. Adding a required field to an existing input object is a breaking schema change. See [Non-Null](./non-null) for the complete nullability model.

# Add Default Values

Default values are part of the public schema contract. They determine what happens when a client omits an input field and can make some additions backward compatible.

Use `[DefaultValue]` for scalar, enum, and other runtime values:

```csharp
public sealed class ProductSearchInput
{
    public string? Term { get; set; }

    [DefaultValue(20)]
    public int PageSize { get; set; }
}
```

```graphql
input ProductSearchInput {
  term: String
  pageSize: Int! = 20
}
```

The equivalent descriptor configuration is:

```csharp
public sealed class ProductSearchInputType : InputObjectType<ProductSearchInput>
{
    protected override void Configure(
        IInputObjectTypeDescriptor<ProductSearchInput> descriptor)
    {
        descriptor.Field(t => t.PageSize).DefaultValue(20);
    }
}
```

For object or list defaults, use GraphQL value literal syntax:

```csharp
public sealed class ProductSearchInput
{
    [DefaultValueSyntax("[\"name\", \"price\"]")]
    public IReadOnlyList<string> IncludeFields { get; set; } = [];
}
```

```graphql
input ProductSearchInput {
  includeFields: [String!]! = ["name", "price"]
}
```

The descriptor equivalent is:

```csharp
descriptor
    .Field(t => t.IncludeFields)
    .DefaultValueSyntax("[\"name\", \"price\"]");
```

Changing a default value can alter runtime behavior, even if client operations still validate.

# Distinguish Omitted Fields from Explicit `null` with `Optional<T>`

Use `Optional<T>` when omitting a field has a different meaning than providing an explicit value. This is common in patch-style updates.

```csharp
public sealed record UpdateProductInput(
    [property: ID<Product>] int Id,
    Optional<string?> Description,
    [property: DefaultValue(0)] Optional<decimal> Price);
```

`Description` can represent three states:

| Client input         | `Optional<T>.HasValue` | Meaning                                    |
| -------------------- | ---------------------- | ------------------------------------------ |
| Field omitted        | `false`                | Leave the current value unchanged.         |
| Field set to `null`  | `true`                 | Clear the value, if the field allows null. |
| Field set to a value | `true`                 | Replace the current value.                 |

In resolver code, check `HasValue` before reading the value:

```csharp
public static async Task<ProductPayload> UpdateProductAsync(
    UpdateProductInput input,
    ProductService products,
    CancellationToken cancellationToken)
{
    var patch = new ProductPatch(input.Id);

    if (input.Description.HasValue)
    {
        patch.Description = input.Description.Value;
    }

    if (input.Price.HasValue)
    {
        patch.Price = input.Price.Value;
    }

    var product = await products.UpdateAsync(patch, cancellationToken);
    return new ProductPayload(product);
}
```

When using `Optional<T>` with a non-nullable field, add a default value or configure the field explicitly if clients should be able to omit it. Use nullable inner types, defaults, or descriptor configuration to match your intended schema shape.

# Use `@oneOf` for Mutually Exclusive Input Choices

Apply `@oneOf` when a client must provide exactly one of several nullable fields. A selector is a common use case.

```graphql
input ProductSelectorInput @oneOf {
  id: ID
  sku: String
}
```

With attributes:

```csharp
[OneOf]
public sealed class ProductSelectorInput
{
    [ID<Product>]
    public int? Id { get; set; }

    public string? Sku { get; set; }
}
```

With descriptor configuration:

```csharp
public sealed class ProductSelectorInputType
    : InputObjectType<ProductSelectorInput>
{
    protected override void Configure(
        IInputObjectTypeDescriptor<ProductSelectorInput> descriptor)
    {
        descriptor.OneOf();
        descriptor.Field(t => t.Id).ID<Product>();
        descriptor.Field(t => t.Sku);
    }
}
```

Rules for `@oneOf` input objects in Hot Chocolate v16:

- Exactly one field must be provided and non-null.
- All fields in a one-of input must be nullable.
- One-of fields cannot have default values.
- Invalid one-of definitions are rejected during schema validation.
- Invalid one-of values are rejected during request validation or execution.

# Validate Input at the Right Boundary

GraphQL and Hot Chocolate validate the structure of incoming input:

- Unknown input fields
- Invalid literals and variable values
- Required field presence
- Nullability rules
- Default value application
- Immutable input constructor binding during schema build
- `@oneOf` schema and value rules

Your application code should handle domain and workflow validation:

- Business invariants, such as positive price or sufficient stock
- Authorization and permissions
- Cross-field rules, such as `min <= max`
- Domain errors and user-facing mutation payload errors

# Evolve Input Objects Safely

Changing input objects affects your schema contract. Favor additive, optional changes, and introduce new input types when a workflow requires a different contract.

| Change                                           | Usually safe?                              | Notes                                   |
| ------------------------------------------------ | ------------------------------------------ | --------------------------------------- |
| Add nullable field                               | Yes                                        | Existing clients can omit it.           |
| Add field with default                           | Yes, if the default preserves old behavior | The default becomes contract behavior.  |
| Add required field without default               | No                                         | Existing operations can fail.           |
| Rename or remove a field                         | No                                         | Client operations break.                |
| Change nullable to non-null                      | No                                         | Existing clients may send or omit null. |
| Reuse one input across workflows, then change it | Risky                                      | Prefer one input per use case.          |

# Generated Filter and Sort Inputs Are Separate

Filtering and sorting are also represented as input object types in a GraphQL schema, but they use specialized data APIs rather than general `InputObjectType<T>` configuration.

- Filtering uses `HotChocolate.Data.Filters.FilterInputType<T>`.
- Sorting uses `HotChocolate.Data.Sorting.SortInputType<T>`.

When customizing generated data inputs, only expose fields that are safe and performant to query. See [Filtering](../resolvers-and-data/filtering) and [Sorting](../resolvers-and-data/sorting) for more details.

# Troubleshooting Input Object Types

| Symptom                                                        | Likely cause                                                                                                   | Fix                                                                                    |
| -------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------- |
| A resolver parameter became an input object unexpectedly       | It is a class or record parameter and Hot Chocolate did not treat it as a scalar, enum, or service             | Review argument and service binding. See [Arguments](./arguments).                     |
| A record or immutable input fails during schema startup        | Constructor parameter names or types do not match properties                                                   | Match names and types, or use a mutable input shape.                                   |
| Client omitted a field but the resolver sees `null`            | A nullable property cannot distinguish omission from explicit null                                             | Use `Optional<T>` when omission matters.                                               |
| Adding an input field broke clients                            | The new field was non-null and had no default                                                                  | Add nullable or defaulted fields, or introduce a new input type.                       |
| A `@oneOf` input is rejected                                   | No field, multiple fields, a null selected field, a non-null field definition, or a defaulted field definition | Make all one-of fields nullable, remove defaults, and send exactly one non-null value. |
| An output object, interface, or union was used inside an input | Input fields must use input-compatible types                                                                   | Create a dedicated input object, or use `@oneOf` for input choices.                    |

# Next Steps

- See [Arguments](./arguments) for scalar arguments, complex arguments, and resolver parameter binding.
- See [Mutations](./operations-mutations) for mutation conventions, payloads, and errors.
- See [Non-Null](./non-null) and [Lists](./lists) to control required fields and list modifiers.
- See [Documentation](./documentation) to add descriptions to input object types and input fields.
- See [Filtering](../resolvers-and-data/filtering) and [Sorting](../resolvers-and-data/sorting) for generated data input types.
