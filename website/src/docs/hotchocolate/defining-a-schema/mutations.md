---
title: "Mutations"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

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
<ExampleTabs.Annotation>

```csharp
public class Mutation
{
    public async Task<BookAddedPayload> AddBook(Book book)
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
            .AddMutationType<Mutation>();
    }

    // Omitted code for brevity
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

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

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            .AddMutationType<MutationType>();
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
public class Mutation
{
    public async Task<BookAddedPayload> AddBook(Book book)
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
            .BindComplexType<Mutation>();
    }
}
```

</ExampleTabs.Schema>
</ExampleTabs>

> ⚠️ Note: Only **one** mutation type can be registered using `AddMutationType()`. If we want to split up our mutation type into multiple classes, we can do so using type extensions.
>
> [Learn more about extending types](/docs/hotchocolate/defining-a-schema/extending-types)

A mutation type is just a regular object type, so everything that applies to an object type also applies to the mutation type (this is true for all root types).

[Learn more about object types](/docs/hotchocolate/defining-a-schema/object-types)

# Transactions

With multiple mutations executed serially in one request it can be useful to wrap these in a transaction that we can control.

Hot Chocolate provides for this the `ITransactionScopeHandler` which is used by the operation execution middleware to create transaction scopes for mutation requests.

Hot Chocolate provides a default implementation based on the `System.Transactions.TransactionScope` which works with Microsoft ADO.NET data provider and hence can be used in combination with Entity Framework.

The default transaction scope handler can be added like the following.

```csharp
services
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
services
    .AddGraphQLServer()
    .AddTransactionScopeHandler<CustomTransactionScopeHandler>();
```

# Conventions

> ⚠️ Experimental Warning: This feature is not yet finished and only available in previews. The API is not finalized and will change until its release.

In GraphQL, it is best practice to have a single argument on mutations called `input`, and each mutation should return a payload object.

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

Following this pattern helps to keep the schema evolvable but requires a lot of boilerplate code to realize.

HotChocolate has built-in conventions for inputs and payloads to minimize boilerplate code.

The HotChocolate mutation conventions are opt-in and can be enabled like the following:

```csharp
service
    .AddGraphQLServer()
    .EnableMutationConventions()
    ...
```

With the mutation conventions enabled, we can define the above with a single mutation class.

```csharp
public class Mutation
{
    [Input]
    [Payload]
    public User? UpdateUserNameAsync([ID] Guid userId, string username)
    {
    //...
    }
}
```

## Defining payloads

To define a payload, you can either annotate the resolver with `[Payload(...)]` or use `.Payload(..)` if you use the code-first approach.

The `Payload` middleware is optimized for the use case of a single field on the payload.

If you need more than one field, you should define your custom payload object.

