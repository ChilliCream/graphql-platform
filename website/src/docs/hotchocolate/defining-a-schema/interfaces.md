---
title: "Interfaces"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

> We are still working on the documentation for Hot Chocolate 11.1 so help us by finding typos, missing things or write some additional docs with us.

An interface is an abstract type that includes a certain set of fields that a type must include to implement the interface.

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

# Usage

Interfaces can be defined like the following.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
[InterfaceType("Message")]
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

        // The interface that is being implemented
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

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddRouting()
        .AddGraphQLServer()
        .AddDocumentFromString(@"
            type Query {
              messages: [Message]
            }

            interface Message {
              author: User!
              createdAt: DateTime!
            }

            type TextMessage implements Message {
              author: User!
              createdAt: DateTime!
              content: String!
            }
        ")
        .AddResolver(
            "Query",
            "messages",
            (context, ct) => context.Service<IMessageRepository>().GetMessages());
}
```

</ExampleTabs.Schema>
</ExampleTabs>

## Custom Resolvers

TODO

# Interfaces implementing interfaces

TODO
