---
title: "Mutations"
---

The Mutation type is the entry point for write operations. Unlike query fields, mutation fields are expected to cause side effects: creating, updating, or deleting data. GraphQL executes top-level mutation fields serially, one after another, to guarantee ordering. Child fields of a mutation result are executed in parallel, like any other object type.

**GraphQL schema**

```graphql
type Mutation {
  addBook(input: AddBookInput!): AddBookPayload!
  publishBook(input: PublishBookInput!): PublishBookPayload!
}
```

**Client mutation**

```graphql
mutation {
  addBook(input: { title: "C# in depth" }) {
    book {
      id
      title
    }
  }
}
```

# Defining a Mutation Type

Mark a class with `[MutationType]` and the source generator registers it as part of the Mutation type. Like query types, the class must be `partial`.

<ExampleTabs>
<Implementation>

```csharp
// Types/BookMutations.cs
[MutationType]
public static partial class BookMutations
{
    public static async Task<Book> AddBookAsync(
        string title,
        string author,
        CatalogContext db,
        CancellationToken ct)
    {
        var book = new Book { Title = title, Author = author };
        db.Books.Add(book);
        await db.SaveChangesAsync(ct);
        return book;
    }
}
```

</Implementation>
<Code>

```csharp
// Types/BookMutations.cs
public class BookMutations
{
    public async Task<Book> AddBookAsync(
        string title,
        string author,
        CatalogContext db,
        CancellationToken ct)
    {
        var book = new Book { Title = title, Author = author };
        db.Books.Add(book);
        await db.SaveChangesAsync(ct);
        return book;
    }
}

// Types/BookMutationsType.cs
public class BookMutationsType : ObjectType<BookMutations>
{
    protected override void Configure(
        IObjectTypeDescriptor<BookMutations> descriptor)
    {
        descriptor.Field(f => f.AddBookAsync(default!, default!, default!, default!));
    }
}
```

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddMutationType<BookMutationsType>();
```

</Code>
</ExampleTabs>

# Splitting Across Classes

Like query types, you can annotate multiple classes with `[MutationType]`. The source generator merges them into one Mutation type.

```csharp
// Types/BookMutations.cs
[MutationType]
public static partial class BookMutations
{
    public static async Task<Book> AddBookAsync(
        string title, CatalogContext db, CancellationToken ct)
    {
        var book = new Book { Title = title };
        db.Books.Add(book);
        await db.SaveChangesAsync(ct);
        return book;
    }
}
```

```csharp
// Types/AuthorMutations.cs
[MutationType]
public static partial class AuthorMutations
{
    public static async Task<Author> AddAuthorAsync(
        string name, CatalogContext db, CancellationToken ct)
    {
        var author = new Author { Name = name };
        db.Authors.Add(author);
        await db.SaveChangesAsync(ct);
        return author;
    }
}
```

# Mutation Conventions

In GraphQL, it is best practice for each mutation to accept a single `input` argument and return a payload object. The payload contains the changed data and any domain errors. This pattern keeps the schema evolvable but requires boilerplate.

Hot Chocolate generates the input and payload types for you when mutation conventions are enabled.

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddMutationConventions(applyToAllMutations: true);
```

With conventions enabled, you write the resolver with plain parameters and a return type. Hot Chocolate wraps them in an input type and payload type automatically.

<ExampleTabs>
<Implementation>

```csharp
// Types/UserMutations.cs
[MutationType]
public static partial class UserMutations
{
    public static async Task<User?> UpdateUserNameAsync(
        [ID] Guid userId,
        string username,
        UserService users,
        CancellationToken ct)
        => await users.UpdateNameAsync(userId, username, ct);
}
```

</Implementation>
<Code>

```csharp
// Types/UserMutationsType.cs
public class UserMutationsType : ObjectType<UserMutations>
{
    protected override void Configure(
        IObjectTypeDescriptor<UserMutations> descriptor)
    {
        descriptor
            .Field(f => f.UpdateUserNameAsync(default, default!, default!, default))
            .Argument("userId", a => a.ID());
    }
}
```

</Code>
</ExampleTabs>

This produces the following schema:

```graphql
type Mutation {
  updateUserName(input: UpdateUserNameInput!): UpdateUserNamePayload!
}

input UpdateUserNameInput {
  userId: ID!
  username: String!
}

type UpdateUserNamePayload {
  user: User
}
```

Services (`UserService`, `CancellationToken`) are not included in the generated input type. Only parameters that map to GraphQL arguments appear in the input.

If you prefer to opt in per mutation instead of globally, use `[UseMutationConvention]` on individual methods:

```csharp
[UseMutationConvention]
public static async Task<User?> UpdateUserNameAsync(/* ... */)
```

## Opting Out

To exclude a specific mutation from global conventions:

```csharp
[UseMutationConvention(Disable = true)]
public static async Task<User?> UpdateUserNameAsync(/* ... */)
```

You can also partially opt out by providing your own input or payload type. If your method already accepts a type named `{MutationName}Input` or returns `{MutationName}Payload`, the convention recognizes it and does not generate a replacement.

```csharp
public static UpdateUserNamePayload UpdateUserNameAsync(UpdateUserNameInput input)
{
    // Custom payload and input — conventions leave them as-is
}
```

## Customizing Names

