---
title: "Unions"
---

A union type represents a set of object types. It is very similar to an [interface](/docs/hotchocolate/v15/defining-a-schema/interfaces), except that there is no requirement for common fields between the specified types.

```sdl
type TextContent {
  text: String!
}

type ImageContent {
  imageUrl: String!
  height: Int!
}

union PostContent = TextContent | ImageContent
```

Clients can query fields returning a union like the following.

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

Learn more about unions [here](https://graphql.org/learn/schema/#union-types).

# Usage

Unions can be defined like the following.

<ExampleTabs>
<Implementation>

We can use a marker interface (or an abstract class) to define object types as part of a union.

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

public class Query
{
    public IPostContent GetContent()
    {
        // Omitted code for brevity
    }
}
```

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddType<TextContent>()
    .AddType<ImageContent>();
```

</Implementation>
<Code>

```csharp
public class TextContent
{
    public string Text { get; set; }
}

public class TextContentType : ObjectType<TextContent>
{
}

public class ImageContent
{
    public string ImageUrl { get; set; }

    public int Height { get; set; }
}

public class ImageContentType : ObjectType<ImageContent>
{
}

public class PostContentType : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        descriptor.Name("PostContent");

        // The object types that belong to this union
        descriptor.Type<TextContentType>();
        descriptor.Type<ImageContentType>();
    }
}

public class Query
{
    public object GetContent()
    {
        // Omitted code for brevity
    }
}

public class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field(f => f.GetContent(default))
            .Type<PostContentType>();
    }
}
```

Since the types are already registered within the union, we do not have to register them again in our `Program` class.

We can use a marker interface, as in the implementation-first approach, to type our union definition: `UnionType<IMarkerInterface>`

</Code>
<Schema>

```csharp
public class TextContent
{
    public string Text { get; set; }
}

public class ImageContent
{
    public string ImageUrl { get; set; }

    public int Height { get; set; }
}
```

```csharp
builder.Services
    .AddGraphQLServer()
    .AddDocumentFromString(@"
        type Query {
          content: PostContent
        }

        type TextContent {
          text: String!
        }

        type ImageContent {
          imageUrl: String!
          height: Int!
        }

        union PostContent = TextContent | ImageContent
    ")
    .BindRuntimeType<TextContent>()
    .BindRuntimeType<ImageContent>()
    .AddResolver("Query", "content", (context) =>
    {
        // Omitted code for brevity
    });
```

</Schema>
</ExampleTabs>
