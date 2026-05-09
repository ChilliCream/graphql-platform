---
title: "Interfaces"
---

A GraphQL interface defines a set of fields shared by multiple object types. When a field returns an interface, clients can select these shared fields directly and use fragments to access fields specific to concrete object types.

Use interfaces for output polymorphism when all possible results share a meaningful contract. If your result types do not have common fields, use a [union](./unions). If there is only one concrete shape, use an [object type](./object-types).

```graphql
interface Message {
  id: String!
  role: ChatMessageRole!
  sentAt: DateTime!
}

type ChatMessage implements Message {
  id: String!
  role: ChatMessageRole!
  sentAt: DateTime!
  content: String!
}

type DocumentMessage implements Message {
  id: String!
  role: ChatMessageRole!
  sentAt: DateTime!
  documentUrl: String!
}
```

Clients can query `id`, `role`, and `sentAt` on `Message`. The `content` and `documentUrl` fields require fragments because they are only present on specific object types.

# When to Use an Interface

Decide on the schema element before writing code. Interfaces are most effective when the shared fields are part of the API contract, not an implementation detail alone.

| Need                                  | Use                                                   | Notes                                           |
| ------------------------------------- | ----------------------------------------------------- | ----------------------------------------------- |
| Common fields on all results          | Interface                                             | Clients can select shared fields directly.      |
| Related results without common fields | [Union](./unions)                                     | Clients use fragments for every concrete shape. |
| One concrete result shape             | [Object type](./object-types)                         | No abstract runtime resolution is needed.       |
| Polymorphic input                     | [Input object pattern or OneOf](./input-object-types) | GraphQL interfaces are output-only.             |

Use an interface when:

- All possible result types share meaningful fields.
- Clients should be able to query those fields without fragments.
- Your resolver returns an abstract output contract, such as a C# interface or abstract base class.

Avoid interfaces when:

- The possible result types only share context, not fields.
- The field always returns a single concrete object type.
- The polymorphic value is an argument or input field.

# Modeling the Shared Contract in C#

Hot Chocolate can map a C# interface or abstract class to a GraphQL interface. In implementation-first schemas, annotate the CLR contract to control the GraphQL name or to ensure interface type interpretation.

```csharp
// Types/IMessage.cs
#nullable enable

using HotChocolate.Types;

[InterfaceType("Message")]
public interface IMessage
{
    string Id { get; }
    ChatMessageRole Role { get; }
    DateTime SentAt { get; }
}

public enum ChatMessageRole
{
    User,
    Assistant,
    System
}

// Types/ChatMessage.cs
public sealed record ChatMessage(
    string Id,
    ChatMessageRole Role,
    DateTime SentAt,
    string Content) : IMessage;

// Types/DocumentMessage.cs
public sealed record DocumentMessage(
    string Id,
    ChatMessageRole Role,
    DateTime SentAt,
    string DocumentUrl) : IMessage;

// Types/MessageQueries.cs
[QueryType]
public static partial class MessageQueries
{
    public static IReadOnlyList<IMessage> GetMessages()
        => new IMessage[]
        {
            new ChatMessage(
                "m1",
                ChatMessageRole.Assistant,
                new DateTime(2025, 2, 1, 10, 0, 0, DateTimeKind.Utc),
                "Welcome to the chat."),
            new DocumentMessage(
                "m2",
                ChatMessageRole.System,
                new DateTime(2025, 2, 1, 10, 1, 0, DateTimeKind.Utc),
                "https://example.com/intro.pdf")
        };
}
```

Register the interface and each implementing object type:

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddTypes()
    .AddInterfaceType<IMessage>()
    .AddType<ChatMessage>()
    .AddType<DocumentMessage>();
```

This setup produces a schema equivalent to the following SDL:

```graphql
interface Message {
  id: String!
  role: ChatMessageRole!
  sentAt: DateTime!
}

type ChatMessage implements Message {
  id: String!
  role: ChatMessageRole!
  sentAt: DateTime!
  content: String!
}

