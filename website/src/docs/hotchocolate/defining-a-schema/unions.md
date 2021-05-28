---
title: "Unions"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

A union type represents a set of object types. It is very similar to an [interface](/docs/hotchocolate/defining-a-schema/interfaces), except that there is no requirement for common fields between the specified types.

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
  postContent {
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
<ExampleTabs.Annotation>

We can use a marker interface to define object types as part of a union.

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

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddType<TextContent>()
            .AddType<ImageContent>();
    }
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

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

Since the types are already registered within the union, we do not have to register them again in our `Startup` class.

We can use a marker interface, as in the annotation-based approach, to type our union definition: `UnionType<IMarkerInterface>`

</ExampleTabs.Code>
<ExampleTabs.Schema>

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

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
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
            .BindComplexType<TextContent>()
            .BindComplexType<ImageContent>()
            .AddResolver("Query", "content", (context) =>
            {
                // Omitted code for brevity
            });
    }
}
```

</ExampleTabs.Schema>
</ExampleTabs>
