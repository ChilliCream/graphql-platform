---
title: "Interfaces"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

An interface is an abstract type that defines a certain set of fields that a type must include to implement the interface.

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

Clients can query fields returning an interface like the following.

```graphql
{
  messages {
    author {
      name
    }
    ... on TextMessage {
      content
    }
  }
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

<!-- We can also use classes to define an interface.

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
``` -->

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

> Note: We have to explicitly register the interface implementations:
>
> ```csharp
> services.AddType<TextMessageType>()
> ```

# Interfaces implementing interfaces

Interfaces can also implement other interfaces.

```sdl
interface Message {
  author: User
}

interface DatedMessage implements Message {
  createdAt: DateTime!
  author: User
}

type TextMessage implements DatedMessage & Message {
  author: User
  createdAt: DateTime!
  content: String
}
```

We can implement this like the following.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
[InterfaceType("Message")]
public interface IMessage
{
    User Author { get; set; }
}

[InterfaceType("DatedMessage")]
public interface IDatedMessage : IMessage
{
    DateTime CreatedAt { get; set; }
}

public class TextMessage : IDatedMessage
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
            .AddType<IDatedMessage>()
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
}

public class MessageType : InterfaceType<IMessage>
{
    protected override void Configure(IInterfaceTypeDescriptor<IMessage> descriptor)
    {
        descriptor.Name("Message");
    }
}

public interface IDatedMessage : IMessage
{
    DateTime CreatedAt { get; set; }
}

public class DatedMessageType : InterfaceType<IDatedMessage>
{
    protected override void Configure(IInterfaceTypeDescriptor<IDatedMessage> descriptor)
    {
        descriptor.Name("DatedMessage");

        descriptor.Implements<MessageType>();
    }
}

public class TextMessage : IDatedMessage
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
        descriptor.Implements<DatedMessageType>();
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
            .AddType<DatedMessageType>()
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
              author: User
            }

            interface DatedMessage implements Message {
              createdAt: DateTime!
              author: User
            }

            type TextMessage implements DatedMessage & Message {
              author: User
              createdAt: DateTime!
              content: String
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

> Note: We also have to register the `DatedMessage` interface manually, if we do not expose it through a field directly:
>
> ```csharp
> services.AddType<DatedMessageType>()
> ```

<!-- todo: this name feels not correct -->

# Custom Resolvers

We can also declare additional resolvers on our interfaces.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
[InterfaceType("Message")]
public interface IMessage
{
    User Author { get; set; }

    DateTime GetCreatedAt();
}

public class TextMessage : IMessage
{
    public User Author { get; set; }

    public DateTime GetCreatedAt()
    {
        // Omitted code for brevity
    }
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public interface IMessage
{
    User Author { get; set; }

    DateTime GetCreatedAt();
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

    public DateTime GetCreatedAt()
    {
        // Omitted code for brevity
    }
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
```

If we do not want to pollute our interface with methods, we can also declare them directly on the interface type.

TODO: should this cover `descriptor.Field`? What is the recommended approach?

Before properties on our interfaces were automatically transformed into default resolvers on the implementing types.
Now that we have specified custom resolvers, all implementing types need to provide an implementation of this resolver.

</ExampleTabs.Code>
<ExampleTabs.Schema>

TODO

</ExampleTabs.Schema>
</ExampleTabs>