type DocumentMessage implements Message {
  id: String!
  role: ChatMessageRole!
  sentAt: DateTime!
  documentUrl: String!
}

enum ChatMessageRole {
  USER
  ASSISTANT
  SYSTEM
}

type Query {
  messages: [Message!]!
}
```

Hot Chocolate does not automatically add every CLR implementation to the schema because a resolver returns `IMessage`. You must register each possible object type with `.AddType<T>()`, a generated registration method, or an explicit `ObjectType<T>` registration.

# Querying Interface Fields with Fragments

You can select shared fields directly on the interface. Use `__typename` if the client needs to distinguish between concrete result shapes.

```graphql
query GetMessages {
  messages {
    __typename
    id
    role
    sentAt
    ... on ChatMessage {
      content
    }
    ... on DocumentMessage {
      documentUrl
    }
  }
}
```

The expected response shape is:

```json
{
  "data": {
    "messages": [
      {
        "__typename": "ChatMessage",
        "id": "m1",
        "role": "ASSISTANT",
        "sentAt": "2025-02-01T10:00:00Z",
        "content": "Welcome to the chat."
      },
      {
        "__typename": "DocumentMessage",
        "id": "m2",
        "role": "SYSTEM",
        "sentAt": "2025-02-01T10:01:00Z",
        "documentUrl": "https://example.com/intro.pdf"
      }
    ]
  }
}
```

Use named fragments when multiple queries need the same selections:

```graphql
fragment MessageFields on Message {
  __typename
  id
  role
  sentAt
}

fragment ChatMessageFields on ChatMessage {
  content
}

fragment DocumentMessageFields on DocumentMessage {
  documentUrl
}

query GetMessages {
  messages {
    ...MessageFields
    ...ChatMessageFields
    ...DocumentMessageFields
  }
}
```

GraphQL validation will reject a concrete field selected directly on the interface:

```graphql
query InvalidMessagesQuery {
  messages {
    id
    content
  }
}
```

The `content` field belongs to `ChatMessage`, not `Message`. Move it into `... on ChatMessage { content }`.

# Configuring an Interface with `InterfaceType<T>`

Use `InterfaceType<T>` for fluent configuration of names, descriptions, binding behavior, directives, interface inheritance, or advanced runtime matching.

```csharp
// Types/MessageType.cs
using HotChocolate.Types;

public sealed class MessageType : InterfaceType<IMessage>
{
    protected override void Configure(
        IInterfaceTypeDescriptor<IMessage> descriptor)
    {
        descriptor.Name("Message");
        descriptor.Description("A message that can appear in a chat transcript.");

        descriptor
            .Field(t => t.SentAt)
            .Description("The UTC time when the message was sent.");
    }
}

// Types/ChatMessageType.cs
public sealed class ChatMessageType : ObjectType<ChatMessage>
{
    protected override void Configure(
        IObjectTypeDescriptor<ChatMessage> descriptor)
    {
        descriptor.Implements<MessageType>();
    }
}

// Types/DocumentMessageType.cs
public sealed class DocumentMessageType : ObjectType<DocumentMessage>
{
    protected override void Configure(
        IObjectTypeDescriptor<DocumentMessage> descriptor)
    {
        descriptor.Implements<MessageType>();
    }
}
```

Register the interface type and the object type configurations:

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddType<MessageType>()
    .AddType<ChatMessageType>()
    .AddType<DocumentMessageType>();
```

Keep fields that exist only on a single concrete object type on that object type. For example, configure `ChatMessage.Content` on `ChatMessageType`, not on `MessageType`.

# Configuring Interface Fields

By default, Hot Chocolate uses implicit binding. Public members on the C# interface or abstract class become GraphQL interface fields. Switch to explicit binding if you want the interface contract to opt in each field.

```csharp
public sealed class MessageType : InterfaceType<IMessage>
{
    protected override void Configure(
        IInterfaceTypeDescriptor<IMessage> descriptor)
    {
        descriptor.Name("Message");
        descriptor.BindFieldsExplicitly();

        descriptor.Field(t => t.Id);
        descriptor.Field(t => t.Role);

        descriptor
            .Field(t => t.SentAt)
            .Name("sentAt");
    }
}
```

