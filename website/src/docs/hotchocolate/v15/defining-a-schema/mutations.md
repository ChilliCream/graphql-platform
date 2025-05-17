---
title: "Mutations"
---

The mutation type in GraphQL is used to mutate/change data. This means that when we are doing mutations, we are intending to cause side-effects in the system.

GraphQL defines mutations as top-level fields on the mutation type. Meaning only the fields on the mutation root type itself are mutations. Everything that is returned from a mutation field represents the changed state of the server.

```sdl
type Mutation {
 addBook(input: AddBookInput!): AddBookPayload!
 publishBook(input: PublishBookInput!): PublishBookPayload!
}
```

Clients can execute one or more mutations through the mutation type.

```graphql
mutation {
  addBook(input: { title: "C# in depth" }) {
    book {
      id
      title
    }
  }
  publishBook(input: { id: 1 }) {
    book {
      publishDate
    }
  }
}
```

Each of these mutations is executed serially one by one whereas their child selection sets are executed possibly in parallel since only top-level mutation fields (those directly under `mutation`) are allowed to cause side-effects in GraphQL.

# Usage

A mutation type can be defined like the following.

<ExampleTabs>
<Implementation>

```csharp
public class Mutation
{
    public async Task<BookAddedPayload> AddBook(Book book)
    {
        // Omitted code for brevity
    }
}
```

```csharp
builder.Services
    .AddGraphQLServer()
    .AddMutationType<Mutation>();
```

</Implementation>
<Code>

```csharp
public class Mutation
{
    public async Task<BookAddedPayload> AddBook(Book book)
    {
        // Omitted code for brevity
    }
}

public class MutationType : ObjectType<Mutation>
{
    protected override void Configure(
        IObjectTypeDescriptor<Mutation> descriptor)
    {
        descriptor.Field(f => f.AddBook(default));
    }
}
```

```csharp
builder.Services
    .AddGraphQLServer()
    .AddMutationType<MutationType>();
```

</Code>
<Schema>

```csharp
public class Mutation
{
    public async Task<BookAddedPayload> AddBook(Book book)
    {
        // Omitted code for brevity
    }
}
```

```csharp
builder.Services
    .AddGraphQLServer()
    .AddDocumentFromString(@"
        type Mutation {
            addBook(input: BookInput): Book
        }

        input BookInput {
            title: String
            author: String
        }

        type Book {
            title: String
            author: String
        }
        ")
    .BindRuntimeType<Mutation>();
```

</Schema>
</ExampleTabs>

> Warning: Only **one** mutation type can be registered using `AddMutationType()`. If we want to split up our mutation type into multiple classes, we can do so using type extensions.
>
> [Learn more about extending types](/docs/hotchocolate/v15/defining-a-schema/extending-types)

A mutation type is just a regular object type, so everything that applies to an object type also applies to the mutation type (this is true for all root types).

[Learn more about object types](/docs/hotchocolate/v15/defining-a-schema/object-types)

# Transactions

With multiple mutations executed serially in one request it can be useful to wrap these in a transaction that we can control.

Hot Chocolate provides for this the `ITransactionScopeHandler` which is used by the operation execution middleware to create transaction scopes for mutation requests.

Hot Chocolate provides a default implementation based on the `System.Transactions.TransactionScope` which works with Microsoft ADO.NET data provider and hence can be used in combination with Entity Framework.

The default transaction scope handler can be added like the following.

```csharp
builder.Services
    .AddGraphQLServer()
    .AddDefaultTransactionScopeHandler();
```

This is how the default implementation looks like:

```csharp
/// <summary>
/// Represents the default mutation transaction scope handler implementation.
/// </summary>
public class DefaultTransactionScopeHandler : ITransactionScopeHandler
{
    /// <summary>
    /// Creates a new transaction scope for the current
    /// request represented by the <see cref="IRequestContext"/>.
    /// </summary>
    /// <param name="context">
    /// The GraphQL request context.
    /// </param>
    /// <returns>
    /// Returns a new <see cref="ITransactionScope"/>.
    /// </returns>
    public virtual ITransactionScope Create(IRequestContext context)
    {
        return new DefaultTransactionScope(
            context,
            new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted
            }));
    }
}
```

If we implement a custom transaction scope handler or if we choose to extend upon the default transaction scope handler, we can add it like the following.

