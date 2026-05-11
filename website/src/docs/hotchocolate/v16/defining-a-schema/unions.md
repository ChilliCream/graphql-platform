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
[UnionType("PostContent")]
public interface IPostContent
{
}

public class TextContent : IPostContent
{
    public string Text { get; set; }
}

public class ImageContent : IPostContent
{
    public string ImageUrl { get; set; }
    public int Height { get; set; }
}

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
builder
    .AddGraphQL()
    .AddType<TextContent>()
    .AddType<ImageContent>();
```

Each type that implements the marker interface must be registered so Hot Chocolate includes it in the union.

</Implementation>
<Code>

```csharp
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
public class PostContentType : UnionType<IPostContent>
{
}
```

</Code>
</ExampleTabs>

# Union vs Interface

Both unions and interfaces are abstract types that let a field return one of several object types. They differ in how much structure they enforce and how clients query them.

**Use a union** when the member types are genuinely different entities with no meaningful shared fields, for example a search that can return a `User`, a `Post`, or a `Comment`. Clients must use inline fragments for every field because the union guarantees no common structure.

```graphql
union SearchResult = User | Post | Comment

# Client must fragment into each type
query {
  search(term: "graphql") {
    ... on User {
      name
    }
    ... on Post {
      title
    }
    ... on Comment {
      body
    }
  }
}
```

**Use an interface** when the types share common fields that clients regularly query together. The interface enforces a contract: every implementing type must include the interface fields. Clients can query those fields directly without fragments.

```graphql
interface Event {
  id: ID!
  timestamp: DateTime!
}

# Shared fields are queryable directly
query {
  events {
    id
    timestamp
    ... on UserEvent {
      user {
        name
      }
    }
    ... on SystemEvent {
      severity
    }
  }
}
```

**Use both together** when you need the flexibility of a union with some guaranteed fields across members. The errors-as-data pattern is a common example: a union separates success from failure, while an interface guarantees a `message` field on all error types.

```graphql
interface CheckoutError {
  message: String!
}

type InsufficientStockError implements CheckoutError {
  message: String!
  availableStock: Int!
}

type InvalidPaymentError implements CheckoutError {
  message: String!
}

union CheckoutResult = Order | InsufficientStockError | InvalidPaymentError
```

# Next Steps

- **Need shared fields across types?** See [Interfaces](/docs/hotchocolate/v16/defining-a-schema/interfaces).
- **Need to define output types?** See [Object Types](/docs/hotchocolate/v16/defining-a-schema/object-types).
- **Need input polymorphism?** See [OneOf Input Objects](/docs/hotchocolate/v16/defining-a-schema/input-object-types#oneof-input-objects).
