---
title: Object Type
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

The object type is the most important output type in GraphQL and represents the data we can fetch from our GraphQL server. The GraphQL SDL representation of an object looks like the following:

```sdl
type Starship {
  id: ID!
  name: String!
  length(unit: LengthUnit = METER): Float
}
```

An object type in GraphQL consists of a collection of fields. Each object type has to have at least one field declared to be valid. Object fields in GraphQL can have arguments and are more like methods in _C#_. Each field has a distinct type. All field types have to be output types (scalars, enums, objects, unions, or interfaces). The arguments of a field, on the other hand, have to be input types scalars, enums, and input objects) and
represent raw data that is passed into a field.

> **Note:** Every single code example will be shown in three different approaches, annotation-based (previously known as pure code-first), code-first, and schema-first. However, they will always result in the same outcome on a GraphQL schema perspective and internally in Hot Chocolate. All three approaches have their pros and cons and can be combined when needed with each other. If you would like to learn more about the three approaches in Hot Chocolate, click on [Coding Approaches](/docs/hotchocolate/api-reference/coding-approaches).

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
// Query.cs
public class Query
{
    public Book GetBook() => new Book { Title  = "C# in depth", Author = "Jon Skeet" };
}

// Book.cs
public class Book
{
    public string Title { get; set; }

    public string Author { get; set; }
}

// Startup.cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddRouting()
            .AddGraphQLServer()
            .AddQueryType<Query>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
// Query.cs
public class Query
{
    public Book GetBook() => new Book { Title  = "C# in depth", Author = "Jon Skeet" };
}

// QueryType.cs
public class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field(f => f.GetBook())
            .Type<BookType>();
    }
}

// Book.cs
public class Book
{
    public string Title { get; set; }

    public string Author { get; set; }
}

// BookType.cs
public class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field(f => f.Title)
            .Type<StringType>();

        descriptor
            .Field(f => f.Author)
            .Type<StringType>();
    }
}


// Startup.cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddRouting()
            .AddGraphQLServer()
            .AddQueryType<QueryType>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
// Query.cs
public class Query
{
    public string Say() => "Hello World!";
}

// Startup.cs
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddRouting()
            .AddGraphQLServer()
            .AddDocumentFromString(@"
                type Query {
                  book: Book
                }

                type Book {
                  title: String
                  author: String
                }
            ")
            .BindComplexType<Query>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Schema>
</ExampleTabs>

# Extension

The GraphQL SDL supports extending object types, this means that we can add fields to an existing object type without changing the code of our initial type definition.

Extending types is useful for schema stitching but also when we want to add just something to an exist type or if we just want to split large type definitions. The latter is often the case with the query type definition.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
[ExtendObjectType("Person")]
public class PersonResolvers
{
    public IEnumerable<Person> GetFriends([Parent] Person person, [Service] IPersonRepository repository)
        => repository.GetFriends(person.Id);
}

services
    .AddGraphQLServer()
    // ...
    .AddType<PersonResolvers>();
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class PersonTypeExtension : ObjectTypeExtension
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Person");
        descriptor.Field("address")
            .Type<NonNullType<StringType>>()
            .Resolver(context =>
            {
                // resolver logic
            });
    }
}

services
    .AddGraphQLServer()
    // ...
    .AddType<PersonTypeExtension>()
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```sdl
extend type Person {
  address: String!
}
```

</ExampleTabs.Schema>
</ExampleTabs>

Type extensions basically work like usual types and are also added like usual types.

More about type extensions can be found [here](../defining-a-schema/extending-types).