```csharp
builder.Services
    .AddGraphQLServer()
    .AddTransactionScopeHandler<CustomTransactionScopeHandler>();
```

# Conventions

In GraphQL, it is best practice to have a single argument on mutations called `input`, and each mutation should return a payload object.
The payload object allows to read the changes of the mutation or to access the domain errors caused by a mutation.

```sdl
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

Following this pattern helps to keep the schema evolvable but requires a lot of boilerplate code to realize.

## Input and Payload

HotChocolate has built-in conventions for mutations to minimize boilerplate code.

The HotChocolate mutation conventions are opt-in and can be enabled like the following:

```csharp
service
    .AddGraphQLServer()
    .AddMutationConventions()
    ...
```

With the mutation conventions enabled, we can define the described mutation pattern with minimal code by just annotating a field with `UseMutationConvention`.

<ExampleTabs>
<Implementation>

```csharp
public class Mutation
{
    [UseMutationConvention]
    public User? UpdateUserNameAsync([ID] Guid userId, string username)
    {
    //...
    }
}
```

</Implementation>
<Code>

```csharp
public class Mutation
{
    public User UpdateUserNameAsync(
        Guid userId,
        string username)
        => ...
}

public class MutationType : ObjectType<Mutation>
{
    protected override void Configure(
        IObjectTypeDescriptor<Mutation> descriptor)
    {
        descriptor
            .Field(f => f.UpdateUserNameAsync(default, default))
            .Argument("userId", a => a.ID())
            .UseMutationConvention();
    }
}
```

</Code>
<Schema>

```sdl
type Mutation {
  updateUserName(userId: ID!, username: String!) : User @useMutationConvention
}
```

</Schema>
</ExampleTabs>

We also can configure the mutation conventions to be applied to all mutations by default.

```csharp
service
    .AddGraphQLServer()
    .AddMutationConventions(applyToAllMutations: true)
    ...
```

In the case that the conventions are applied by default we no longer need any annotation.

<ExampleTabs>
<Implementation>

```csharp
public class Mutation
{
    public User? UpdateUserNameAsync([ID] Guid userId, string username)
    {
    //...
    }
}
```

</Implementation>
<Code>

```csharp
public class Mutation
{
    public User UpdateUserNameAsync(
        Guid userId,
        string username)
        => ...
}

public class MutationType : ObjectType<Mutation>
{
    protected override void Configure(
        IObjectTypeDescriptor<Mutation> descriptor)
    {
        descriptor
            .Field(f => f.UpdateUserNameAsync(default, default))
            .Argument("userId", a => a.ID());
    }
}
```

</Code>
<Schema>

```sdl
type Mutation {
  updateUserName(userId: ID!, username: String!) : User
}
```

</Schema>
</ExampleTabs>

## Errors

The mutation conventions also allow you to create mutations that follow the error
[stage 6a Pattern Marc-Andre Giroux laid out](https://xuorig.medium.com/a-guide-to-graphql-errors-bb9ba9f15f85) with minimal effort.

The basic concept here is to keep the resolver clean of any error handling code and use exceptions to signal an error state. The field will simply expose which exceptions are domain errors that shall be exposed to the schema. All other exceptions will still cause runtime errors.

<ExampleTabs>
<Implementation>

```csharp
public class Mutation
{
    [Error(typeof(UserNameTakenException))]
    [Error(typeof(InvalidUserNameException))]
    public User? UpdateUserNameAsync([ID] Guid userId, string username)
    {
        //...
    }
}
```

</Implementation>
<Code>

```csharp
public class Mutation
{
    public User? UpdateUserNameAsync(Guid userId, string username)
    {
       // ...
    }
}

public class MutationType : ObjectType<Mutation>
{
    protected override void Configure(
        IObjectTypeDescriptor<Mutation> descriptor)
    {
        descriptor
          .Field(f => f.UpdateUserNameAsync(default))
          .Error<UserNameTakenException>()
          .Error<InvalidUserNameException>();
    }
}
```

</Code>
<Schema>

```sdl
type Mutation {
  updateUserName(userId: ID!, username: String!): User
}
```

```csharp
public class Mutation
{
    [Error(typeof(UserNameTakenException))]
    [Error(typeof(InvalidUserNameException))]
    public User? UpdateUserNameAsync(Guid userId, string username)
    {
        //...
    }
}
```

</Schema>
</ExampleTabs>

The HotChocolate schema is automatically rewritten, and an error middleware will catch all the exceptions that represent domain errors and rewrite them into the correct error object.

The configuration above emits the following schema:

```sdl
type Mutation {
  updateUserName(input: UpdateUserNameInput!): UpdateUserNamePayload!
}

