---
title: "Input Object Types"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

We already looked at [arguments](/docs/hotchocolate/defining-a-schema/arguments), which allow us to use simple [scalars](/docs/hotchocolate/defining-a-schema/scalars) like `String` to pass data into a field. GraphQL defines input object types to allow us to use objects as arguments on our fields.

Input object type definitions differ from [object types](/docs/hotchocolate/defining-a-schema/object-types) only in the used keyword and in that their fields can not have arguments.

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

[Learn more about object types](/docs/hotchocolate/defining-a-schema/object-types)

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