Override the global naming patterns through `MutationConventionOptions`:

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddMutationConventions(
        new MutationConventionOptions
        {
            InputArgumentName = "input",
            InputTypeNamePattern = "{MutationName}Input",
            PayloadTypeNamePattern = "{MutationName}Payload",
            PayloadErrorTypeNamePattern = "{MutationName}Error",
            PayloadErrorsFieldName = "errors",
            ApplyToAllMutations = true
        });
```

Override per mutation with `[UseMutationConvention]`:

```csharp
[UseMutationConvention(
    InputTypeName = "RenameUserInput",
    PayloadTypeName = "RenameUserPayload")]
public static async Task<User?> UpdateUserNameAsync(/* ... */)
```

# Domain Errors

Mutation conventions support typed domain errors on the payload. Annotate a mutation with `[Error]` to declare which exceptions represent domain errors. Hot Chocolate catches those exceptions and maps them to error types on the payload. All other exceptions remain runtime errors.

```csharp
// Types/UserMutations.cs
[MutationType]
public static partial class UserMutations
{
    [Error(typeof(UserNameTakenException))]
    [Error(typeof(InvalidUserNameException))]
    public static async Task<User?> UpdateUserNameAsync(
        [ID] Guid userId,
        string username,
        UserService users,
        CancellationToken ct)
        => await users.UpdateNameAsync(userId, username, ct);
}
```

This produces a payload with an `errors` field:

```graphql
type UpdateUserNamePayload {
  user: User
  errors: [UpdateUserNameError!]
}

interface Error {
  message: String!
}

type UserNameTakenError implements Error {
  message: String!
}

type InvalidUserNameError implements Error {
  message: String!
}

union UpdateUserNameError = UserNameTakenError | InvalidUserNameError
```

Exception class names are rewritten: `UserNameTakenException` becomes `UserNameTakenError` in the schema.

## Controlling Error Shape

There are three ways to map an exception to a schema error:

**Map the exception directly.** Annotate `[Error(typeof(MyException))]`. The exception's `Message` property becomes the error message. This is the quickest approach.

**Map with a factory method.** Create an error class with a `public static CreateErrorFrom(MyException ex)` method. This lets you control the error shape and hide internal details.

```csharp
// Errors/UserNameTakenError.cs
public class UserNameTakenError
{
    private UserNameTakenError(string message) => Message = message;

    public string Message { get; }

    public static UserNameTakenError CreateErrorFrom(UserNameTakenException ex)
        => new($"The username {ex.Username} is already taken.");
}
```

**Map with a constructor.** Give the error class a constructor that accepts the exception.

```csharp
// Errors/UserNameTakenError.cs
public class UserNameTakenError
{
    public UserNameTakenError(UserNameTakenException ex)
        => Message = $"The username {ex.Username} is already taken.";

    public string Message { get; }
}
```

Factory methods can also be instance methods implementing `IPayloadErrorFactory<TError, TException>`, which supports dependency injection. Errors and error factories can be shared across multiple mutations. You can use `AggregateException` to return multiple errors at once.

## Custom Error Interface

The default error interface requires a `message` field. To add an error code or other fields, define your own interface:

<ExampleTabs>
<Implementation>

```csharp
// Types/IUserError.cs
[GraphQLName("UserError")]
public interface IUserError
{
    string Message { get; }
    string Code { get; }
}
```

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddErrorInterfaceType<IUserError>();
```

</Implementation>
<Code>

```csharp
// Types/CustomErrorInterfaceType.cs
public class CustomErrorInterfaceType : InterfaceType
{
    protected override void Configure(IInterfaceTypeDescriptor descriptor)
    {
        descriptor.Name("UserError");
        descriptor.Field("message").Type<NonNullType<StringType>>();
        descriptor.Field("code").Type<NonNullType<StringType>>();
    }
}
```

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddErrorInterfaceType<CustomErrorInterfaceType>();
```

</Code>
</ExampleTabs>

All error types must declare the fields required by the interface. They do not need to implement the C# interface, but they must have matching properties.

# Transactions

When a request contains multiple mutations, Hot Chocolate can wrap them in a transaction. The default implementation uses `System.Transactions.TransactionScope`, which works with ADO.NET providers and Entity Framework.

```csharp
// Program.cs
builder.Services
    .AddGraphQLServer()
    .AddDefaultTransactionScopeHandler();
```

To customize the transaction behavior, implement `ITransactionScopeHandler` and register it:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddTransactionScopeHandler<CustomTransactionScopeHandler>();
```

# Troubleshooting

## Input type not generated

Verify that mutation conventions are enabled (`AddMutationConventions`). Without conventions, Hot Chocolate uses the method parameters directly as field arguments instead of wrapping them in an input type.

## Domain error not appearing on payload

Check that the exception type is annotated with `[Error(typeof(...))]` on the mutation method. Also verify mutation conventions are enabled. Domain errors require the convention's payload rewriting to work.

## Multiple mutations execute out of order

GraphQL guarantees serial execution of top-level mutation fields. If mutations appear to run out of order, verify that you are sending them as multiple fields in a single `mutation` operation, not as separate requests.

# Next Steps

- **Need to read data?** See [Queries](/docs/hotchocolate/v16/defining-a-schema/queries).
- **Need real-time updates?** See [Subscriptions](/docs/hotchocolate/v16/defining-a-schema/subscriptions).
- **Need to understand input types?** See [Input Object Types](/docs/hotchocolate/v16/defining-a-schema/input-object-types).
- **Need to fetch data efficiently?** See [DataLoader](/docs/hotchocolate/v16/resolvers-and-data/dataloader).