Use a consistent naming style per type to keep the schema predictable.

| Task                        | Attribute API                                            | Fluent API                                                                           | Notes                                                           |
| --------------------------- | -------------------------------------------------------- | ------------------------------------------------------------------------------------ | --------------------------------------------------------------- |
| Name the interface          | `[InterfaceType("Message")]`, `[GraphQLName("Message")]` | `descriptor.Name("Message")`                                                         | Prefer one naming style per type.                               |
| Name a field                | `[GraphQLName("sentAt")]`                                | `.Field(t => t.SentAt).Name("sentAt")`                                               | Implementors must satisfy the same GraphQL field name and type. |
| Exclude a field             | `[GraphQLIgnore]`                                        | `descriptor.Ignore(t => t.InternalState)`                                            | Applies to members inferred from the CLR contract.              |
| Require opt-in fields       | Not available                                            | `descriptor.BindFieldsExplicitly()`                                                  | Pair with `descriptor.Field(...)`.                              |
| Restore inferred fields     | Not available                                            | `descriptor.BindFieldsImplicitly()`                                                  | Useful when a global default changed binding behavior.          |
| Implement another interface | CLR inheritance plus registration                        | `descriptor.Implements<OtherInterfaceType>()`                                        | Register intermediate interfaces when needed.                   |
| Customize runtime matching  | Not available                                            | Object `descriptor.IsOfType(...)` or interface `descriptor.ResolveAbstractType(...)` | Use only when the defaults do not match your runtime values.    |

For field nullability, descriptions, directives, and object-only field configuration, see [Object types](./object-types), [Non-Null](/docs/hotchocolate/v16/build/schema-elements/lists-and-non-null), and [Documentation](/docs/hotchocolate/v16/build/schema-elements/documentation-comments).

# Understanding Runtime Type Resolution

When a resolver returns an interface value, Hot Chocolate must select a concrete GraphQL object type for each runtime value.

1. The resolver returns a value typed as `IMessage`, an abstract class, or another abstract contract.
2. Hot Chocolate reads the possible object types registered for the GraphQL interface.
3. Each possible object type checks whether it represents the runtime value.
4. The selected object type determines which fragments match and which concrete fields can resolve.

This resolution step makes implementor registration important. If `DocumentMessage` is not registered, it will not appear in introspection and Hot Chocolate cannot select it at execution time.

Most schemas can rely on the default runtime matching. Configure an object type with `IsOfType(...)` if a type needs custom instance matching. Use interface `ResolveAbstractType(...)` only when the mapping must inspect a wrapper, discriminator, or domain value and return a registered `ObjectType`. This delegate shape is advanced, so keep this configuration close to tests that exercise every possible result type.

For diagnostics, schema options include `StrictRuntimeTypeValidation` for stricter union and interface runtime validation, and `DefaultIsOfTypeCheck` as an abstract type fallback. See [Options](/docs/hotchocolate/v16/build/server-configuration/schema-options) for option names and defaults.

# Using Abstract Classes and C# Interfaces Carefully

A C# interface or abstract class is a convenient source for GraphQL interface fields. The GraphQL contract is still defined by the schema: registered object types must provide fields that match the interface field names, types, and nullability.

You can model the same interface with an abstract class:

```csharp
[InterfaceType("Message")]
public abstract class MessageBase
{
    public string Id { get; init; } = default!;
    public ChatMessageRole Role { get; init; }
    public DateTime SentAt { get; init; }
}
```

A marker C# interface with no shared members usually describes a union-shaped problem. Choose an interface when the shared fields are useful to clients.

# Interfaces Implementing Other Interfaces

GraphQL interfaces can implement other interfaces. Use this to layer small contracts, but avoid deep hierarchies that make the schema difficult to read.

```graphql
interface Message {
  id: String!
  role: ChatMessageRole!
}

interface TimestampedMessage implements Message {
  id: String!
  role: ChatMessageRole!
  sentAt: DateTime!
}

type ChatMessage implements Message & TimestampedMessage {
  id: String!
  role: ChatMessageRole!
  sentAt: DateTime!
  content: String!
}
```

