---
title: "Interfaces"
---

A GraphQL interface defines a set of fields that multiple object types share. When a field returns an interface type, the client can query the shared fields directly and use fragments to access type-specific fields. Interfaces are output-only types and cannot be used as arguments or input fields.

**GraphQL schema**

```graphql
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

**Client query**

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

The shared `createdAt` field is queried directly on the interface. The `content` field, which exists only on `TextMessage`, is accessed through an inline fragment.

# Defining an Interface Type

Hot Chocolate maps C# interfaces and abstract classes to GraphQL interface types.

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

[QueryType]
public static partial class MessageQueries
{
    public static IMessage[] GetMessages()
    {
        // ...
    }
}
```

```csharp
builder
    .AddGraphQL()
    .AddType<TextMessage>();
```

You must register each implementing type explicitly so Hot Chocolate knows which object types belong to the interface.

You can also use an abstract class instead of an interface:

```csharp
[InterfaceType]
public abstract class Message
{
    public User Author { get; set; }
    public DateTime CreatedAt { get; set; }
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
        descriptor.Implements<MessageType>();
    }
}
```

```csharp
builder
    .AddGraphQL()
    .AddQueryType<QueryType>()
    .AddType<TextMessageType>();
```

</Code>
</ExampleTabs>

# Ignoring Fields

You can exclude specific fields from the GraphQL interface.

<ExampleTabs>
<Implementation>

```csharp
[InterfaceType("Message")]
public interface IMessage
{
    [GraphQLIgnore]
    User Author { get; set; }

    DateTime CreatedAt { get; set; }
}
```

</Implementation>
<Code>

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
</ExampleTabs>

# Overriding Names

Use `[GraphQLName]` or the `Name` method to override inferred names.

<ExampleTabs>
<Implementation>

```csharp
[GraphQLName("Post")]
public interface IMessage
{
    User Author { get; set; }

    [GraphQLName("addedAt")]
    DateTime CreatedAt { get; set; }
}
```

You can also specify the name through the `[InterfaceType]` attribute:

```csharp
[InterfaceType("Post")]
public interface IMessage
```

</Implementation>
<Code>

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
</ExampleTabs>

Both produce the following schema:

```graphql
interface Post {
  author: User!
  addedAt: DateTime!
}
```

# Interfaces Implementing Interfaces

GraphQL interfaces can implement other interfaces, forming a hierarchy.

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
```

```csharp
builder
    .AddGraphQL()
    .AddType<IDatedMessage>()
    .AddType<TextMessage>();
```

</Implementation>
<Code>

```csharp
public class DatedMessageType : InterfaceType<IDatedMessage>
{
    protected override void Configure(
        IInterfaceTypeDescriptor<IDatedMessage> descriptor)
    {
        descriptor.Name("DatedMessage");
        descriptor.Implements<MessageType>();
    }
}

public class TextMessageType : ObjectType<TextMessage>
{
    protected override void Configure(
        IObjectTypeDescriptor<TextMessage> descriptor)
    {
        descriptor.Implements<DatedMessageType>();
    }
}
```

```csharp
builder
    .AddGraphQL()
    .AddQueryType<QueryType>()
    .AddType<DatedMessageType>()
    .AddType<TextMessageType>();
```

</Code>
</ExampleTabs>

Register intermediate interfaces (like `DatedMessage`) explicitly if they are not returned directly from a resolver field.

# Default Resolvers

Interface fields can define default resolvers, similar to default interface methods in C#. When an object type implements the interface, it automatically inherits the resolver for any field where it does not define its own. If the object type does not even declare the field, the field is created automatically with the interface's resolver.

This is useful when multiple types share the same resolution logic for a field and you want to define it once on the interface rather than repeating it in every implementing type.

<ExampleTabs>
<Implementation>

```csharp
[InterfaceType("Message")]
public interface IMessage
{
    User Author { get; }
    DateTime CreatedAt { get; }
}

[InterfaceType<IMessage>]
public static partial class MessageNode
{
    public static string DisplayName([Parent] IMessage message)
        => $"{message.Author.Name} - {message.CreatedAt:d}";
}
```

All object types implementing `IMessage` inherit the `displayName` field and its resolver. No additional configuration is needed on the implementing types.

</Implementation>
<Code>

```csharp
public class MessageType : InterfaceType
{
    protected override void Configure(IInterfaceTypeDescriptor descriptor)
    {
        descriptor.Name("Message");

        descriptor
            .Field("displayName")
            .Type<StringType>()
            .Resolve(context =>
            {
                var message = context.Parent<IMessage>();
                return $"{message.Author.Name} - {message.CreatedAt:d}";
            });
    }
}

public class TextMessageType : ObjectType<TextMessage>
{
    protected override void Configure(
        IObjectTypeDescriptor<TextMessage> descriptor)
    {
        descriptor.Implements<MessageType>();
    }
}
```

`TextMessageType` inherits the `displayName` field and its resolver from `MessageType`. No additional configuration is needed.

</Code>
</ExampleTabs>

Object types can override an inherited resolver by defining their own resolver for the same field.

# Next Steps

- **Need types without shared fields?** See [Unions](/docs/hotchocolate/v16/defining-a-schema/unions).
- **Need to define output types?** See [Object Types](/docs/hotchocolate/v16/defining-a-schema/object-types).
- **Need to extend an existing type?** See [Extending Types](/docs/hotchocolate/v16/defining-a-schema/extending-types).
- **Need to document interface fields?** See [Documentation](/docs/hotchocolate/v16/defining-a-schema/documentation).
