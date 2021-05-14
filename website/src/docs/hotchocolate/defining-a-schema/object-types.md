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

<!-- todo: every sentence starts with we.... urrrgh -->

We create a new class inheriting from `ObjectType<T>` to map our POCO `Author` to an object type.

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

We will learn how to use the `descriptor` in the following chapters.

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

# Fields

TODO

<!-- todo: maybe example tabs here -->

# Binding behavior

In the Annotation-based approach all public properties and methods are implicitly mapped to fields of the schema object type.

In the Code-first approach we have a little more control over this behavior. By default all public properties of our POCO are mapped to fields of the schema object type. This behavior is called implicit binding. There is also an explicit binding behavior, where we have to opt-in properties we want to include.

We can configure our preferred binding behavior globally like the following:

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

In the Annotation-based approach we can ignore fields using the `[GraphQLIgnore]` attribute.

```csharp
public class Book
{
    [GraphQLIgnore]
    public string Title { get; set; }

    public Author Author { get; set; }
}
```

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
    [GraphQLName("FullName")]
    public string Name { get; set; }
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

Per default the name of the object type in the schema is either the class name, if we are inheriting from `ObjectType`, or the name of the POCO, if we are inheriting from `ObjectType<T>`.

We can override these defaults using the `Name` method on the `IObjectTypeDescriptor` / `IObjectFieldDescriptor`.

```csharp
public class AuthorType : ObjectType<Author>
{
    protected override void Configure(IObjectTypeDescriptor<Author> descriptor)
    {
        descriptor.Name("BookAuthor");

        descriptor
            .Field(f => f.Name)
            .Name("FullName");
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

Field names are automatically converted into camelCase by Hot Chocolate.

# Explicit types

Hot Chocolate will most of the type correctly infer the schema types of our fields. Sometimes we might have to be explicit about it though, for example when we are working with custom scalars.

We can define explicit schema types like the following.

<ExampleTabs>
<ExampleTabs.Annotation>

Per default the name of the class is the name of the object type in the schema and the names of the properties are the names of the fields of that object type.

We can override these defaults using the `[GraphQLName]` attribute.

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

TODO

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
    protected override void Configure(IObjectTypeDescriptor<Response> descriptor)
    {
        descriptor.Field(f => f.Status);

        descriptor
            .Field(f => f.Payload)
            .Type<T>();
    }
}

// todo: this is not code-first...
public class Query
{
    [GraphQLType(typeof(ResponseType<IntType>))]
    public Response GetResponse()
    {
        return new Response
        {
            Status = "OK",
            Payload = 123
        };
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

## Naming

If we were to add another field of the type `ResponseType<DateTimeType>`, we would get an error, since both `ResponseType` have the same name.

We can change the name of our generic object type depending on the used generic type.

```csharp
public class ResponseType<T> : ObjectType<Response>
    where T : class, IOutputType
{
    protected override void Configure(IObjectTypeDescriptor<Response> descriptor)
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