In implementation-first schemas, CLR inheritance can express this relationship:

```csharp
[InterfaceType("Message")]
public interface IMessage
{
    string Id { get; }
    ChatMessageRole Role { get; }
}

[InterfaceType("TimestampedMessage")]
public interface ITimestampedMessage : IMessage
{
    DateTime SentAt { get; }
}
```

Register intermediate interfaces when they are not otherwise reachable from resolver return types:

```csharp
builder
    .AddGraphQL()
    .AddInterfaceType<IMessage>()
    .AddInterfaceType<ITimestampedMessage>()
    .AddType<ChatMessage>();
```

In code-first schemas, call `descriptor.Implements<OtherInterfaceType>()` from the derived interface type configuration and from object types that implement it.

# Related Interface Workflows

Several Hot Chocolate features build on GraphQL interfaces. Refer to their dedicated pages for complete workflows.

| Workflow                     | What to know                                                                                                                                                             | Read next                                                                       |
| ---------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------- |
| Relay `Node`                 | `Node` is the Relay global identification interface with an `id: ID!` field. Hot Chocolate provides `[Node]`, `.ImplementsNode()`, ID serialization, and node resolvers. | [Relay](/docs/hotchocolate/v16/build/schema-elements/relay)                     |
| Type extensions by interface | `[ExtendObjectType(typeof(SomeInterface))]` adds fields to implementing object types, not to the interface type itself.                                                  | [Extending types](/docs/hotchocolate/v16/build/schema-elements/extending-types) |
| Error interfaces             | Mutation error payloads can expose shared error fields through an output interface. Keep error modeling with mutation documentation.                                     | [Mutations](/docs/hotchocolate/v16/build/schema-elements/operations-mutations)  |
| Selection sets and fragments | Interface queries use the same selection set and fragment rules as other GraphQL queries.                                                                                | [Queries](/docs/hotchocolate/v16/build/schema-elements/operations-queries)      |

# Troubleshooting Interfaces

| Symptom                                                         | Likely cause                                                                           | Fix                                                                                                                                    |
| --------------------------------------------------------------- | -------------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------- |
| Implementing type is absent from the schema                     | The object type is not registered or not reachable                                     | Register all implementors with `.AddType<T>()`, generated registration, or object type classes.                                        |
| Interface field resolves, but a concrete fragment never matches | The runtime value does not match any registered possible object type                   | Register the concrete type, return the expected CLR runtime type, configure object `IsOfType(...)`, or use `ResolveAbstractType(...)`. |
| Runtime error says an abstract type could not be resolved       | No possible type matched the returned value                                            | Check implementor registration and runtime values. Consider `StrictRuntimeTypeValidation` while diagnosing.                            |
| Interface field is missing from SDL                             | Explicit binding is enabled, the field was ignored, or global binding changed defaults | Add `descriptor.Field(...)`, remove `[GraphQLIgnore]`, or change binding behavior.                                                     |
| Query validation rejects a concrete field                       | The field belongs to an implementor, not the interface                                 | Use an inline or named fragment on the concrete type.                                                                                  |
| Implementor does not satisfy an interface field                 | Field name, nullability, or GraphQL type does not match                                | Align the interface field and object field configuration.                                                                              |
| Extension field appears on objects, not on the interface        | Extension by interface targets implementors                                            | This is expected. See [Extending types](/docs/hotchocolate/v16/build/schema-elements/extending-types).                                 |
| A C# marker interface creates an unhelpful GraphQL interface    | The interface has no shared fields                                                     | Use a [union](./unions) when result types only share context.                                                                          |

# Next Steps

- Define concrete implementors with [Object types](./object-types).
- Model polymorphic output without shared fields using [Unions](./unions).
- Learn about input alternatives with [Input object types](./input-object-types).
- Add fields across implementors with [Extending types](/docs/hotchocolate/v16/build/schema-elements/extending-types).
