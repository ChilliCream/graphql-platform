---
title: "Interfaces"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

> We are still working on the documentation for Hot Chocolate 11.1 so help us by finding typos, missing things or write some additional docs with us.

An Interface is an abstract type that includes a certain set of fields that a type must include to implement the interface.

Interfaces are defined in the schema as follows.

```sdl
interface Message {
  author: User!
  createdAt: DateTime!
}

type TextMessage implements Message {
  author: User!
  createdAt: DateTime!
  content: String!
}
```

Learn more about interfaces [here](https://graphql.org/learn/schema/#interfaces).

# Definition

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
[GraphQLName("Message")]
public interface IMessage
{
    User Author { get; set; }

    DateTime CreatedAt { get; set; }
}

public class TextMessage : IMessage
{
    public User Author { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Content { get; set; }
}

public class Query
{
  public IMessage[] GetMessages([Service] IMessageRepository repository)
      => repository.GetMessages();
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddType<TextMessage>();
    }
}
```

<!-- TODO: This is currently broken: https://github.com/ChilliCream/hotchocolate/issues/3577 -->

We can also use classes to define an interface.

```csharp
[InterfaceType]
public abstract class Message
{
    public User SendBy { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class TextMessage : Message
{
    public string Content { get; set; }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            // ...
            .AddType<TextMessage>();
    }
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public interface IMessage
{
    User Author { get; set; }

    DateTime CreatedAt { get; set; }
}

public class MessageType : InterfaceType<IMessage>
{
    protected override void Configure(IInterfaceTypeDescriptor<IMessage> descriptor)
    {
        descriptor.Name("Message");
    }
}

public class TextMessage : IMessage
{
    public User Author { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Content { get; set; }
}

public class TextMessageType : ObjectType<TextMessage>
{
    protected override void Configure(IObjectTypeDescriptor<TextMessage> descriptor)
    {
        descriptor.Name("TextMessage");

        descriptor.Implements<MessageType>();
    }
}

public class Query
{
    public IMessage[] GetMessages([Service] IMessageRepository repository)
        => repository.GetMessages();
}

public class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor.Name("Query");

        descriptor
            .Field(f => f.GetMessages(default));
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddQueryType<QueryType>()
            .AddType<TextMessageType>();
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

TODO: Fix up

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddRouting()
        .AddGraphQLServer()
        .AddDocumentFromString(@"
        type Query {
            messages: Message
        }

        interface Message {
          sendBy: User!
          createdAt: DateTime!
        }

        type TextMessage implements Message {
          sendBy: User!
          createdAt: DateTime!
          content: String!
        }

        interface MediaMessage implements Message {
          sendBy: User!
          createdAt: DateTime!
          mediaType: MediaType!
        }

        type VideoMessage implements MediaMessage {
          sendBy: User!
          createdAt: DateTime!
          mediaType: MediaType!
          videoUrl: String!
        }

        type VideoMessage implements Message & HasMediaType {
          sendBy: User!
          createdAt: DateTime!
          mediaType: MediaType!
          videoUrl: String!
        }
        ")
        .AddResolver(
            "Query",
            "messages",
            (context, token) => context.Service<IMessageRepo>().GetMessages());
}
```

</ExampleTabs.Schema>
</ExampleTabs>

## Custom Resolvers

TODO
