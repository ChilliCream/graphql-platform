---
title: "Object Types"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

The most important type in a GraphQL schema is the object type. It contains fields that can return simple scalars like `String`, `Int`, or again object types.

```sdl
type Author {
  name: String
}

type Book {
  title: String
  author: Author
}
```

Learn more about object types [here](https://graphql.org/learn/schema/#object-types-and-fields).

# Usage

Object types can be defined like the following.

<ExampleTabs>
<ExampleTabs.Annotation>

In the Annotation-based approach we are essentially just creating regular C# classes.

```csharp
public class Author
{
    public string Name { get; set; }
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

In the Code-first approach we create a new class inheriting from `ObjectType<T>` to map our POCO `Author` to an object type.

```csharp
public class Author
{
    public string Name { get; set; }
}

public class AuthorType : ObjectType<Author>
{
}
```

We can override the `Configure` method to have access to an `IObjectTypeDescriptor` through which we can configure the object type.

```csharp
public class AuthorType : ObjectType<Author>
{
    protected override void Configure(IObjectTypeDescriptor<Author> descriptor)
    {

    }
}
```

The `descriptor` gives us the ability to configure the object type. We will cover how to use it in the following chapters.

We can also create schema object types without a backing POCO.

```csharp
public class AuthorType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {

    }
}
```

Head over [here](#additional-fields) to learn how to add fields to such a type.

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
public class Author
{
    public string Name { get; set; }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddDocumentFromString(@"
                type Author {
                  name: String
                }
            ")
            .BindComplexType<Author>();
    }
}
```

</ExampleTabs.Schema>
</ExampleTabs>

# Binding behavior

In the Annotation-based approach all public properties and methods are implicitly mapped to fields of the schema object type.

In the Code-first approach we have a little more control over this behavior. By default all public properties and methods of our POCO are mapped to fields of the schema object type. This behavior is called implicit binding. There is also an explicit binding behavior, where we have to opt-in properties we want to include.

We can configure our preferred binding behavior globally like the following.

```csharp
services
    .AddGraphQLServer()
    .ModifyOptions(options =>
    {
        options.DefaultBindingBehavior = BindingBehavior.Explicit;
    });
```

We can also override it on a per type basis:

```csharp
public class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
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
<ExampleTabs.Annotation>

In the Annotation-based approach we can ignore fields using the `[GraphQLIgnore]` attribute.

```csharp
public class Book
{
    [GraphQLIgnore]
    public string Title { get; set; }

