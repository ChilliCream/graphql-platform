---
title: "Interfaces"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

> We are still working on the documentation for Hot Chocolate 11.1 so help us by finding typos, missing things or write some additional docs with us.

An Interface is an abstract type that includes a certain set of fields that a type must include to implement the interface.

Interfaces are defined in the schema as follows.

```sdl
interface Message {
  sendBy: User!
  createdAt: DateTime!
}

type TextMessage implements Message {
  sendBy: User!
  createdAt: DateTime!
  content: String!
}
```

Learn more about interfaces [here](https://graphql.org/learn/schema/#interfaces).

<!-- ## Interface Definition

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
</ExampleTabs> -->
