---
title: "Unions"
---

A GraphQL union represents a set of object types that share no required common fields. Unlike [interfaces](/docs/hotchocolate/v16/defining-a-schema/interfaces), union members do not need to declare the same fields. Clients use inline fragments to select fields from each possible type.

**GraphQL schema**

```graphql
type TextContent {
  text: String!
}

type ImageContent {
  imageUrl: String!
  height: Int!
}

union PostContent = TextContent | ImageContent
```

**Client query**

```graphql
{
  content {
    ... on TextContent {
      text
    }
    ... on ImageContent {
      imageUrl
    }
  }
}
```

# Defining a Union Type

Use a marker interface (an interface with no members) or an abstract class to group the types that belong to the union.

<ExampleTabs>
<Implementation>

```csharp
// Types/IPostContent.cs
[UnionType("PostContent")]
public interface IPostContent
{
}

// Types/TextContent.cs
public class TextContent : IPostContent
{
    public string Text { get; set; }
}

// Types/ImageContent.cs
public class ImageContent : IPostContent
{
    public string ImageUrl { get; set; }
    public int Height { get; set; }
}

// Types/ContentQueries.cs
[QueryType]
public static partial class ContentQueries
{
    public static IPostContent GetContent()
    {
        // ...
    }
}
```

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddType<TextContent>()
    .AddType<ImageContent>();
```

Each type that implements the marker interface must be registered so Hot Chocolate includes it in the union.

</Implementation>
<Code>

```csharp
// Types/PostContentType.cs
public class PostContentType : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Name("PostContent");
        descriptor.Type<TextContentType>();
        descriptor.Type<ImageContentType>();
    }
}
```

The member types are registered through the union definition, so you do not need to register them separately.

You can also use a marker interface with `UnionType<T>`:

```csharp
// Types/PostContentType.cs
public class PostContentType : UnionType<IPostContent>
{
}
```

</Code>
</ExampleTabs>

# Union vs Interface

Choose a union when the grouped types have no meaningful shared fields. Choose an interface when you want to guarantee a common set of fields across all implementing types.

| Feature                      | Union | Interface |
| ---------------------------- | ----- | --------- |
| Common fields required       | No    | Yes       |
| Query shared fields directly | No    | Yes       |
| Types can belong to multiple | Yes   | Yes       |

# Troubleshooting

## "No types registered for union"

A union must contain at least one member type. Verify that you have registered each implementing class with `.AddType<T>()` or included it in the `UnionType` configuration.

## Unexpected `null` for union field

If the resolver returns a type that is not part of the union, Hot Chocolate cannot resolve it and returns `null`. Verify the runtime type implements the marker interface or is listed in the union configuration.

## Cannot query `__typename` on union

The `__typename` meta-field is available on all union members. Clients can include `__typename` inside any inline fragment to determine which type was returned.

# Next Steps

- **Need shared fields across types?** See [Interfaces](/docs/hotchocolate/v16/defining-a-schema/interfaces).
- **Need to define output types?** See [Object Types](/docs/hotchocolate/v16/defining-a-schema/object-types).
- **Need input polymorphism?** See [OneOf Input Objects](/docs/hotchocolate/v16/defining-a-schema/input-object-types#oneof-input-objects).