    public Author Author { get; set; }
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

In the Code-first approach we can ignore certain properties of our POCO using the `Ignore` method on the `descriptor`. This is only necessary, if the binding behavior of the object type is implicit.

```csharp
public class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor.Ignore(f => f.Title);
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

We do not have to ignore fields in the Schema-first approach.

</ExampleTabs.Schema>
</ExampleTabs>

## Including fields

In the Code-first approach we can explicitly include certain properties of our POCO using the `Field` method on the `descriptor`. This is only necessary, if the binding behavior of the object type is explicit.

```csharp
public class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor.Field(f => f.Title);
    }
}
```

# Naming

Hot Chocolate infers the names of the object types and their fields automatically, but sometimes we might want to specify names ourselves.

<ExampleTabs>
<ExampleTabs.Annotation>

Per default the name of the class is the name of the object type in the schema and the names of the properties are the names of the fields of that object type.

We can override these defaults using the `[GraphQLName]` attribute.

```csharp
[GraphQLName("BookAuthor")]
public class Author
{
    [GraphQLName("fullName")]
    public string Name { get; set; }
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

By default, the name of the object type in the schema is either the class name, if we are inheriting from `ObjectType`, or the name of the POCO (`T`) if we are inheriting from `ObjectType<T>`.

We can override these defaults using the `Name` method on the `IObjectTypeDescriptor` / `IObjectFieldDescriptor`.

```csharp
public class AuthorType : ObjectType<Author>
{
    protected override void Configure(IObjectTypeDescriptor<Author> descriptor)
    {
        descriptor.Name("BookAuthor");

        descriptor
            .Field(f => f.Name)
            .Name("fullName");
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

Simply change the names in the schema.

</ExampleTabs.Schema>
</ExampleTabs>

This would produce the following `BookAuthor` schema object type:

```sdl
type BookAuthor {
  fullName: String
}
```

# Explicit types

Hot Chocolate will, most of the time, correctly infer the schema types of our fields. Sometimes we might have to be explicit about it though, for example when we are working with custom scalars.

<ExampleTabs>
<ExampleTabs.Annotation>

In the annotation-based approach we can use the `[GraphQLType]` attribute.

```csharp
public class Author
{
    [GraphQLType(typeof(StringType))]
    public string Name { get; set; }
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

In the Code-first approach we can use the `Type<T>` method on the `IObjectFieldDescriptor`.

```csharp
public class AuthorType : ObjectType<Author>
{
    protected override void Configure(IObjectTypeDescriptor<Author> descriptor)
    {
        descriptor
            .Field(f => f.Name)
            .Type<StringType>();
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

Simply change the field type in the schema.

</ExampleTabs.Schema>
</ExampleTabs>

# Additional fields

We can add additional (dynamic) fields to our schema types, without adding new properties to our backing class.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public class Author
{
    public string Name { get; set; }

    public DateTime AdditionalField()
    {
        // Omitted code for brevity
    }
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

In the Code-first approach we can use the `Resolve` method on the `IObjectFieldDescriptor`.

```csharp
public class AuthorType : ObjectType<Author>
{
    protected override void Configure(IObjectTypeDescriptor<Author> descriptor)
    {
        descriptor
            .Field("AdditionalField")
            .Resolve(context =>
            {
                // Omitted code for brevity
            })
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
public class Author
{
    public string Name { get; set; }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddDocumentFromString(@"
                type Author {
                  name: String
                  additionalField: DateTime!
                }
            ")
            .BindComplexType<Author>()
            .AddResolver("Author", "additionalField", (context) =>
            {
                // Omitted code for brevity
            });
    }
}
```

</ExampleTabs.Schema>
</ExampleTabs>

What we have just created is a resolver. Hot Chocolate automatically creates resolvers for our properties, but we can also define them ourselves.

[Learn more about resolvers](/docs/hotchocolate/fetching-data/resolvers)

# Generics

> Note: Read about [interfaces](/docs/hotchocolate/defining-a-schema/interfaces) and [unions](/docs/hotchocolate/defining-a-schema/unions) before resorting to generic object types.

In the Code-first approach we can define generic object types.

```csharp
public class Response
{
    public string Status { get; set; }

    public object Payload { get; set; }
}

public class ResponseType<T> : ObjectType<Response>
    where T : class, IOutputType
{
    protected override void Configure(
        IObjectTypeDescriptor<Response> descriptor)
    {
        descriptor.Field(f => f.Status);

        descriptor
            .Field(f => f.Payload)
            .Type<T>();
    }
}

public class Query
{
    public Response GetResponse()
    {
        return new Response
        {
            Status = "OK",
            Payload = 123
        };
    }
}

public class QueryType : ObjectType<Query>
{
    protected override void Configure(IObjectTypeDescriptor<Query> descriptor)
    {
        descriptor
            .Field(f => f.GetResponse())
            .Type<ResponseType<IntType>>();
    }
}
```

This will produce the following schema types.

```sdl
type Query {
  response: Response
}

type Response {
  status: String!
  payload: Int
}
```

We have used an `object` as the generic field above, but we can also make `Response` generic and add another generic parameter to the `ResponseType`.

```csharp
public class Response<T>
{
    public string Status { get; set; }

    public T Payload { get; set; }
}

public class ResponseType<TSchemaType, TRuntimeType>
    : ObjectType<Response<TRuntimeType>>
    where TSchemaType : class, IOutputType
{
    protected override void Configure(
        IObjectTypeDescriptor<Response<TRuntimeType>> descriptor)
    {
        descriptor.Field(f => f.Status);

        descriptor
            .Field(f => f.Payload)
            .Type<TSchemaType>();
    }
}
```

## Naming

If we were to use the above type with two different generic arguments, we would get an error, since both `ResponseType` have the same name.

We can change the name of our generic object type depending on the used generic type.

```csharp
public class ResponseType<T> : ObjectType<Response>
    where T : class, IOutputType
{
    protected override void Configure(
        IObjectTypeDescriptor<Response> descriptor)
    {
        descriptor
            .Name(dependency => dependency.Name + "Response")
            .DependsOn<T>();

        descriptor.Field(f => f.Status);

        descriptor
            .Field(f => f.Payload)
            .Type<T>();
    }
}
```
