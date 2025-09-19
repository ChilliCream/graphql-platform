---
title: "Object Types"
---

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

# Definition

Object types can be defined like the following.

<ExampleTabs>
<Implementation>

In the implementation-first approach we are essentially just creating regular C# classes.

```csharp
public class Author
{
    public string Name { get; set; }
}
```

</Implementation>
<Code>

In the code-first approach we create a new class inheriting from `ObjectType<T>` to map our POCO `Author` to an object type.

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

The `IObjectTypeDescriptor` gives us the ability to configure the object type. We will cover how to use it in the following chapters.

Since there could be multiple types inheriting from `ObjectType<Author>`, but differing in their name and fields, it is not certain which of these types should be used when we return an `Author` CLR type from one of our resolvers.

**Therefore it's important to note that code-first object types are not automatically inferred. They need to be explicitly specified or registered.**

We can either [explicitly specify the type on a per-resolver basis](#explicit-types) or we can register the type once globally:

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddType<AuthorType>();
    }
}
```

With this configuration every `Author` CLR type we return from our resolvers would be assumed to be an `AuthorType`.

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

</Code>
<Schema>

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
            .BindRuntimeType<Author>();
    }
}
```

</Schema>
</ExampleTabs>

# Binding behavior

In the implementation-first approach all public properties and methods are implicitly mapped to fields on the schema object type. The same is true for `T` of `ObjectType<T>` when using the code-first approach.

In the code-first approach we can also enable explicit binding, where we have to opt-in properties and methods we want to include instead of them being implicitly included.

<!-- todo: this should not be covered in each type documentation, rather once in a server configuration section -->

We can configure our preferred binding behavior globally like the following.

```csharp
services
    .AddGraphQLServer()
    .ModifyOptions(options =>
    {
        options.DefaultBindingBehavior = BindingBehavior.Explicit;
    });
```

> Warning: This changes the binding behavior for all types, not only object types.

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
<Implementation>

In the implementation-first approach we can ignore fields using the `[GraphQLIgnore]` attribute.

```csharp
public class Book
{
    [GraphQLIgnore]
    public string Title { get; set; }

    public Author Author { get; set; }
}
```

</Implementation>
<Code>

In the code-first approach we can ignore fields of our POCO using the `Ignore` method on the `IObjectTypeDescriptor`. This is only necessary, if the binding behavior of the object type is implicit.

```csharp
public class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor.Ignore(f => f.Title);
    }
}
```

</Code>
<Schema>

We do not have to ignore fields in the schema-first approach.

</Schema>
</ExampleTabs>

## Including fields

In the code-first approach we can explicitly include properties of our POCO using the `Field` method on the `IObjectTypeDescriptor`. This is only necessary, if the binding behavior of the object type is explicit.

```csharp
public class BookType : ObjectType<Book>
{
    protected override void Configure(IObjectTypeDescriptor<Book> descriptor)
    {
        descriptor.BindFieldsExplicitly();

        descriptor.Field(f => f.Title);
    }
}
```

# Naming

Unless specified explicitly, Hot Chocolate automatically infers the names of object types and their fields. Per default the name of the class becomes the name of the object type. When using `ObjectType<T>` in code-first, the name of `T` is chosen as the name for the object type. The names of methods and properties on the respective class are chosen as names of the fields of the object type.

The following conventions are applied when transforming C# method and property names into SDL types and fields:

- **Get prefixes are removed:** The get operation is implied and therefore redundant information.
- **Async postfixes are removed:** The `Async` is an implementation detail and therefore not relevant to the schema.
- **The first letter is lowercased:** This is not part of the specification, but a widely agreed upon standard in the GraphQL world.

If we need to we can override these inferred names.

<ExampleTabs>
<Implementation>

The `[GraphQLName]` attribute allows us to specify an explicit name.

```csharp
[GraphQLName("BookAuthor")]
public class Author
{
    [GraphQLName("fullName")]
    public string Name { get; set; }
}
```

</Implementation>
<Code>

The `Name` method on the `IObjectTypeDescriptor` / `IObjectFieldDescriptor` allows us to specify an explicit name.

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

</Code>
<Schema>

Simply change the names in the schema.

</Schema>
</ExampleTabs>

This would produce the following `BookAuthor` schema object type:

```sdl
type BookAuthor {
  fullName: String
}
```

If only one of our clients requires specific names, it is better to use [aliases](https://graphql.org/learn/queries/#aliases) in this client's operations than changing the entire schema.

```graphql
{
  MyUser: user {
    Username: name
  }
}
```

# Explicit types

Hot Chocolate will, most of the time, correctly infer the schema types of our fields. Sometimes we might have to be explicit about it though. For example when we are working with custom scalars or code-first types in general.

<ExampleTabs>
<Implementation>

In the implementation-first approach we can use the `[GraphQLType]` attribute.

```csharp
public class Author
{
    [GraphQLType(typeof(StringType))]
    public string Name { get; set; }
}
```

</Implementation>
<Code>

In the code-first approach we can use the `Type<T>` method on the `IObjectFieldDescriptor`.

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

</Code>
<Schema>

Simply change the field type in the schema.

</Schema>
</ExampleTabs>

# Additional fields

We can add additional (dynamic) fields to our schema types, without adding new properties to our backing class.

<ExampleTabs>
<Implementation>

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

</Implementation>
<Code>

In the code-first approach we can use the `Resolve` method on the `IObjectFieldDescriptor`.

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

</Code>
<Schema>

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
            .BindRuntimeType<Author>()
            .AddResolver("Author", "additionalField", (context) =>
            {
                // Omitted code for brevity
            });
    }
}
```

</Schema>
</ExampleTabs>

What we have just created is a resolver. Hot Chocolate automatically creates resolvers for our properties, but we can also define them ourselves.

[Learn more about resolvers](/docs/hotchocolate/v12/fetching-data/resolvers)

# Generics

> Note: Read about [interfaces](/docs/hotchocolate/v12/defining-a-schema/interfaces) and [unions](/docs/hotchocolate/v12/defining-a-schema/unions) before resorting to generic object types.

In the code-first approach we can define generic object types.

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