input UpdateUserNameInput {
  userId: ID!
  username: String!
}

type UpdateUserNamePayload {
  user: User
  errors: [UpdateUserNameError!]
}

type User {
  username: String
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

There are three ways to map an exception to a user error.

1. Map the exception directly
2. Map with a factory method (`CreateErrorFrom`)
3. Map with a constructor

> Note: You can use AggregateExceptions to return multiple errors at once.

### Map exceptions directly

The quickest way to define a user error, is to map the exception directly into the graph. You can just annotate the exception directly on the resolver.
If the exception is thrown and is caught in the error middleware, it will be rewritten into an user error that is exposed on the mutation payload.

> The name of the exception will be rewritten. `Exception` is replaced with `Error` to follow the common GraphQL naming conventions.

<ExampleTabs>
<Implementation>

```csharp
public class UserNameTakenException : Exception
{
    public UserNameTakenException(string username)
        : base($"The username {username} is already taken.")
    {
    }
}

public class Mutation
{
    [Error(typeof(UserNameTakenException))]
    public User? UpdateUserNameAsync([ID] Guid userId, string username)
    {
        //...
    }
}
```

</Implementation>
<Code>

```csharp
public class UserNameTakenException : Exception
{
    public UserNameTakenException(string username)
        : base($"The username {username} is already taken.")
    {
    }
}

public class Mutation
{
    public User? UpdateUserNameAsync(Guid userId, string username)
    {
       // ...
    }
}

public class MutationType : ObjectType<Mutation>
{
    protected override void Configure(
        IObjectTypeDescriptor<Mutation> descriptor)
    {
        descriptor
          .Field(f => f.UpdateUserNameAsync(default))
          .Error<UserNameTakenException>();
    }
}
```

</Code>
<Schema>

```csharp
public class UserNameTakenException : Exception
{
    public UserNameTakenException(string username)
        : base($"The username {username} is already taken.")
    {
    }
}
```

```sdl
type Mutation {
  updateUserName(userId: ID!, username: String!): User
}
```

```csharp
public class Mutation
{
    [Error(typeof(UserNameTakenException))]
    public User? UpdateUserNameAsync(Guid userId, string username)
    {
        //...
    }
}
```

</Schema>
</ExampleTabs>

### Map with a factory method

Often there is a need to control the error shape and ensure that not too many details are exposed. In these cases, we can use a custom error class representing the user error in our schema.

The error instance and the translation of the exception can be done by an error factory. The error factory method receives an exception and returns the error object.

Add a `public` `static` method called `CreateErrorFrom` that takes an exception and returns the error object.

<ExampleTabs>
<Implementation>

```csharp
public class UserNameTakenError
{
    private UserNameTakenError(string username)
    {
        Message = $"The username {username} is already taken.";
    }

    public static UserNameTakenError CreateErrorFrom(UserNameTakenException ex)
    {
        return new UserNameTakenError(ex.Username);
    }

    public static UserNameTakenError CreateErrorFrom(OtherException ex)
    {
        return new UserNameTakenError(ex.Username);
    }

    public string Message { get; }
}

public class UserNameTakenException : Exception
{
    public UserNameTakenException(string username)
    {
        Username = username;
    }

    public string Username { get; }
}

public class Mutation
{
    [Error(typeof(UserNameTakenError))]
    public User? UpdateUserNameAsync([ID] Guid userId, string username)
    {
        //...
    }
}
```

</Implementation>
<Code>

```csharp
public class UserNameTakenError
{
    private UserNameTakenError(string username)
    {
        Message = $"The username {username} is already taken.";
    }

    public static UserNameTakenError CreateErrorFrom(UserNameTakenException ex)
    {
        return new UserNameTakenError(ex.Username);
    }

    public static UserNameTakenError CreateErrorFrom(OtherException ex)
    {
        return new UserNameTakenError(ex.Username);
    }

    public string Message { get; }
}

public class UserNameTakenException : Exception
{
    public UserNameTakenException(string username)
    {
        Username = username;
    }

    public string Username { get; }
}

public class Mutation
{
    public User? UpdateUserNameAsync(Guid userId, string username)
    {
       // ...
    }
}

public class MutationType : ObjectType<Mutation>
{
    protected override void Configure(
        IObjectTypeDescriptor<Mutation> descriptor)
    {
        descriptor
          .Field(f => f.UpdateUserNameAsync(default))
          .Error<UserNameTakenError>();
    }
}
```

</Code>
<Schema>

```csharp
public class UserNameTakenError
{
    private UserNameTakenError(string username)
    {
        Message = $"The username {username} is already taken.";
    }

    public static UserNameTakenError CreateErrorFrom(UserNameTakenException ex)
    {
        return new UserNameTakenError(ex.Username);
    }

    public static UserNameTakenError CreateErrorFrom(OtherException ex)
    {
        return new UserNameTakenError(ex.Username);
    }

    public string Message { get; }
}
```

```sdl
type Mutation {
  updateUserName(userId: ID!, username: String!): User
}
```

```csharp
public class Mutation
{
    [Error(typeof(UserNameTakenError))]
    public User? UpdateUserNameAsync(Guid userId, string username)
    {
        //...
    }
}
```

</Schema>
</ExampleTabs>

Error factories can also be located in a dedicated class.

```csharp
public static class CreateUserErrorFactory
{
    public static UserNameTakenError CreateErrorFrom(DomainExceptionA ex)
    {
        return new UserNameTakenError();
    }

    public static UserNameTakenError CreateErrorFrom(DomainExceptionB ex)
    {
        return new UserNameTakenError();
    }
}

public class Mutation
{
    [Error(typeof(CreateUserErrorFactory))]
    public CreateUserPayload CreateUser(CreateUserInput input)
    {
       // ...
    }
}
```

Further the error factory methods do not have to be static.
You can also use the `IPayloadErrorFactory<TError, TException>` interface, to define instance error factory methods. This also enables you to use dependency injection with your factory class.

```csharp
public class CreateUserErrorFactory
    : IPayloadErrorFactory<MyCustomError, DomainExceptionA>
    , IPayloadErrorFactory<MyCustomError, DomainExceptionB>
{
    public MyCustomError CreateErrorFrom(DomainExceptionA ex)
    {
        return new MyCustomErrorA();
    }

    public MyCustomError CreateErrorFrom(DomainExceptionB ex)
    {
        return new MyCustomErrorB();
    }
}

public class Mutation
{
    [Error(typeof(CreateUserErrorFactory))]
    public CreateUserPayload CreateUser(CreateUserInput input)
    {
       // ...
    }
}
```

### Map with a constructor

Lastly, we can also use the constructor of an error class to consume an exception. Essentially the constructor in this case represents the factory that we described earlier.

<ExampleTabs>
<Implementation>

```csharp
public class UserNameTakenError
{
    private UserNameTakenError(UserNameTakenException ex)
    {
        Message = $"The username {ex.Username} is already taken.";
    }

    public string Message { get; }
}

public class UserNameTakenException : Exception
{
    public UserNameTakenException(string username)
    {
        Username = username;
    }

    public string Username { get; }
}

public class Mutation
{
    [Error(typeof(UserNameTakenError))]
    public User? UpdateUserNameAsync([ID] Guid userId, string username)
    {
        //...
    }
}
```

</Implementation>
<Code>

```csharp
public class UserNameTakenError
{
    private UserNameTakenError(UserNameTakenException ex)
    {
        Message = $"The username {ex.Username} is already taken.";
    }

    public string Message { get; }
}

public class UserNameTakenException : Exception
{
    public UserNameTakenException(string username)
    {
        Username = username;
    }

    public string Username { get; }
}

public class Mutation
{
    public User? UpdateUserNameAsync(Guid userId, string username)
    {
       // ...
    }
}

public class MutationType : ObjectType<Mutation>
{
    protected override void Configure(
        IObjectTypeDescriptor<Mutation> descriptor)
    {
        descriptor
          .Field(f => f.UpdateUserNameAsync(default))
          .Error<UserNameTakenError>();
    }
}
```

</Code>
<Schema>

```csharp
public class UserNameTakenError
{
    private UserNameTakenError(UserNameTakenException ex)
    {
        Message = $"The username {ex.Username} is already taken.";
    }

    public string Message { get; }
}

public class UserNameTakenException : Exception
{
    public UserNameTakenException(string username)
    {
        Username = username;
    }

    public string Username { get; }
}
```

```sdl
type Mutation {
  updateUserName(userId: ID!, username: String!): User
}
```

```csharp
public class Mutation
{
    [Error(typeof(UserNameTakenError))]
    public User? UpdateUserNameAsync(Guid userId, string username)
    {
        //...
    }
}
```

</Schema>
</ExampleTabs>

> Note: errors and error factories can be shared between multiple mutations.

## Customization

While the mutation conventions strictly follow the outlined mutation and error patterns they still can be customized.

### Naming

The naming patterns for inputs, payloads and errors can be adjusted globally as well as on a per mutation basis.

In order to change the global mutation naming patterns you can pass in the `MutationConventionOptions` into the `AddMutationConventions` configuration method.

```csharp
builder.Services
    .AddGraphQL()
    .AddMutationConventions(
        new MutationConventionOptions
        {
            InputArgumentName = "input",
            InputTypeNamePattern = "{MutationName}Input",
            PayloadTypeNamePattern = "{MutationName}Payload",
            PayloadErrorTypeNamePattern = "{MutationName}Error",
            PayloadErrorsFieldName = "errors",
            ApplyToAllMutations = true
        })
    ...
```

To override the global mutation settings on a mutation use the `UseMutationConvention` annotation.

```csharp
[UseMutationConvention(
    InputTypeName = "FooInput",
    InputArgumentName = "foo",
    PayloadTypeName = "FooPayload",
    PayloadFieldName = "bar")]
public User? UpdateUserNameAsync(Guid userId, string username)
{
    //...
}
```

### Opting Out

Often we want to infer everything and only opt-out for exceptional cases, and the mutation convention allows us to do that in an effortless way.

The first way to opt out of the global conventions is to use the `UseMutationConvention` annotation. With `UseMutationConvention` we can tell the type system initialization to disable the convention on certain mutations.

```csharp
[UseMutationConvention(Disable = true)]
public User? UpdateUserNameAsync(Guid userId, string username)
{
    //...
}
```

In many cases, we do not want to entirely opt-out but rather override the global settings since we wish for a more complex payload or input. We can simply add our own payload or input type in these cases, and the schema initialization will recognize that. Essentially if we follow the naming pattern for either input or payload, the initialization will not rewrite that part that already follows the global convention.

```csharp
public UpdateUserNamePayload UpdateUserNameAsync(UpdateUserNameInput input)
{
    //...
}
```

You can also partially opt-out:

```csharp
public User UpdateUserNameAsync(UpdateUserNameInput input)
{
    //...
}
```

### Custom error interface

Lastly, we can customize the error interface we want to use with our mutation convention. The error interface is shared across all error types that the schema defines and provides the minimum shape that all errors have to fulfill.

By default, this error interface type is called `Error` and defines a non-nullable field `message`.

```sdl
interface Error {
  message: String!
}
```

Often we also want to provide an error code so that the GUI components can more easily implement error handling logic. In such a case, we could provide our own error interface.

> Note: All your error types have to implement the contract that the interface declares! Your errors/exceptions do not have to implement the common interface, but they have to declare all the interface's members.

<ExampleTabs>
<Implementation>

```csharp
[GraphQLName("UserError")]
public interface IUserError
{
  string Message { get; }

  string Code { get; }
}
```

```csharp
builder.Services
    .AddGraphQLServer()
    // ... Omitted code for brevity
    .AddErrorInterfaceType<IUserError>();
```

</Implementation>
<Code>

```csharp
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
builder.Services
    .AddGraphQLServer()
    // ... Omitted code for brevity
    .AddErrorInterfaceType<CustomErrorInterfaceType>();
```

</Code>
<Schema>

```sdl
interface UserError @errorInterface {
  message: String!
  code: String!
}
```

</Schema>
</ExampleTabs>

```sdl
interface UserError {
  message: String!
  code: String!
}
```
