---
title: "Input Object Types"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

In GraphQL we distinguish between input- and output-types. We already learned about [object types](/docs/hotchocolate/defining-a-schema/object-types) which are the most prominent output-type and let us consume data. Further, we used simple [scalars](/docs/hotchocolate/defining-a-schema/scalars) like `String` to pass data into a field as an argument. GraphQL defines input object types in order to define complex structures of raw data that can be used as input data.

Input object type definitions differ from object types only in the used keyword and in that their fields can not have arguments.

```sdl
input BookInput {
  title: String
  author: String
}
```

# Usage

Input object types can be defined like the following.

<ExampleTabs>
<ExampleTabs.Annotation>

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

If you would like your input type classes to be immutable, or you are using [nullable reference types](https://docs.microsoft.com/en-us/dotnet/csharp/nullable-references), you can add a constructor and Hot Chocolate will use it under the hood. Just note that, (1) the type of the argument must exactly match the property's type, and, (2) the name of the argument must match the property name (bar a lowercase first letter).

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

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class BookInput
{
    public string Title { get; set; }

    public string Author { get; set; }
}

public class BookInputType : InputObjectType<BookInput>
{
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

</ExampleTabs.Code>
<ExampleTabs.Schema>

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

</ExampleTabs.Schema>
</ExampleTabs>

⚠ Object types nested inside of an input object type need to also be declared as input types.