The field's name that holds the result can be configured with the `FieldName` parameter of the attribute/extension method.
By default, the field name is the name of the returned runtime type. You can override the type name with the `TypeName` parameter.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public class Mutation
{
    [Payload("customPayloadField", TypeName = "CustomTypeName")] // <-------------
    public User CreateUserAsync(
        [Service] IUserService service,
        string username,
        string name,
        string lastName)
        => userSerivce.CreateUser(username, name, lastName);
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class Mutation
{
    public User CreateUserAsync(
        [Service] IUserService service,
        string username,
        string name,
        string lastName)
        => userSerivce.CreateUser(username, name, lastName);
}

public class MutationType : ObjectType<Mutation>
{
    protected override void Configure(
        IObjectTypeDescriptor<Mutation> descriptor)
    {
        descriptor
            .Field(f => f.CreateUserAsync(default, default, default, default))
            .Payload("customPayloadField", "CustomTypeName");
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
// Currently only supported in Code First and Annotation Based
```

</ExampleTabs.Schema>
</ExampleTabs>

The configuration above emits the following schema:

```graphql
type CustomTypeName {
  customPayloadField: User
}
```

## Defining inputs

The `[Input]` annotation or the `.Input()` extension method can combine arguments in an input object.

By default, all method parameters are combined into a single input object.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public class Mutation
{
    [Input]
    public User CreateUserAsync(
        [Service] IUserService service,
        string username,
        string name,
        string lastName)
        => userSerivce.CreateUser(username, name, lastName);
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class Mutation
{
    public User CreateUserAsync(
        [Service] IUserService service,
        string username,
        string name,
        string lastName)
        => userSerivce.CreateUser(username, name, lastName);
}

public class MutationType : ObjectType<Mutation>
{
    protected override void Configure(
        IObjectTypeDescriptor<Mutation> descriptor)
    {
        descriptor
            .Field(f => f.CreateUserAsync(default, default, default, default))
            .Input();
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
// Currently only supported in Code First and Annotation Based
```

</ExampleTabs.Schema>
</ExampleTabs>

The default name of the argument is `input`. You can configure this with the `name` parameter.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public class Mutation
{
    [Input("custom")]
    public User CreateUserAsync(
        [Service] IUserService service,
        string username,
        string name,
        string lastName)
        => userSerivce.CreateUser(username, name, lastName);
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class Mutation
{
    public User CreateUserAsync(
        [Service] IUserService service,
        string username,
        string name,
        string lastName)
        => userSerivce.CreateUser(username, name, lastName);
}

public class MutationType : ObjectType<Mutation>
{
    protected override void Configure(
        IObjectTypeDescriptor<Mutation> descriptor)
    {
        descriptor
            .Field(f => f.CreateUserAsync(default, default, default, default))
            .Input("custom");
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
// Currently only supported in Code First and Annotation Based
```

</ExampleTabs.Schema>
</ExampleTabs>

The configuration above emits the following schema:

```graphql
type Mutation {
  createUser(custom: CreateUserCustomInput): CreateUserPayload
}

type CreateUserPayload {
  user: User
}

input CreateUserCustomInput {
  username: String!
  name: String!
  lastName: String!
}
```

You can also define the `Input` annotation on arguments directly and define multiple inputs.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public class Mutation
{
    public User CreateUserAsync(
        [Service] IUserService service,
        [Input("a")] string username,
        [Input("a")] string name,
        [Input("b")] string lastName)
        => userSerivce.CreateUser(username, name, lastName);
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class Mutation
{
    public User CreateUserAsync(
        [Service] IUserService service,
        string username,
        string name,
        string lastName)
        => userSerivce.CreateUser(username, name, lastName);
}

public class MutationType : ObjectType<Mutation>
{
    protected override void Configure(
        IObjectTypeDescriptor<Mutation> descriptor)
    {
        descriptor
            .Field(f => f.CreateUserAsync(default, default, default, default))
            .Argument("username", x => x.Input("a"))
            .Argument("name", x => x.Input("a"))
            .Argument("lastName", x => x.Input("b"))
            .Input();
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
// Currently only supported in Code First and Annotation Based
```

</ExampleTabs.Schema>
</ExampleTabs>

The configuration above emits the following schema:

```graphql
type Mutation {
  createUser(a: CreateUserAInput, b: CreateUserBInput): CreateUserPayload
}

type CreateUserPayload {
  user: User
}

input CreateUserAInput {
  username: String!
  name: String!
}
input CreateUserBInput {
  lastName: String!
}
```

## Defining errors

The convetions also allow you to create mutations that follow the
[Stage 6a Pattern of Marc-Andre Giroux](https://xuorig.medium.com/a-guide-to-graphql-errors-bb9ba9f15f85), without having to declare a lot of boilerplate.

The basic concept of the error middleware is to keep the resolver clean of any error handling code and use exceptions to signal a error state. 
The HotChocolate schema is automatically rewritten into the correct GraphQL pattern.
A middleware will catch all the exceptions and rewrite the output to the correct types.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public class Mutation
{
    [Error(typeof(SomeSpecificDomainError))]
    [Error(typeof(SomeOtherError))]
    public CreateUserPayload CreateUser(CreateUserInput input)
    {
       // ...
    }
}

public record CreateUserInput(string UserName);

public record CreateUserPayload(User User);
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class Mutation
{
    public CreateUserPayload CreateUser(CreateUserInput input)
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
          .Field(f => f.CreateUser(default))
          .Error<SomeSpecificDomainError>()
          .Error<SomeOtherError>();
    }
}

public record CreateUserInput(string UserName);

public record CreateUserPayload(User User);
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
// Currently only supported in Code First and Annotation Based
```

</ExampleTabs.Schema>
</ExampleTabs>

The configuration above emits the following schema: 

```graphql
type Mutation {
  createUser(input: CreateUserInput!): CreateUserPayload!
}

input CreateUserInput {
  userName: String!
}

type CreateUserPayload {
  user: User
  # generated
  errors: [CreateUserError!]
}

type User {
  username: String
}

# generated
interface Error {
  message: String!
}

# generated
type SomeSpecificDomainError implements Error {
  message: String!
}

# generated
type SomeOtherDomainError implements Error {
  message: String!
}

# generated
union CreateUserError = SomeSpecificDomainError | SomeOtherDomainError
```


There are three ways to map an exception to a GraphQL error.

1. Map the exception directly
2. Map with a factory method (`CreateErrorFrom`)
3. Map with a constructor

> TIPP: You can use AggregateExceptions to return multiple errors

### Map exceptions directly
The quickest way to define a GraphQL Error, is to map the exception directly into the graph. You can just annotate the exception directly on the resolver.
If the exception is thrown and reaches the middleware, the middlware will catch the exception and rewrite it to the error payload.

> The name of the exception will be rewritten. `Exception` is replaced with `Error` to follow the common GraphQL naming conventions.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public class MyCustomException : Exception
{
     public MyCustomException() : base("My custom message") 
     {
     }
}

public class Mutation
{
    [Error(typeof(MyCustomException))]
    public CreateUserPayload CreateUser(CreateUserInput input)
    {
       // ...
    }
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class MyCustomException : Exception
{
     public MyCustomException() : base("My custom message") 
     {
     }
}

public class Mutation
{
    public CreateUserPayload CreateUser(CreateUserInput input)
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
          .Field(f => f.CreateUser(default))
          .Error<MyCustomException>();
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
// Currently only supported in Code First and Annotation Based
```

</ExampleTabs.Schema>
</ExampleTabs>


### Map with a factory method 

If there should be any translation between exception and error, you can defined a classwith factory methods. 
These factory methods receive a exception and returnna object which will be used as the representation of the error
You can either use the constructor or the `CreateErrorFrom` factory method.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public class MyCustomError
{
    public static MyCustomError CreateErrorFrom(DomainExceptionA ex)
    {
        return new MyCustomError();
    }

    public static MyCustomError CreateErrorFrom(DomainExceptionB ex)
    {
        return new MyCustomError();
    }

    public string Message => "My custom error Message";
}

public class Mutation
{
    [Error(typeof(MyCustomError))]
    public CreateUserPayload CreateUser(CreateUserInput input)
    {
       // ...
    }
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class MyCustomError
{
    public static MyCustomError CreateErrorFrom(DomainExceptionA ex)
    {
        return new MyCustomError();
    }

    public static MyCustomError CreateErrorFrom(DomainExceptionB ex)
    {
        return new MyCustomError();
    }

    public string Message => "My custom error Message";
}

public class Mutation
{
    public CreateUserPayload CreateUser(CreateUserInput input)
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
          .Field(f => f.CreateUser(default))
          .Error<MyCustomError>();
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
// Currently only supported in Code First and Annotation Based
```

</ExampleTabs.Schema>
</ExampleTabs>

You can also define factories in a dedicated class. 

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public class CreateUserErrorFactory
{
    public static MyCustomErrorA CreateErrorFrom(DomainExceptionA ex)
    {
        return new MyCustomError();
    }

    public static MyCustomErrorB CreateErrorFrom(DomainExceptionB ex)
    {
        return new MyCustomError();
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

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class CreateUserErrorFactory
{
    public static MyCustomErrorA CreateErrorFrom(DomainExceptionA ex)
    {
        return new MyCustomError();
    }

    public static MyCustomErrorB CreateErrorFrom(DomainExceptionB ex)
    {
        return new MyCustomError();
    }
}

public class Mutation
{
    public CreateUserPayload CreateUser(CreateUserInput input)
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
          .Field(f => f.CreateUser(default))
          .Error<CreateUserErrorFactory>();
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
// Currently only supported in Code First and Annotation Based
```

</ExampleTabs.Schema>
</ExampleTabs>

The factory methods do not have to be static. 
You can also use the `IPayloadErrorFactory<TError, TException>` interface, to define factory methods.

```csharp
public class CreateUserErrorFactory 
    : IPayloadErrorFactory<MyCustomErrorA, DomainExceptionA>
    , IPayloadErrorFactory<MyCustomErrorB, DomainExceptionB>
{
    public MyCustomErrorA CreateErrorFrom(DomainExceptionA ex)
    {
        return new MyCustomError();
    }

    public MyCustomErrorB CreateErrorFrom(DomainExceptionB ex)
    {
        return new MyCustomError();
    }
}
```

### Map with a constructor
Instead of using factory methods, you can also use the constructor of the date transfer object directly.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
public class MyCustomError
{
    public MyCustomError(MyCustomDomainException exception)
    {
        Message = exception.Message;
    }

    public MyCustomError(MyCustomDomainException2 exception)
    {
        Message = exception.Message;
    }

    public string Message { get; }
}

public class Mutation
{
    [Error(typeof(MyCustomError))]
    public CreateUserPayload CreateUser(CreateUserInput input)
    {
       // ...
    }
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

```csharp
public class MyCustomError
{
    public MyCustomError(MyCustomDomainException exception)
    {
        Message = exception.Message;
    }

    public MyCustomError(MyCustomDomainException2 exception)
    {
        Message = exception.Message;
    }

    public string Message { get; }
}

public class Mutation
{
    public CreateUserPayload CreateUser(CreateUserInput input)
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
          .Field(f => f.CreateUser(default))
          .Error<MyCustomError>();
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
// Currently only supported in Code First and Annotation Based
```

</ExampleTabs.Schema>
</ExampleTabs>

### Customize the error interface
The error interface is shared across all errors that the schema defines.
By default this interface type is called `Error` and defines a non nullable field `message`

```graphql
interface Error {
  message: String!
}
```

This interface can be customized. 
Keep in mind that all you error types, have to implement the contract that the interface declares!
You errors/exceptions do not have to implement the common interface, but they have to declare all the members that the interface defines.

<ExampleTabs>
<ExampleTabs.Annotation>

```csharp
[GraphQLName("UserError")]
public interface IUserError 
{
  string Message { get; }

  string Code { get; }
}

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            // ... Omitted code for brevity
            .AddErrorInterfaceType<IUserError>();
    }
}
```

</ExampleTabs.Annotation>
<ExampleTabs.Code>

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

public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        services
            .AddGraphQLServer()
            // ... Omitted code for brevity
            .AddErrorInterfaceType<CustomErrorInterfaceType>();
    }
}
```

</ExampleTabs.Code>
<ExampleTabs.Schema>

```csharp
// Currently only supported in Code First and Annotation Based
```

</ExampleTabs.Schema>
</ExampleTabs>

```graphql
interface UserError {
  message: String!
  code: String!
}
```
