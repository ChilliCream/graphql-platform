---
title: "Interfaces"
---

An interface is an abstract type that defines a certain set of fields that an object type or another interface must include to implement the interface. Interfaces can only be used as output types, meaning we can't use interfaces as arguments or as fields on input object types.

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

type Query {
    messages: [Message]!
}
```

# Usage

Given is the schema from above.

When querying a field returning an interface, we can query the fields defined in the interface like we would query a regular object type.

```graphql
{
  messages {
    createdAt
  }
}
```

If we need to access fields that are part of an object type implementing the interface, we can do so using [fragments](https://graphql.org/learn/queries/#fragments).

```graphql
{
  messages {
    createdAt
    ... on TextMessage {
      content
    }
  }
}
```

# Definition

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
```

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddType<TextMessage>();
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
```

```csharp
builder.Services
    .AddGraphQLServer()
    // ...
    .AddType<TextMessage>();
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
```

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<QueryType>()
    .AddType<TextMessageType>();
```

</Code>
<Schema>

```csharp
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
```

```csharp
builder.Services
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
    .BindRuntimeType<TextMessage>()
    .AddResolver("Query", "messages", (context) =>
    {
        // Omitted code for brevity
    });
```

</Schema>
</ExampleTabs>

> Note: We have to explicitly register the interface implementations:
>
> ```csharp
> services.AddGraphQLServer().AddType<TextMessageType>()
> ```

# Binding behavior

In the implementation-first approach all public properties and methods are implicitly mapped to fields on the schema interface type. The same is true for `T` of `InterfaceType<T>` when using the code-first approach.

In the code-first approach we can also enable explicit binding, where we have to opt-in properties and methods we want to include instead of them being implicitly included.

<!-- todo: this should not be covered in each type documentation, rather once in a server configuration section -->

We can configure our preferred binding behavior globally like the following.

```csharp
builder.Services
    .AddGraphQLServer()
    .ModifyOptions(options =>
    {
        options.DefaultBindingBehavior = BindingBehavior.Explicit;
    });
```

> Warning: This changes the binding behavior for all types, not only interface types.

We can also override it on a per type basis:

```csharp
public class MessageType : InterfaceType<IMessage>
{
    protected override void Configure(
        IInterfaceTypeDescriptor<IMessage> descriptor)
    {
        descriptor.BindFields(BindingBehavior.Implicit);

        // We could also use the following methods respectively
        // descriptor.BindFieldsExplicitly();
        // descriptor.BindFieldsImplicitly();
    }
}
```

## Ignoring fields

<ExampleTabs>
<Implementation>

In the implementation-first approach we can ignore fields using the `[GraphQLIgnore]` attribute.

```csharp
public interface IMessage
{
    [GraphQLIgnore]
    User Author { get; set; }

    DateTime CreatedAt { get; set; }
}
```

</Implementation>
<Code>

In the code-first approach we can ignore fields using the `Ignore` method on the `IInterfaceTypeDescriptor`. This is only necessary, if the binding behavior of the interface type is implicit.

```csharp
public class MessageType : InterfaceType<IMessage>
{
    protected override void Configure(
        IInterfaceTypeDescriptor<IMessage> descriptor)
    {
        descriptor.Ignore(f => f.Author);
    }
}

```

</Code>
<Schema>

We do not have to ignore fields in the schema-first approach.

</Schema>
</ExampleTabs>

## Including fields

In the code-first approach we can explicitly include properties of our POCO using the `Field` method on the `IInterfaceTypeDescriptor`. This is only necessary, if the binding behavior of the interface type is explicit.

```csharp
public class MessageType : InterfaceType<IMessage>
{
    protected override void Configure(
        IInterfaceTypeDescriptor<IMessage> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor.Field(f => f.Title);
    }
}
```

# Naming

Unless specified explicitly, Hot Chocolate automatically infers the names of interface types and their fields. Per default the name of the interface / abstract class becomes the name of the interface type. When using `InterfaceType<T>` in code-first, the name of `T` is chosen as the name for the interface type. The names of methods and properties on the respective interface / abstract class are chosen as names of the fields of the interface type

If we need to we can override these inferred names.

<ExampleTabs>
<Implementation>

The `[GraphQLName]` attribute allows us to specify an explicit name.

```csharp
[GraphQLName("Post")]
public interface IMessage
{
    User Author { get; set; }

    [GraphQLName("addedAt")]
    DateTime CreatedAt { get; set; }
}
```

We can also specify a name for the interface type using the `[InterfaceType]` attribute.

```csharp
[InterfaceType("Post")]
public interface IMessage
```

</Implementation>
<Code>

The `Name` method on the `IInterfaceTypeDescriptor` / `IInterfaceFieldDescriptor` allows us to specify an explicit name.

```csharp
public class MessageType : InterfaceType<IMessage>
{
    protected override void Configure(
        IInterfaceTypeDescriptor<IMessage> descriptor)
    {
        descriptor.Name("Post");

        descriptor
            .Field(f => f.CreatedAt)
            .Name("addedAt");
    }
}
```

</Code>
<Schema>

Simply change the names in the schema.

</Schema>
</ExampleTabs>

This would produce the following `Post` schema interface type:

```sdl
interface Post {
  author: User!
  addedAt: DateTime!
}
```

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
```

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddType<IDatedMessage>()
    .AddType<TextMessage>();
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
```

```csharp
builder.Services
    .AddGraphQLServer()
    .AddQueryType<QueryType>()
    .AddType<DatedMessageType>()
    .AddType<TextMessageType>();
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
```

```csharp
builder.Services
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
    .BindRuntimeType<TextMessage>()
    .AddResolver("Query", "messages", (context) =>
    {
        // Omitted code for brevity
    });
```

</Schema>
</ExampleTabs>

> Note: We also have to register the `DatedMessage` interface manually, if we do not expose it through a field directly:
>
> ```csharp
> services.AddGraphQLServer().AddType<DatedMessageType>()
> ```
