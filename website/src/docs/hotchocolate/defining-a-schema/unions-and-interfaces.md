---
title: "Unions and Interfaces"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

Similar to most type systems, GraphQL knows abstract types. There are two kinds of abstract types:
[Interfaces](https://graphql.org/learn/schema/#interfaces) and [Unions](https://graphql.org/learn/schema/#unions)

# Interfaces

An interface type can be used for abstract types that share fields.

```sdl
interface Message {
  sendBy: User!
  createdAt: DateTime!
}
```

An object type or interface type that _implements_ an interface, does have to declare all the fields that are declared on the interface.

```sdl
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
```

A type can also implement multiple interfaces.

```sdl
type VideoMessage implements Message & HasMediaType {
  sendBy: User!
  createdAt: DateTime!
  mediaType: MediaType!
  videoUrl: String!
}
```

## Querying Interfaces

All fields declared on the interface type are available to query directly.
[Inline Fragments 📄](https://spec.graphql.org/June2018/#sec-Inline-Fragments) allow to query for fields of a specific implementation.

```graphql
{
  messages {
    __typename
    sendBy {
      userName
    }
    createdAt
    ... on TextMessage {
      content
    }
    ... on VideoMessage {
      videoUrl
    }
    ... on MediaMessage {
      mediaType
    }
  }
}
```

```json
{
  "messages": [
    {
      "__typename": "TextMessage",
      "sendBy": {
        "userName": "CookingMaster86"
      },
      "createdAt": "2020-01-01T11:43:00Z",
      "context": "Hi there, can you show me how you did it?"
    }
    {
      "__typename": "VideoMessage",
      "sendBy": {
        "userName": "SpicyChicken404"
      },
      "createdAt": "2020-01-01T12:00:00Z",
      "videoUrl": "http://chillicream.com/cooking/recipies",
    }
  ]
}
```

## Interface Definition

HotChocolate tries to infer interfaces from the .Net types.
When a resolver returns an interface, you just have to register the implementation on the schema builder.
HotChocolate will register the types as implementations of the interface.

<ExampleTabs>
<ExampleTabs.Annotation>

In the annotation based approach, you most likely do not need to worry about interfaces at all.

```csharp
public class Query
{
  public IMessage[] GetMessages([Service]IMessageRepo repo) => repo.GetMessages();
}

[GraphQLName("Message")]
public interface IMessage
{
  User SendBy { get; }

  DateTime CreatedAt { get; }
}

public class TextMessage : IMessage
{
  public User SendBy { get; set; }

  public DateTime CreatedAt { get; set; }

  public string Content { get; set; }
}
// .....
```

_Configure Services_

```csharp
  public void ConfigureServices(IServiceCollection services)
  {
      services
          .AddRouting()
          .AddGraphQLServer()
          .AddQueryType<Query>()
          // HotChocolate knows that TextMessage implements IMessage and will add it to the list
          // of implementations
          .AddType<TextMessage>()
          .AddType<VideoMessage>()
  }
```

You can also use classes as definitions for interfaces.
To mark a base class as an interface definition you can use `[InterfaceType]`.

```csharp
[InterfaceType]
public abstract class Message
{
  public User SendBy { get; set; }

  public DateTime CreatedAt { get; set; }
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

HotChocolate provides a fluent configuration API for interfaces that is very similar to the `ObjectType` interface.

```csharp
public class MediaMessageType : InterfaceType<IMediaMessage>
{
    protected override void Configure(IInterfaceTypeDescriptor<IMediaMessage> descriptor)
    {
        // Configure Type Name
        descriptor.Name("MediaMessage");

        // By default all fields are bound implicitly. This means, all fields of `IMessage` are
        // added to the type and do not have to be added with descriptor.Field(x => x.FieldName)
        // This behaviour can be changed from opt out to opt in by calling BindFieldsExplicitly
        descriptor.BindFieldsExplicitly();

        // Declare  Fields
        descriptor.Field(x => x.MediaType);
        descriptor.Field(x => x.CreatedAt);
        descriptor.Field(x => x.SendBy);

        // This interface implements a interface
        descriptor.Implements<MessageType>();
    }
}
```

In a `ObjectType` you can declare what interface this object type implements.

```csharp
public class VideoMessageType : ObjectType<VideoMessage>
{
    protected override void Configure(IObjectTypeDescriptor<VideoMessage> descriptor)
    {
        descriptor.Implements<MessageType>();
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

In schema first interfaces can be declared directly in SDL:

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

# Unions

Unions are very similar to interfaces. The difference is that members of an unions do not have fields in common.
Unions are useful if you have completely disjunct structured types.

```sdl
type Group {
  id: ID!
  members: [GroupMember]
}

type User {
  userName: String!
}

union GroupMember = User | Group
```

## Querying Unions

Union types do not have fields in common.  
You have to use [Inline Fragments 📄](https://spec.graphql.org/June2018/#sec-Inline-Fragments) to query for fields of a specific implementation.

```graphql
{
  accessControl {
    __typename
    ... on Group {
      id
    }
    ... on User {
      userName
    }
  }
}
```

```json
{
  "accessControl": [
    {
      "__typename": "Group",
      "id": "R3JvdXA6MQ=="
    },
    {
      "__typename": "User",
      "userName": "SpicyChicken404"
    },
    {
      "__typename": "User",
      "userName": "CookingMaster86"
    }
  ]
}
```

## Union Definition

<ExampleTabs>
<ExampleTabs.Annotation>

In the annotation based approach, HotChocolate tries to infer union types from the .Net types.
You can manage the membership of union types with a marker interface.

```csharp
[UnionType("GroupMember")]
public interface IGroupMember
{
}

public class Group : IGroupMember
{
  [Id]
  public Guid Identifier { get; set; }

  public IGroupMember[] Members { get; set; }
}

public class User : IGroupMember
{
  public string UserName { get; set; }
}

public class Query
{
  public IGroupMember[] GetAccessControl([Service]IAccessRepo repo) => repo.GetItems();
}
```

_Configure Services_

```csharp
  public void ConfigureServices(IServiceCollection services)
  {
      services
          .AddRouting()
          .AddGraphQLServer()
          // HotChocolate will pick up IGroupMember as a UnionType<IGroupMember>
          .AddQueryType<Query>()
          // HotChocolate knows that User and Group implement IGroupMember and will add it to the
          // list of possible types of the UnionType
          .AddType<Group>()
          .AddType<User>()
  }
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

HotChocolate provides a fluent configuration API for union types that is very similar to the `ObjectType` interface.

```csharp
// In case you have a marker interface and want to configure it, you can also just user UnionType<IMarkerInterface>
public class GroupMemberType : UnionType
{
    protected override void Configure(IUnionTypeDescriptor descriptor)
    {
        // Configure Type Name
        descriptor.Name("GroupMember");

        // Declare Possible Types
        descriptor.Type<GroupType>();
        descriptor.Type<UserType>();
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

In schema first unions can be declared directly in SDL:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services
        .AddRouting()
        .AddGraphQLServer()
        .AddDocumentFromString(@"
        type Query {
            accessControl: [GroupMember]
        }

        type Group {
            id: ID!
            members: [GroupMember]
        }

        type User {
            userName: String!
        }

        union GroupMember = User | Group
        ")
        .AddResolver(
            "Query",
            "accessControl",
            (context, token) => context.Service<IAccessRepo>().GetItems());
}
```

</ExampleTabs.Schema>
</ExampleTabs>
