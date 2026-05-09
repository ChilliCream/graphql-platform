---
title: GraphQLName attribute
---

C# names describe implementation. GraphQL names are the public schema contract that clients use in queries, variables, fragments, persisted operations, generated clients, caches, and schema checks.

Use `[GraphQLName("...")]` when a C# symbol should keep its C# name but expose a different GraphQL name.

```csharp
#nullable enable

using HotChocolate;

[GraphQLName("Message")]
public interface IMessage
{
    string Text { get; }
}

public sealed class ChatMessage : IMessage
{
    public string Text { get; set; } = default!;
}
```

Expected SDL:

```graphql
interface Message {
  text: String!
}

type ChatMessage implements Message {
  text: String!
}
```

Changing a released schema name is a breaking change unless you migrate clients deliberately. Plan the migration before you rename a type, field, argument, input field, enum value, or enum type.

# Start with Hot Chocolate's default names

Prefer the default naming conventions unless the public API needs different client vocabulary.

| C# symbol                         | Default GraphQL name | Notes                                                                                              |
| --------------------------------- | -------------------- | -------------------------------------------------------------------------------------------------- |
| `Book` class                      | `Book`               | Type names use the CLR type name.                                                                  |
| `Name` property                   | `name`               | Object fields and input fields are camelCased.                                                     |
| `GetBooksAsync()` resolver method | `books`              | `Get` is stripped, `Async` is stripped, then the result is camelCased.                             |
| `string postalCode` parameter     | `postalCode`         | Resolver arguments are camelCased.                                                                 |
| `CreateBook` input CLR type       | `CreateBookInput`    | Input object names get an `Input` suffix when the inferred name does not already end with `Input`. |
| `ReadOnly` enum member            | `READ_ONLY`          | Enum values use `UPPER_SNAKE_CASE`.                                                                |
| `IMessage` interface              | `IMessage`           | `StripLeadingIFromInterface` can change this as a schema option.                                   |

Use an override when the C# name is correct for your code but wrong for clients, for example `StockKeepingUnit` in C# and `sku` in GraphQL.

# What GraphQLName changes

`[GraphQLName]` changes the public GraphQL schema name. It does not rename the C# symbol, database column, JSON property, or CLR member.

Syntax:

```csharp
using HotChocolate;

[GraphQLName("schemaName")]
```

Common targets:

| C# target        | GraphQL element                                   | Default example    | Attribute example             | Descriptor alternative                                                    |
| ---------------- | ------------------------------------------------- | ------------------ | ----------------------------- | ------------------------------------------------------------------------- |
| Class or struct  | Object type, input type, or other discovered type | `Author`           | `[GraphQLName("BookAuthor")]` | `descriptor.Name("BookAuthor")`                                           |
| Interface        | Interface type                                    | `IMessage`         | `[GraphQLName("Message")]`    | `descriptor.Name("Message")`                                              |
| Property         | Object field, interface field, or input field     | `name`             | `[GraphQLName("fullName")]`   | `.Field(t => t.Name).Name("fullName")`                                    |
| Method           | Object, query, or mutation field                  | `books`            | `[GraphQLName("allBooks")]`   | `.Field("allBooks")` or `.Field(t => t.GetBooksAsync()).Name("allBooks")` |
| Method parameter | Argument                                          | `stockKeepingUnit` | `[GraphQLName("sku")]`        | `.Argument("sku")` or `[Argument("sku")]`                                 |
| Enum             | Enum type                                         | `UserRole`         | `[GraphQLName("Role")]`       | `descriptor.Name("Role")`                                                 |
| Enum member      | Enum value                                        | `GUEST`            | `[GraphQLName("VISITOR")]`    | `descriptor.Value(UserRole.Guest).Name("VISITOR")`                        |

The CLR attribute can be applied to classes, structs, interfaces, properties, methods, parameters, enums, and enum fields. Resolver class binding can also read the attribute in advanced registration scenarios, but most schemas should use the targets shown above.

# Rename a type or interface

Put `[GraphQLName]` on the CLR type when the schema name belongs with that type everywhere Hot Chocolate discovers it.

```csharp
using HotChocolate;

[GraphQLName("BookAuthor")]
public sealed class Author
{
    public string Name { get; set; } = default!;
}
```

Expected SDL:

```graphql
type BookAuthor {
  name: String!
}
```

Use a descriptor when you want schema configuration outside the domain model, when the type comes from another assembly, or when one CLR type needs different projections in different schema areas.

```csharp
using HotChocolate.Types;

public sealed class AuthorType : ObjectType<Author>
{
    protected override void Configure(IObjectTypeDescriptor<Author> descriptor)
    {
        descriptor.Name("BookAuthor");
    }
}
```

Input object types keep the input naming rule after name inference. If you put `[GraphQLName("CreateBook")]` on an input CLR type, Hot Chocolate exposes `CreateBookInput`. If you want the final schema name to be `CreateBookInput`, choose that full name in the attribute and verify the generated SDL.

