---
title: Interface Type
---

The interface is an abstract output type that includes a certain set of fields that an object must include to implement the interface.

The GraphQL schema representation of an interface looks like the following:

```sdl
interface Node {
  id: ID!
}
```

The GraphQL schema representation of an object implementing that interface would look like this:

```sdl
type Starship implements Node {
  id: ID!
  name: String!
  length(unit: LengthUnit = METER): Float
}
```

A single object can implement multiple interfaces and is not limited to just one.

An interface in GraphQL consists of a collection of fields. Multiple objects can implement this interface.

With Hot Chocolate you can define an interface by using the GraphQL SDL syntax or by using C#. In contrast to objects we do not need to specify resolvers for interface fields since the interface only specifies the structure of the data that can be queried but not how to retrieve the data.

In order to specify an interface, we only have to write an actual C# interface.

```csharp
public interface INode
{
    string Id { get; }
}


SchemaBuilder.New()
  .AddType<INode>()
  .Create();
```

Like with objects we can also specify an interface with a schema type to express in more detail what we want our schema type to look like.

```csharp
public class NodeType
    : InterfaceType<INode>
{
    protected override void Configure(IInterfaceTypeDescriptor<INode> descriptor)
    {
        descriptor.Name("Node");
        descriptor.Field(t => t.Id).Type<NonNullType<IdType>>();
    }
}

SchemaBuilder.New()
  .AddType<NodeType>()
  .Create();
```

Also like with any type you have a generic schema type and a non-generic one:

```csharp
public class NodeType
    : InterfaceType
{
    protected override void Configure(IInterfaceTypeDescriptor descriptor)
    {
        descriptor.Name("Node");
        descriptor.Field("id").Type<NonNullType<IdType>>();
    }
}

SchemaBuilder.New()
  .AddType<NodeType>()
  .Create();
```

There are two important things to know here, if you are using a generic schema-type or if you are registering the interface directly with the schema then you do not have to explicitly specify with the object type that the object is implementing this schema since we can infer that.

Let me give you an example:

```csharp
public class Foo : INode
{
    string Id { get; }
}
```

The `Foo` class is implementing `INode` so if we register `INode` with our schema as an interface then the schema would infer that `Foo` implements `INode`.

```csharp
SchemaBuilder.New()
  .AddType<INode>()
  .AddType<Foo>()
  .Create();
```

The upper schema would look like the following in the GraphQL SDL syntax:

```sdl
interface INode {
  id: ID!
}

type Foo implements INode {
  id: ID!
}
```

If we did not register `INode` with our schema we would ignore the interface implementation:

```csharp
SchemaBuilder.New()
  .AddType<Foo>()
  .Create();
```

```sdl
type Foo {
  id: ID!
}
```

But what if we defined the schema with the non-generic base class or if `Foo` did not implement that interface?

In this case we could tell our schema type that we are implementing it.

```csharp
public class FooType
    : ObjectType<Foo>
{
    protected override void Configure(IObjectTypeDescriptor<Foo> descriptor)
    {
        descriptor.Implements<NodeType>();
    }
}
```
