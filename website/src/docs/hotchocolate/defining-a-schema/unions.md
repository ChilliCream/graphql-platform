---
title: "Unions"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

A Union is very similar to an [interface](/docs/hotchocolate/defining-a-schema/interfaces), except that there are no common fields between the specified types.

Unions are defined in the schema as follows.

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

Learn more about unions [here](https://graphql.org/learn/schema/#union-types).

# Definition

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
    public IPostContent GetContent([Service] IContentRepository repository)
        => repository.GetContent();
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
    public object GetContent([Service] IContentRepository repository)
        => repository.GetContent();
}


public class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor.Name("Query");

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
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddRouting()
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
        .AddResolver(
            "Query",
            "content",
            (context, ct) => context.Service<IContentRepository>().GetContent());
}
```

</ExampleTabs.Schema>
</ExampleTabs>

# Unions as arguments

TODO
