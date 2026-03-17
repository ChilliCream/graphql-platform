---
title: "Input Object Types"
---

GraphQL input object types let you pass structured data as arguments. While [scalar arguments](/docs/hotchocolate/v16/defining-a-schema/arguments) work for simple values, input types let you group related fields into a single object. Input types differ from output object types: their fields cannot have arguments and they use the `input` keyword in the schema.

**GraphQL schema**

```graphql
input CreateBookInput {
  title: String!
  author: String!
}

type Mutation {
  createBook(input: CreateBookInput!): Book
}
```

# Defining an Input Type

Any C# class or record used as a resolver parameter (that is not a scalar, enum, or service) becomes an input type in the schema.

<ExampleTabs>
<Implementation>

```csharp
// Types/CreateBookInput.cs
public class CreateBookInput
{
    public string Title { get; set; }
    public string Author { get; set; }
}

// Types/BookMutations.cs
[MutationType]
public static partial class BookMutations
{
    public static async Task<Book> CreateBookAsync(
        CreateBookInput input,
        CatalogContext db,
        CancellationToken ct)
    {
        var book = new Book { Title = input.Title, Author = input.Author };
        db.Books.Add(book);
        await db.SaveChangesAsync(ct);
        return book;
    }
}
```

If a class used as an argument does not end in `Input`, Hot Chocolate appends `Input` to the type name in the schema automatically.

</Implementation>
<Code>

```csharp
// Types/CreateBookInput.cs
public class CreateBookInput
{
    public string Title { get; set; }
    public string Author { get; set; }
}

// Types/CreateBookInputType.cs
public class CreateBookInputType : InputObjectType<CreateBookInput>
{
    protected override void Configure(
        IInputObjectTypeDescriptor<CreateBookInput> descriptor)
    {
        descriptor.Field(f => f.Title).Type<NonNullType<StringType>>();
        descriptor.Field(f => f.Author).Type<NonNullType<StringType>>();
    }
}
```

</Code>
</ExampleTabs>

# Using Records and Immutable Types

Input types can use immutable classes or C# records. Hot Chocolate calls the constructor instead of setting properties. The rules are:

1. Each constructor parameter type must match the corresponding property type.
2. Each constructor parameter name must match the property name (with a lowercase first letter).
3. All properties must have a matching constructor parameter.

Hot Chocolate validates input constructors at schema build time, so mismatches are caught early.

```csharp
// Types/CreateBookInput.cs
public record CreateBookInput(string Title, string Author);
```

This record is equivalent to a class with a constructor and get-only properties.

# Default Values

The `[DefaultValue]` attribute assigns a default value to an input field. When the client omits the field, the default is used.

<ExampleTabs>
<Implementation>

```csharp
// Types/UserFilterInput.cs
public class UserFilterInput
{
    public string? Name { get; set; }

    [DefaultValue(true)]
    public bool IsActive { get; set; }
}
```

This produces:

```graphql
input UserFilterInput {
  name: String
  isActive: Boolean! = true
}
```

</Implementation>
<Code>

```csharp
// Types/UserFilterInputType.cs
public class UserFilterInputType : InputObjectType<UserFilterInput>
{
    protected override void Configure(
        IInputObjectTypeDescriptor<UserFilterInput> descriptor)
    {
        descriptor.Field(f => f.IsActive).DefaultValue(true);
    }
}
```

</Code>
</ExampleTabs>

Default values maintain backward compatibility. When you add a new field to an input type, providing a default value keeps existing queries working.

## Default Values with GraphQL Syntax

For complex defaults (objects or lists), use `[DefaultValueSyntax]` with GraphQL value literal syntax.

```csharp
// Types/UserProfileInput.cs
public class UserProfileInput
{
    public string? Name { get; set; }

    [DefaultValueSyntax("{ notifications: true, theme: \"light\" }")]
    public Preferences? Preferences { get; set; }
}
```

In code-first, use the `DefaultValueSyntax` method:

```csharp
descriptor
    .Field(f => f.Preferences)
    .DefaultValueSyntax("{ notifications: true, theme: \"light\" }");
```

# Optional Properties

Use `Optional<T>` to distinguish between a field that was not provided and a field explicitly set to `null`. This is important for partial updates where you need to know whether the client intended to clear a value.

```csharp
// Types/UpdateBookInput.cs
public class UpdateBookInput
{
    [DefaultValue("")]
    public Optional<string> Title { get; set; }

    public string Author { get; set; }
}
```

When using `Optional<T>` on a non-nullable field, you must add `[DefaultValue]` to make the field optional in the schema.

Records work too:

```csharp
public record UpdateBookInput(
    [property: DefaultValue("")] Optional<string> Title,
    string Author);
```

# OneOf Input Objects

A `@oneOf` input type requires that exactly one field is set and non-null. This provides input polymorphism, letting a single argument accept different shapes of data.

**GraphQL schema**

```graphql
input PetInput @oneOf {
  cat: CatInput
  dog: DogInput
}
```

<ExampleTabs>
<Implementation>

```csharp
// Types/PetInput.cs
[OneOf]
public class PetInput
{
    public CatInput? Cat { get; set; }
    public DogInput? Dog { get; set; }
}

// Types/CatInput.cs
public class CatInput
{
    public string Name { get; set; }
    public int NumberOfLives { get; set; }
}

// Types/DogInput.cs
public class DogInput
{
    public string Name { get; set; }
    public bool WagsTail { get; set; }
}

// Types/PetMutations.cs
[MutationType]
public static partial class PetMutations
{
    public static Pet CreatePet(PetInput input)
    {
        // ...
    }
}
```

</Implementation>
<Code>

```csharp
// Types/PetInputType.cs
public class PetInputType : InputObjectType<PetInput>
{
    protected override void Configure(
        IInputObjectTypeDescriptor<PetInput> descriptor)
    {
        descriptor.OneOf();
    }
}
```

</Code>
</ExampleTabs>

All fields on a `@oneOf` input must be nullable. Hot Chocolate validates at runtime that exactly one field is provided.

# Troubleshooting

## Input type not appearing in schema

Verify the class is used as a parameter in a resolver. If the class is not referenced by any resolver, Hot Chocolate does not include it in the schema. In code-first, register it explicitly with `.AddType<T>()`.

## Constructor validation error at startup

If you use an immutable type and the constructor parameters do not match the properties, Hot Chocolate throws at schema build time. Verify that each parameter name matches a property name (case-insensitive first letter) and that types align.

## `Optional<T>` field still required

When using `Optional<T>` on a non-nullable type, add `[DefaultValue]` to make it optional in the schema. Without a default value, the field remains required.

# Next Steps

- **Need scalar arguments?** See [Arguments](/docs/hotchocolate/v16/defining-a-schema/arguments).
- **Need to write mutations?** See [Mutations](/docs/hotchocolate/v16/defining-a-schema/mutations).
- **Need to understand nullability?** See [Non-Null](/docs/hotchocolate/v16/defining-a-schema/non-null).
- **Need to document input fields?** See [Documentation](/docs/hotchocolate/v16/defining-a-schema/documentation).
