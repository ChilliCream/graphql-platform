---
title: "Input Object Types"
---

We already looked at [arguments](/docs/hotchocolate/v12/defining-a-schema/arguments), which allow us to use simple [scalars](/docs/hotchocolate/v12/defining-a-schema/scalars) like `String` to pass data into a field. GraphQL defines input object types to allow us to use objects as arguments on our fields.

Input object type definitions differ from [object types](/docs/hotchocolate/v12/defining-a-schema/object-types) only in the used keyword and in that their fields can not have arguments.

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

[Learn more about object types](/docs/hotchocolate/v12/defining-a-schema/object-types)

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
            .BindRuntimeType<BookInput>()
            .AddResolver( "Mutation", "addBook", (context) =>
            {
                var input = context.ArgumentValue<BookInput>("input");

                // Omitted code for brevity
            });
    }
}
```

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

## Optional Properties

If we want our input type classes to contain optional properties, we can use the `Optional<T>` type or mark the properties of the class as `nullable`. It is important to also define a default value for any non-nullable property that is using the `Optional<T>` type by adding the `[DefaultValue]` attribute, otherwise the field will still be required when defining the input.

```csharp
public class BookInput
{
    [DefaultValue("")]
    public Optional<string> Title { get; set; }
    public string Author { get; set; }

    public BookInput(string title, string author)
    {
        Title = title;
        Author = author;
    }
}

```

Also with record types, the equivalent of the above would be:

```csharp
public record BookInput([property:DefaultValue("")]Optional<string> Title, string Author);

```

## `Oneof` Input Objects

`Oneof` Input Objects are a special variant of Input Objects where the type system asserts that exactly one of the fields must be set and non-null, all others being omitted. This is represented in introspection with the \_\_Type.oneField: Boolean field, and in SDL via the @oneOf directive on the input object.

> Warning: `Oneof` Input Objects is currently a draft feature to the GraphQL spec. <https://github.com/graphql/graphql-spec/pull/825>

<Video videoId="tztXm15grU0" />

This introduces a form of input polymorphism to GraphQL. For example, the following PetInput input object lets you choose between a number of potential input types:

```sdl
input PetInput @oneOf {
  cat: CatInput
  dog: DogInput
  fish: FishInput
}

input CatInput { name: String!, numberOfLives: Int }
input DogInput { name: String!, wagsTail: Boolean }
input FishInput { name: String!, bodyLengthInMm: Int }

type Mutation {
  addPet(pet: PetInput!): Pet
}
```

Since the `Oneof` Input Objects RFC is not yet in the draft stage it is still an opt-in feature. In order to activate it set the schema options to enable it.

```csharp
builder.Services
    .AddGraphQLServer()
    ...
    .ModifyOptions(o => o.EnableOneOf = true);
```

Once activate you can create `Oneof` Input Objects like the following:

<ExampleTabs>
<Implementation>

```csharp
[OneOf]
public class PetInput
{
    public Dog? Dog { get; set; }

    public Cat? Cat { get; set; }
}

public interface IPet
{
    string Name { get; }
}

public class Dog : IPet
{
    public string Name { get; set; }
}

public class Cat : IPet
{
    public string Name { get; set; }
}

public class Mutation
{
    public Task<IPet> CreatePetAsync(PetInput input)
    {
        // Omitted code for brevity
    }
}
```

This will produce the following schema.

```sdl
input PetInput @oneOf {
  dog: DogInput
  cat: CatInput
}

input DogInput {
  name: String!
}

input CatInput {
  name: String!
}

interface Pet {
  name: String!
}

type Dog implements Pet {
  name: String!
}

type Cat implements Pet {
  name: String!
}

type Mutation {
  createPet(input: PetInput): Pet
}
```

</Implementation>
<Code>

```csharp
public class PetInput
{
    public Dog? Dog { get; set; }

    public Cat? Cat { get; set; }
}

public class Dog
{
    public string Name { get; set; }
}

public class Cat
{
    public string Name { get; set; }
}

public class PetType : InterfaceType
{
    protected override void Configure(
        IInterfaceTypeDescriptor descriptor)
    {
        descriptor
            .Name("Pet")
            .Field("name")
            .Type("String!");
    }
}

public class PetInputType : InputObjectType<PetInput>
{
    protected override void Configure(
        IInputObjectTypeDescriptor<PetInput> descriptor)
    {
        descriptor.OneOf();
    }
}

public class MutationType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Mutation);

        descriptor
            .Field("createPet")
            .Argument("input", a => a.Type<PetInputType>())
            .Resolve(context =>
            {
                var input = context.ArgumentValue<PetInput>("input");

                // Omitted code for brevity
            });
    }
}
```

This will produce the following schema.

```sdl
input PetInput @oneOf {
  dog: DogInput
  cat: CatInput
}

input DogInput {
  name: String!
}

input CatInput {
  name: String!
}

interface Pet {
  name: String!
}

type Dog implements Pet {
  name: String!
}

type Cat implements Pet {
  name: String!
}

type Mutation {
  createPet(input: PetInput): Pet
}
```

</Code>
<Schema>

```csharp
public class PetInput
{
    public Dog? Dog { get; set; }

    public Cat? Cat { get; set; }
}

public interface IPet
{
    string Name { get; }
}

public class Dog : IPet
{
    public string Name { get; set; }
}

public class Cat : IPet
{
    public string Name { get; set; }
}

public class Mutation
{
    public Task<IPet> CreatePetAsync(PetInput input)
    {
        // Omitted code for brevity
    }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddDocumentFromString(@"
                input PetInput @oneOf {
                    dog: DogInput
                    cat: CatInput
                }

                input DogInput {
                    name: String!
                }

                input CatInput {
                    name: String!
                }

                interface Pet {
                    name: String!
                }

                type Dog implements Pet {
                    name: String!
                }

                type Cat implements Pet {
                    name: String!
                }

                type Mutation {
                    createPet(input: PetInput): Pet
                }
            ")
            .BindRuntimeType<Mutation>()
            .BindRuntimeType<Pet>()
            .BindRuntimeType<Dog>()
            .ModifyOptions(o => o.EnableOneOf = true);
    }
}
```

</Schema>
</ExampleTabs>
