---
title: "Input Object Types"
---

We already looked at [arguments](/docs/hotchocolate/v11/defining-a-schema/arguments), which allow us to use simple [scalars](/docs/hotchocolate/v11/defining-a-schema/scalars) like `String` to pass data into a field. GraphQL defines input object types to allow us to use objects as arguments on our fields.

Input object type definitions differ from [object types](/docs/hotchocolate/v11/defining-a-schema/object-types) only in the used keyword and in that their fields can not have arguments.

```sdl
input BookInput {
  title: String
  author: String
}
```

# Usage

Input object types can be defined like the following.

<ExampleTabs>
<Implementation>

```csharp
public class BookInput
{
    public string Title { get; set; }

    public string Author { get; set; }
}

public class Mutation
{
    public async Task<Book> AddBook(BookInput input)
    {
        // Omitted code for brevity
    }
}
```

> Note: If a class is used as an argument to a resolver and it does not end in `Input`, Hot Chocolate (by default) will append `Input` to the type name in the resulting schema.

We can also use a class both as an output- and an input-type.

```csharp
public class Book
{
    public string Title { get; set; }

    public string Author { get; set; }
}

public class Mutation
{
    public async Task<Book> AddBook(Book input)
    {
        // Omitted code for brevity
    }
}
```

This will produce the following schema.

```sdl
type Book {
  title: String
  author: String
}

input BookInput {
  title: String
  author: String
}

type Mutation {
  addBook(input: BookInput): Book
}
```

> Note: While it is possible, it is not encouraged, as it complicates future extensions of either type.

</Implementation>
<Code>

```csharp
public class BookInput
{
    public string Title { get; set; }

    public string Author { get; set; }
}

public class BookInputType : InputObjectType<BookInput>
{
    protected override void Configure(
        IInputObjectTypeDescriptor<BookInput> descriptor)
    {
        // Omitted code for brevity
    }
}

public class MutationType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Mutation);

        descriptor
            .Field("addBook")
            .Argument("input", a => a.Type<BookInputType>())
            .Resolve(context =>
            {
                var input = context.ArgumentValue<BookInput>("input");

                // Omitted code for brevity
            });
    }
}
```

The `IInputTypeDescriptor` is really similar to the `IObjectTypeDescriptor` and provides almost the same capabilities.

[Learn more about object types](/docs/hotchocolate/v11/defining-a-schema/object-types)

</Code>
<Schema>

```csharp
public class BookInput
{
    public string Title { get; set; }

    public string Author { get; set; }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddDocumentFromString(@"
                input BookInput {
                  title: String
                  author: String
                }

                type Mutation {
                  addBook(input: BookInput): Book
                }
            ")
            .BindComplexType<BookInput>()
            .AddResolver( "Mutation", "addBook", (context) =>
            {
                var input = context.ArgumentValue<BookInput>("input");

                // Omitted code for brevity
            });
    }
}
```

> Warning: Object types nested inside of an input object type need to also be declared as input object types.

</Schema>
</ExampleTabs>

## Immutable types

If we want our input type classes to be immutable, or we are using [nullable reference types](https://docs.microsoft.com/dotnet/csharp/nullable-references), we can provide a non-empty constructor and Hot Chocolate will instead use that when instantiating the input. Just note that

1. The type of the argument must exactly match the property's type
2. The name of the argument must match the property name (bar a lowercase first letter)
3. No setters will be called, so you need to provide arguments for all the properties.

Hot Chocolate validates any custom input constructor at schema build time, so we don't need to worry about breaking things during refactoring!

```csharp
public class BookInput
{
    // No need for the setters now
    public string Title { get; }
    public string Author { get; }

    public BookingInput(string title, string author)
    {
        Title = title;
        Author = author;
    }
}
```

We can also use record types, if we're on C# 9.0+. The equivalent to the above would be:

```csharp
public record BookingInput(string Title, string Author);
```