```csharp
using HotChocolate;

[GraphQLName("CreateBookInput")]
public sealed record CreateBookRequest(string Title);
```

Expected SDL when the record is used as an input object:

```graphql
input CreateBookInput {
  title: String!
}
```

# Rename fields and resolver methods

Put `[GraphQLName]` on a property when the GraphQL field should use client vocabulary.

```csharp
using HotChocolate;

public sealed class Author
{
    [GraphQLName("fullName")]
    public string Name { get; set; } = default!;
}
```

Expected SDL:

```graphql
type Author {
  fullName: String!
}
```

Descriptor alternative:

```csharp
using HotChocolate.Types;

public sealed class AuthorType : ObjectType<Author>
{
    protected override void Configure(IObjectTypeDescriptor<Author> descriptor)
    {
        descriptor
            .Field(t => t.Name)
            .Name("fullName");
    }
}
```

Resolver methods follow query and mutation naming conventions. `GetBooksAsync` defaults to `books`. Add `[GraphQLName]` when the public field needs a different name.

```csharp
using HotChocolate;

[QueryType]
public static partial class BookQueries
{
    [GraphQLName("allBooks")]
    public static IReadOnlyList<Book> GetBooksAsync(BookRepository books)
    {
        return books.GetAll();
    }
}
```

Expected SDL:

```graphql
type Query {
  allBooks: [Book!]!
}
```

Client query:

```graphql
query GetBooks {
  allBooks {
    title
  }
}
```

If one operation needs a different response key, use a GraphQL alias instead of changing the schema.

```graphql
query GetBooks {
  catalog: allBooks {
    title
  }
}
```

# Rename arguments and input fields

Put `[GraphQLName]` on a resolver parameter when the argument name should differ from the C# parameter name.

```csharp
using HotChocolate;

[QueryType]
public static partial class ProductQueries
{
    public static Product? GetProduct(
        [GraphQLName("sku")] string stockKeepingUnit,
        ProductRepository products)
    {
        return products.FindBySku(stockKeepingUnit);
    }
}
```

Expected SDL:

```graphql
type Query {
  product(sku: String!): Product
}
```

Clients must use the GraphQL argument name in documents and variables.

```graphql
query GetProduct($sku: String!) {
  product(sku: $sku) {
    name
  }
}
```

Use one naming attribute per parameter. If you need to state that a parameter is a GraphQL argument and name it at the same time, `[Argument("sku")]` is clearer than combining `[Argument]` and `[GraphQLName]`.

Input fields can be renamed the same way on the input CLR property.

```csharp
using HotChocolate;

public sealed record CreateAddressInput(
    string Street,
    [property: GraphQLName("zipCode")] string PostalCode);
```

Expected SDL:

```graphql
input CreateAddressInput {
  street: String!
  zipCode: String!
}
```

Descriptor alternative:

```csharp
using HotChocolate.Types;

public sealed class CreateAddressInputType : InputObjectType<CreateAddressInput>
{
    protected override void Configure(
        IInputObjectTypeDescriptor<CreateAddressInput> descriptor)
    {
        descriptor
            .Field(t => t.PostalCode)
            .Name("zipCode");
    }
}
```

Argument and input field renames break existing operations and variables. Required arguments and required input fields without defaults need a coordinated client migration.

# Rename enum types and enum values

Hot Chocolate converts enum members to `UPPER_SNAKE_CASE` by default. Use `[GraphQLName]` for public vocabulary exceptions.

```csharp
using HotChocolate;

[GraphQLName("Role")]
public enum UserRole
{
    [GraphQLName("VISITOR")]
    Guest,
    Standard,
    Administrator
}
```

Expected SDL:

```graphql
enum Role {
  VISITOR
  STANDARD
  ADMINISTRATOR
}
```

Descriptor alternative:

```csharp
using HotChocolate.Types;

public sealed class UserRoleType : EnumType<UserRole>
{
    protected override void Configure(IEnumTypeDescriptor<UserRole> descriptor)
    {
        descriptor.Name("Role");
        descriptor.Value(UserRole.Guest).Name("VISITOR");
    }
}
```

Enum value renames break queries, variables, persisted operations, generated clients, and switch-like client code. Prefer stable enum vocabulary before release.

# Choose the right naming tool

| Tool                        | Use it when                                                                                          | Avoid it when                                                        |
| --------------------------- | ---------------------------------------------------------------------------------------------------- | -------------------------------------------------------------------- |
| `[GraphQLName]`             | A local C# declaration needs one targeted public-name exception.                                     | The naming rule is broad, conditional, or belongs outside the model. |
| Descriptor `.Name(...)`     | You centralize schema configuration, configure third-party types, or project a CLR type differently. | The attribute would be more visible and less code.                   |
| Specialized attributes      | A more specific attribute expresses intent, for example `[InterfaceType("Post")]`.                   | The attribute would hide a broader schema policy.                    |
| Schema options              | You need a built-in policy such as `StripLeadingIFromInterface`.                                     | One or two symbols need different names.                             |
| Custom `INamingConventions` | Your organization needs a global naming policy.                                                      | A local override solves the problem.                                 |
| GraphQL aliases             | A client wants a different response key for one operation.                                           | The schema contract itself needs a new name.                         |

