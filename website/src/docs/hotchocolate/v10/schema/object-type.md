---
title: Object Type
---

The object type is the most prominent output type in GraphQL and represents a kind of object we can fetch from our schema. The GraphQL schema representation of an object looks like the following:

```sdl
type Starship {
  id: ID!
  name: String!
  length(unit: LengthUnit = METER): Float
}
```

An object in GraphQL consists of a collection of fields. Object fields in GraphQL can have arguments, so we could compare it to methods in _C#_. Each field has a distinct type. All field types have to be output types (scalars, enums, objects, unions or interfaces). The arguments of a field on the other hand have to be input types scalars, enums and input objects).

With Hot Chocolate we can define an object by using the GraphQL SDL syntax or by using C#. Each field of an object will get a resolver assigned that knows how to fetch the data for that field.

A single GraphQL object might be the composition of data that comes from several data sources.

If we take the following object for instance:

```sdl
type Query {
  sayHello: String!
}
```

We could define this like the following:

```csharp
SchemaBuilder.New()
  .AddDocumentFromString(@"
      type Query {
        sayHello: String!
      }")
  .AddResolver(context => "Hello!")
  .Create();
```

With C# we could define it like the following:

```csharp
public class Query
{
    public string SayHello() => "Hello!";
}

SchemaBuilder.New()
  .AddQueryType<Query>()
  .Create();
```

GraphQL has a concept of nun-null types. Basically any type can be a non-nullable type, in the SDL we decorate non-nullable types with the `Bang` token `!`. In order to describe this in C# we can use attributes, use C# 8 and nullable reference types or use the underlying schema types to describe our GraphQL type explicitly.

This is how it would look like with our attributes:

```csharp
public class Query
{
    [GraphQLNonNullType]
    public string SayHello() => "Hello!";
}

SchemaBuilder.New()
  .AddQueryType<Query>()
  .Create();
```

With C# 8.0 we can enable nullable reference type either in our project:

```xml
<PropertyGroup>
  <Nullable>enable</Nullable>
</PropertyGroup>
```

Or we could use pre-processor directives to opt-in on a by file base:

```csharp
#nullable enable

public class Query
{
    public string SayHello() => "Hello!";
}

SchemaBuilder.New()
  .AddQueryType<Query>()
  .Create();
```

With schema types the same thing would look like the following:

```csharp
public class Query
{
    public string SayHello() => "Hello!";
}

public class QueryType
    : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor.Field(t => t.SayHello()).Type<NonNullType<StringType>>();
    }
}

SchemaBuilder.New()
  .AddQueryType<QueryType>()
  .Create();
```

# Resolvers

Schema types will also allow us to add fields that are not on our current model.
Let\`s say we have the following C# model:

```csharp
public class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```

And we want to expose the following object to our schema users:

```sdl
type Person {
  id: Int!
  name: String!
  friends: [Person]
}
```

Then we could do something like this:

```csharp
public class PersonType
    : ObjectType<Person>
{
    protected override void Configure(IObjectTypeDescriptor<Person> descriptor)
    {
        descriptor.Field(t => t.Name).Type<NonNullType<StringType>>();
        descriptor.Field("friends")
            .Type<ListType<NonNullType<PersonType>>>()
            .Resolver(context =>
                context.Service<IPersonRepository>().GetFriends(
                    context.Parent<Person>().Id));
    }
}
```

Let\`s have a look at the above example, first we have our name field there, since we need to declare it non-nullable.
But we do not have the `id` field there. Hot Chocolate will always try to infer the usage of the provided type if it is not overridden by the user. We always can opt out of this behavior and tell Hot Chocolate that we do want to declare everything explicitly.

In the case of value types Hot Chocolate can infer the non-nullability correctly in any C# version and we do not have to specify anything extra.

The second thing that is important in this example is that we can introduce fields that are not on our model and that might even come from a completely different data source. In these cases, we have to provide explicit resolvers since we cannot infer the resolver from the C# type.

We also can use schema types if we have no .NET backing type at all. In these cases, we have to write explicit resolvers for each of the fields:

```csharp
public class QueryType
    : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Field("sayHello")
            .Type<NonNullType<StringType>>()
            .Resolver("Hello!");
    }
}
```

We can also turn that around and write our resolver logic in our C# objects since we support method argument injection. We could also create our `Person` type in C# like the following:

```csharp
public class Person
{
    public int Id { get; set; }
    public string Name { get; set; }

    public IEnumerable<Person> GetFriends([Service]IPersonRepository repository) =>
        repository.GetFriends(Id);
}
```

Since in many cases we do not want to put resolver code in our business objects we can also split our type and still move the resolver code to a C# class:

Pure Code-First:

```csharp
public class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
}

[ExtendObjectType(Name = "Person")]
public class PersonResolvers
{
    public IEnumerable<Person> GetFriends(Person person, [Service]IPersonRepository repository) =>
        repository.GetFriends(person.Id);
}
```

Code-First:

```csharp
public class Person
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class PersonResolvers
{
    public IEnumerable<Person> GetFriends([Parent]Person person, [Service]IPersonRepository repository) =>
        repository.GetFriends(person.Id);
}

public class PersonType
    : ObjectType<Person>
{
    protected override void Configure(IObjectTypeDescriptor<Person> descriptor)
    {
        descriptor.Field(t => t.Name).Type<NonNullType<StringType>>();
        descriptor.Field<PersonResolvers>(t => t.GetFriends(default, default))
            .Type<ListType<NonNullType<PersonType>>>();
    }
}
```

> More about resolvers can be read [here](/docs/hotchocolate/v10/schema/resolvers).

# Extension

The GraphQL SDL supports extending object types, this means that we can add fields to an existing object type without changing the code of our initial type definition.

Extending types is useful for schema stitching but also when we want to add just something to an exist type or if we just want to split large type definitions. The latter is often the case with the query type definition.

Hot Chocolate supports extending types with SDL-first, pure code-first and code-first. Let\`s have a look at how we can extend our person object:

SDL-First:

```sdl
extend type Person {
  address: String!
}
```

Pure Code-First:

```csharp
[ExtendObjectType(Name = "Person")]
public class PersonResolvers
{
    public IEnumerable<Person> GetFriends([Parent]Person person, [Service]IPersonRepository repository) =>
        repository.GetFriends(person.Id);
}

SchemaBuilder.New()
  ...
  .AddType<PersonType>()
  .AddType<PersonResolvers>()
  .Create();
```

Code-First

```csharp
public class PersonTypeExtension
    : ObjectTypeExtension
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Person");
        descriptor.Field("address")
            .Type<NonNullType<StringType>>()
            .Resolver(/"Resolver Logic"/);
    }
}

SchemaBuilder.New()
  ..
  .AddType<PersonType>()
  .AddType<PersonTypeExtension>()
  .Create();
```

Type extensions basically work like usual types and are also added like usual types.
