---
title: "Interfaces"
---

An interface is an abstract type that defines a certain set of fields that an object type or another interface must include to implement the interface.

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
<Implementation>

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
    public IMessage[] GetMessages()
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
            .AddType<TextMessage>();
    }
}
```

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

</Implementation>
<Code>

```csharp
public interface IMessage
{
    User Author { get; set; }

    DateTime CreatedAt { get; set; }
}

public class MessageType : InterfaceType<IMessage>
{
    protected override void Configure(
        IInterfaceTypeDescriptor<IMessage> descriptor)
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
    protected override void Configure(
        IObjectTypeDescriptor<TextMessage> descriptor)
    {
        descriptor.Name("TextMessage");

        // The interface that is being implemented
        descriptor.Implements<MessageType>();
    }
}

public class Query
{
    public IMessage[] GetMessages()
    {
        // Omitted code for brevity
    }
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

</Code>
<Schema>

```csharp
public interface IMessage
{
    string Author { get; set; }

    DateTime CreatedAt { get; set; }
}
public class TextMessage : IMessage
{
    public string Author { get; set; }

    public DateTime CreatedAt { get; set; }

    public string Content { get; set; }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
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
            .BindComplexType<TextMessage>()
            .AddResolver("Query", "messages", (context) =>
            {
                // Omitted code for brevity
            });
    }
}
```

</Schema>
</ExampleTabs>

> Note: We have to explicitly register the interface implementations:
>
> ```csharp
> services.AddGraphQLServer().AddType<TextMessageType>()
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
<Implementation>

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
    public IMessage[] GetMessages()
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
            .AddType<IDatedMessage>()
            .AddType<TextMessage>();
    }
}
```

</Implementation>
<Code>

```csharp
public interface IMessage
{
    User Author { get; set; }
}

public class MessageType : InterfaceType<IMessage>
{
    protected override void Configure(
        IInterfaceTypeDescriptor<IMessage> descriptor)
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
    protected override void Configure(
        IInterfaceTypeDescriptor<IDatedMessage> descriptor)
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
    protected override void Configure(
        IObjectTypeDescriptor<TextMessage> descriptor)
    {
        descriptor.Name("TextMessage");

        // The interface that is being implemented
        descriptor.Implements<DatedMessageType>();
    }
}

public class Query
{
    public IMessage[] GetMessages()
    {
        // Omitted code for brevity
    }
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

</Code>
<Schema>

```csharp
public interface IMessage
{
    User Author { get; set; }
}

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

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
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
            .BindComplexType<TextMessage>()
            .AddResolver("Query", "messages", (context) =>
            {
                // Omitted code for brevity
            });
    }
}
```

</Schema>
</ExampleTabs>

> Note: We also have to register the `DatedMessage` interface manually, if we do not expose it through a field directly:
>
> ```csharp
> services.AddGraphQLServer().AddType<DatedMessageType>()
> ```

# Dynamic fields

We can also declare additional dynamic fields (resolvers) on our interfaces.

<ExampleTabs>
<Implementation>

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

</Implementation>
<Code>

```csharp
public interface IMessage
{
    User Author { get; set; }

    DateTime GetCreatedAt();
}

public class MessageType : InterfaceType<IMessage>
{
    protected override void Configure(
        IInterfaceTypeDescriptor<IMessage> descriptor)
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
    protected override void Configure(
        IObjectTypeDescriptor<TextMessage> descriptor)
    {
        descriptor.Name("TextMessage");

        // The interface that is being implemented
        descriptor.Implements<MessageType>();
    }
}
```

If we do not want to pollute our interface with methods, we can also declare them directly on the interface type.

```csharp
public class MessageType : InterfaceType<IMessage>
{
    protected override void Configure(
        IInterfaceTypeDescriptor<IMessage> descriptor)
    {
        descriptor.Name("Message");

        // this is an additional field
        descriptor
            .Field("createdAt")
            .Type<DateTimeType>();
    }
}

public class TextMessage : IMessage
{
    public User Author { get; set; }
}

public class TextMessageType : ObjectType<TextMessage>
{
    protected override void Configure(
        IObjectTypeDescriptor<TextMessage> descriptor)
    {
        descriptor.Name("TextMessage");

        // The interface that is being implemented
        descriptor.Implements<MessageType>();

        descriptor
            .Field("createdAt")
            .Resolve(context =>
            {
                // Omitted code for brevity
            });
    }
}
```

We do not have to use the `descriptor`, we could also create a new method or property named `CreatedAt` in the `TextMessage` class.

If we are dealing with lots of interface implementations, which all have the same logic for resolving a dynamic field, we can create an extension method for the field declarations.

```csharp
public static class MessageExtensions
{
    public static void AddCreatedAt<T>(this IObjectTypeDescriptor<T> descriptor)
        where T : IMessage
    {
        descriptor
            .Field("createdAt")
            .Resolve(context =>
            {
                // Omitted code for brevity
            });
    }
}

public class TextMessageType : ObjectType<TextMessage>
{
    protected override void Configure(
        IObjectTypeDescriptor<TextMessage> descriptor)
    {
        descriptor.Name("TextMessage");

        // The interface that is being implemented
        descriptor.Implements<MessageType>();

        // call to our extension method defined above
        descriptor.AddCreatedAt();
    }
}
```

</Code>
<Schema>

```csharp
public interface IMessage
{
    string Author { get; set; }
}

public class TextMessage : IMessage
{
    public string Author { get; set; }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddDocumentFromString(@"
                interface Message {
                  author: User
                  createdAt: DateTime!
                }

                type TextMessage implements Message {
                  author: User
                  createdAt: DateTime!
                }
            ")
            .BindComplexType<TextMessage>()
            .AddResolver("TextMessage", "createdAt", (context) =>
            {
                // Omitted code for brevity
            });
    }
}
```

</Schema>
</ExampleTabs>