Avoid inconsistent one-off names when a clear convention would serve clients better.

# Preserve schema stability when renaming

Treat GraphQL names as API contract. Renaming can break query documents, fragments, variables, persisted operations, trusted documents, generated clients, normalized cache policies, dashboards, and schema checks.

For output fields, use an additive migration:

```csharp
using HotChocolate;

public sealed class Author
{
    [GraphQLDeprecated("Use fullName.")]
    public string Name { get; set; } = default!;

    [GraphQLName("fullName")]
    public string NameForSchema => Name;
}
```

Expected SDL:

```graphql
type Author {
  name: String! @deprecated(reason: "Use fullName.")
  fullName: String!
}
```

Migration checklist:

1. Add the new field with the new name.
2. Keep the old field.
3. Deprecate the old field.
4. Monitor usage.
5. Remove the old field in a later version.

| Element      | Safer migration path                                                                                                                       |
| ------------ | ------------------------------------------------------------------------------------------------------------------------------------------ |
| Output field | Add the new field, deprecate the old field, remove later.                                                                                  |
| Argument     | Prefer an additive field or operation. Required arguments without defaults cannot be deprecated safely.                                    |
| Input field  | Prefer an additive input field with a default or a parallel operation. Required input fields without defaults cannot be deprecated safely. |
| Enum value   | Add a replacement value when possible, deprecate the old value, coordinate generated client updates.                                       |
| Type name    | Coordinate with clients because fragments, generated models, cache normalization, and introspection tooling may reference it.              |

# Troubleshoot naming problems

## The schema still uses the C# name

Check these causes:

- The attribute is on the wrong symbol. Put it on the property or resolver method for fields, on the parameter for arguments, on the input property for input fields, and on the enum member for enum values.
- The type is not registered or discovered by your schema builder path.
- Descriptor configuration names the same schema element elsewhere. Explicit descriptor configuration can override an inferred object type name and may affect other elements depending on the configuration path.
- The element is an input object type, and Hot Chocolate appended `Input` after name inference.

## Schema creation fails after adding a name

Use valid GraphQL names. Start with an ASCII letter or underscore, then use ASCII letters, digits, or underscores. Avoid names that begin with `__`, because GraphQL reserves double-underscore names for introspection.

Also check for duplicate type, field, argument, input field, or enum value names. Some inference paths normalize invalid characters, while explicit type-system configuration can reject invalid names. Use valid names rather than depending on normalization.

## Clients broke after a rename

Clients still reference the old schema name. If the rename was accidental, restore the old name. For output fields, add the new field alongside the old field and deprecate the old field. For arguments, input fields, enum values, and types, plan a coordinated migration or a parallel operation when additive compatibility is not practical.

## An enum value is not named as expected

Remember the default `UPPER_SNAKE_CASE` conversion, for example `ReadOnly` becomes `READ_ONLY`. Put `[GraphQLName]` on the enum member for vocabulary exceptions. Check for invalid characters and collisions after normalization.

## An input type has an unexpected Input suffix

Input object names get `Input` appended if the inferred name does not already end with `Input`. If an explicit attribute name should be final, choose a name that already includes the desired `Input` suffix and verify the generated SDL.

# API reference summary

| Item                     | Value                                                                                                               |
| ------------------------ | ------------------------------------------------------------------------------------------------------------------- |
| Syntax                   | `[GraphQLName("schemaName")]`                                                                                       |
| Namespace                | `HotChocolate`                                                                                                      |
| CLR attribute targets    | Class, struct, interface, property, method, parameter, enum, field                                                  |
| Common schema elements   | Object, interface, input, and enum type names; object, interface, and input fields; resolver arguments; enum values |
| Constructor validation   | Rejects `null` and empty strings                                                                                    |
| Recommended name pattern | `/^[_A-Za-z][_0-9A-Za-z]*$/`                                                                                        |
| Reserved prefix to avoid | `__`                                                                                                                |

Related APIs include descriptor `.Name(...)`, `.Field(...).Name(...)`, `.Argument(...).Name(...)`, `.Value(...).Name(...)`, `[GraphQLDescription]`, `[GraphQLDeprecated]`, `[GraphQLIgnore]`, `[GraphQLType]`, `[ID]`, `[InterfaceType("...")]`, `StripLeadingIFromInterface`, and `INamingConventions`.

# Next steps

- [Attributes overview](./) for how attributes participate in schema building.
- [Object Types](../schema-elements/object-types) for object type and property naming.
- [Queries](../schema-elements/operations-queries) for resolver method naming.
- [Arguments](../schema-elements/arguments) for resolver parameter naming and argument binding.
- [Input Object Types](../schema-elements/input-object-types) for input object contracts.
- [Enums](../schema-elements/enums) for enum naming and descriptor alternatives.
- [Interfaces](../schema-elements/interfaces) for interface names and interface fields.
