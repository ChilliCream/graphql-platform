---
title: "Errors"
---

import { ExampleTabs } from "../../../components/mdx/example-tabs"

The error extension helps you create mutations that follow the
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

# Defining Errors
There are three ways to map an exception to a GraphQL error.

1. Map the exception directly
2. Map with a factory method (`CreateErrorFrom`)
3. Map with a constructor

> TIPP: You can use AggregateExceptions to return multiple errors

## Map exceptions directly
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


## Map with a factory method 

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

## Map with a constructor
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

# Customize the error interface
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
