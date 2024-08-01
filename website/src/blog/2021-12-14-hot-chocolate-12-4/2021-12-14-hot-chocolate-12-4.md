---
path: "/blog/2021/12/14/hot-chocolate-12-4"
date: "2021-12-14"
title: "A Holly Jolly Christmas with Hot Chocolate 12.4"
tags: ["hotchocolate", "graphql", "dotnet", "aspnetcore"]
featuredImage: "hot-chocolate-12-4-banner.png"
author: Michael Staib
authorUrl: https://github.com/michaelstaib
authorImageUrl: https://avatars1.githubusercontent.com/u/9714350?s=100&v=4
---

Christmas is almost here! With the beginning of the festivities, more and more people are taking off from work. But at ChilliCream, we are still all hands down working on many new things.

Today, we are releasing Hot Chocolate 12.4, which brings a lot of great new productivity features to the table. Let me give you a little tour of what's new.

# Mutation Conventions

The main feature we worked on for this release was definitely the mutation conventions. The new convention will help minimize the effort to create well-defined mutations.

**What do I mean with well-defined mutations?**

In GraphQL, we have developed specific patterns around mutations. One foundational pattern is about the structure of mutations. It was initially developed by Facebook and belonged to the [Relay server specification](https://relay.dev/docs/v9.1.0/graphql-server-specification/#mutations).

> By convention, mutations are named as verbs, their inputs are the name with "Input" appended at the end, and they return an object that is the name with "Payload" appended.

```sdl
type Mutation {
  renameUser(input: RenameUserInput!): RenameUserPayload!
}

input RenameUserInput {
  userId: ID!
  username: String!
}

type RenameUserPayload {
  user: User
}
```

Essentially, each mutation consists of three parts:

- The mutation resolver.
- The mutation payload.
- The mutation input.

Each mutation has its own mutation payload and its own mutation input. This is done to keep a mutation evolvable over time. If we instead share inputs or payloads with other mutations, we would quickly get stuck with our mutation design since changing one mutation will often break the other mutation. By giving each mutation its own set of input and payload, we can evolve each mutation without breaking the other.

There are other reasons for this particular design. We, for instance, have a single input so that clients do not need to deconstruct their objects, and mutations do not end up with hundreds of arguments. The mutation clearly exposes what is required to execute it by having a single input. Further, it makes it very simple for client applications to craft the input object and pass it as a variable.

A separate payload object allows us to expose all affected objects by the mutation. So that the client can fetch all the affected data, it is interested in. Moreover, the payload allows us to expose user errors through just another field to the client on our payload.

```sdl
type Mutation {
  renameUser(input: RenameUserInput!): RenameUserPayload!
}

input RenameUserInput {
  userId: ID!
  username: String!
}

type RenameUserPayload {
  user: User
  errors: [RenameUserError!]
}

union RenameUserError = UserNameTakenError | InvalidUserNameError
```

We can see that having this particular design of mutation is very beneficial for our schema over time and for the usage by our consumers.

What was not so nice is that we needed so many types in C# to create a simple mutation.

```csharp
public class Mutation
{
    public async Task<RenameUserPayload> RenameUserAsync(
        RenameUserInput input,
        IUserService userService,
        CancellationToken cancellationToken)
    {
          try
          {
              User updateUser = await userService.RenameUserAsync(input.UserId, input.Username, cancellationToken);
              return new RenameUserPayload(updatedUser);
          }
          catch (UserNameTakenException ex)
          {
              return new RenameUserPayload(new UserNameTakenError(ex));
          }
          catch (ArgumentException ex)
          {
              return new RenameUserPayload(new InvalidUserNameError(ex));
          }
    }
}

public record RenameUserInput([property: ID(nameof(User)))] Guid UserId, string Username);

public class RenameUserPayload
{
   // code omitted for brevity
}

public class UserNameTakenError
{
   // code omitted for brevity
}

public class InvalidUserNameError
{
   // code omitted for brevity
}
```

That is where the core team tinkered for almost a year until the end to make it very simple to expose mutations. We wanted to eliminate repetitive C# code and let the user focus on the mutation itself. Mutation conventions let you opt-in very quickly by essentially just flipping the switch.

```csharp
services
    .AddGraphQLServer()
    .AddMutationConventions() // < -- this line enable the new conventions.
    ...
```

Once activated, the convention will transform mutations that do not yet apply to the mutation pattern. This works with code-first, schema-first, and annotation-based. Meaning, no matter what approach you take to build your schema, these new conventions will make your life easier.

**Annotation-Base**:

```csharp
public class Mutation
{
    public Task<User> RenameUserAsync(
        [ID(nameof(User))] Guid userId,
        string username,
        IUserService userService,
        CancellationToken cancellationToken)
        => userService.RenameUserAsync(userId, username, cancellationToken);
}
```

**Code-First**:

```csharp
public class MutationType : ObjectType<Mutation>
{
    protected override void Configure(IObjectTypeDescriptor<Mutation> descriptor)
    {
        descriptor
            .Field(f => f.RenameUserAsync(default, default, default, default))
            .Argument("userId", a => a.ID(nameof(User)));
    }
}

public class Mutation
{
    public Task<User> RenameUserAsync(
        Guid userId,
        string username,
        IUserService userService,
        CancellationToken cancellationToken)
        => userService.RenameUserAsync(userId, username, cancellationToken);
}
```

OR without runtime-type:

```csharp
public class MutationType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .Field("renameUser")
            .Argument("userId", a => a.ID(nameof(User)))
            .Argument("username", a => a.Type<NonNullType<StringType>>())
            .Resolve(async ctx =>
            {
                var userService = ctx.Service<IUserService>();
                var userId = ctx.ArgumentValue<Guid>("userId");
                var username = ctx.ArgumentValue<string>("username");

                return userService.RenameUserAsync(userId, username, cancellationToken);
            });
    }
}
```

**Schema-First**:

```sdl
type Mutation {
  renameUser(userId: ID!, username: String!): User
}
```

```csharp
services
    .AddGraphQLServer()
    .AddMutationConventions()
    .AddDocumentFromString(@"
        type Mutation {
          renameUser(userId: ID!, username: String!): User
        }")
    .BindRuntimeType<Mutation>();

public class Mutation
{
    public Task<User> RenameUserAsync(
        Guid userId,
        string username,
        IUserService userService,
        CancellationToken cancellationToken)
        => userService.RenameUserAsync(userId, username, cancellationToken);
}
```

The conventions also let you onboard more slowly by opting-in on a per mutation basis.

```csharp
services
    .AddGraphQLServer()
    .AddMutationConventions(
        new MutationConventionOptions
        {
            ApplyToAllMutations = false
        })
    ...

public class Mutation
{
    [UseMutationConvention]
    public Task<User> RenameUserAsync(
        [ID(nameof(User))] Guid userId,
        string username,
        IUserService userService,
        CancellationToken cancellationToken)
        => userService.RenameUserAsync(userId, username, cancellationToken);
}
```

Further, you can customize the naming patterns for creating the payload/input/error type names.

```csharp
services
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
```

> Note: You can also partially opt-out of the convention by for instance crafting your own input type but letting the convention produce the payload.

## Errors

The second part of this new mutation convention involves user errors. We did a lot of work investigating how we should enable errors or even what pattern we should follow.

Marc-Andre Giroux wrote a great [blog post](https://xuorig.medium.com/a-guide-to-graphql-errors-bb9ba9f15f85) on the various error patterns in GraphQL and analyzed their pro and cons regarding evolvability and usability.

The error stage 6a has all the pros we want:

- Expressive and Discoverable Schema
- Support for Multiple Errors
- Easier Mutation Evolution

But at the same time, it wasn't easy to implement since it came with many moving parts. This meant that we had to write repetitive code again to fulfill this error pattern.

```sdl
type Mutation {
  renameUser(input: RenameUserInput!): RenameUserPayload!
}

input RenameUserInput {
  userId: ID!
  username: String!
}

type RenameUserPayload {
  user: User
  errors: [RenameUserError!]
}

union RenameUserError = UserNameTakenError | InvalidUserNameError

type UserNameTakenError implements Error {
  message: String!
  code: string!
  username: string!
  suggestedAlternatives: [String!]
}

type InvalidUserNameError implements Error {
  message: String!
  code: string!
  username: string!
  invalidCharacters: [String!]!
}

interface Error {
  message: String!
  code: string!
}
```

We looked at how people traditionally solve their errors, and in most cases, people still write custom exceptions. We now allow for annotating these custom exceptions on the resolver and exposing them as user errors on the mutation payload.

```csharp
public class Mutation
{
    [Error<UserNameTakenException>]
    [Error<ArgumentException>]
    public Task<User> RenameUserAsync(
        [ID(nameof(User))] Guid userId,
        string username,
        IUserService userService,
        CancellationToken cancellationToken)
        => userService.RenameUserAsync(userId, username, cancellationToken);
}
```

The above code will translate to the following schema:

```sdl
type Mutation {
  renameUser(input: RenameUserInput!): RenameUserPayload!
}

input RenameUserInput {
  userId: ID!
  username: String!
}

type RenameUserPayload {
  user: User
  errors: [RenameUserError!]
}

union RenameUserError = UserNameTakenError | ArgumentError

type UserNameTakenError implements Error {
  message: String!
  username: string!
  suggestedAlternatives: [String!]
}

type ArgumentError implements Error {
  message: String!
  paramName: string!
}

interface Error {
  message: String!
}
```

Again, we know that we do not always want to expose our errors one to one with exceptions or we even want to have more robust control of which information is exposed to the outside world. This is where we allow for error objects to substitute exceptions that are thrown.

```csharp
public class Mutation
{
    [Error<UserNameTakenException>]
    [Error<InvalidUserNameError>]
    public Task<User> RenameUserAsync(
        [ID(nameof(User))] Guid userId,
        string username,
        IUserService userService,
        CancellationToken cancellationToken)
        => userService.RenameUserAsync(userId, username, cancellationToken);
}

public class InvalidUserNameError
{
    public InvalidUserNameError(ArgumentException ex)
    {
        Message = ex.Message;
    }

    public string Message { get; }

    public string[] InvalidCharacters => new [] { "=", "^" }:
}
```

The error object shape defines the error type shape on our schema and ensures that even if the exception is refactored to have more or less information, we do not accidentally expose information that we do not want to expose.

You can read more about all of this in our [documentation](https://chillicream.com/docs/hotchocolate/v12/defining-a-schema/mutations/#conventions). The documentation also outlines more variants to create user errors.

One last aspect before we move on to the next topic. We also thought about result objects where a service we use does not use exceptions but already has error objects. Or F# code where we might have a union representing a result and its errors. We do not yet support these kinds of things but will further iterate on the current conventions to include these approaches towards results and errors in the future.

# Dependency Injection Improvements

Users that build large schemas with Hot Chocolate from time to time have asked us to help them reduce the DI code they have to write for resolvers.

```csharp
public async Task<ScheduleSessionPayload> ScheduleSessionAsync(
    ScheduleSessionInput input,
    [Service] ISessionService sessionService,
    [Service] ITopicEventSender eventSender)
{
    // code omitted for brevity
}
```

The above resolver gets injected a service we want to interact with within our resolver. We use this service in many resolvers throughout our solution, and having to repeatedly to annotate our service with the attributes `[FromService]`, `[Service]` or `[ScopedService]` bloats our code.

With our new version, you can now register this service as a well-known service on the schema. Wherever the resolver compiler finds this service type, it will generate a dependency injection code resolving it from the DI.

**Registration:**

```csharp
services
    .AddGraphQLServer()
    .RegisterService<ISessionService>()
    .RegisterService<ITopicEventReceiver>()
    .RegisterService<ITopicEventSender>()
    ...
```

**Resolver:**

```csharp
public async Task<ScheduleSessionPayload> ScheduleSessionAsync(
    ScheduleSessionInput input,
    ISessionService sessionService,
    ITopicEventSender eventSender)
{
    // code omitted for brevity
}
```

But this is not where this feature stops. We also wanted to simplify handling services of different kinds. For instance, some services are not thread-safe and can only be accessed by a single resolver in a specific request at once. With the new well-known services feature, we can tell the execution engine about this fact and produce a query plan that will accommodate this.

```csharp
services
    .AddGraphQLServer()
    .RegisterService<ISessionService>(ServiceKind.Synchronized)
    .RegisterService<ITopicEventReceiver>()
    .RegisterService<ITopicEventSender>()
    ...
```

We also might be dealing with pooled services or objects. These can now also be registered as a service.

```csharp
services
    .AddGraphQLServer()
    .RegisterService<ISessionService>(ServiceKind.Synchronized)
    .RegisterService<HeavyObject>(ServiceKind.Pooled)
    .RegisterService<ITopicEventReceiver>()
    .RegisterService<ITopicEventSender>()
    ...
```

We will, in this case, retrieve an `ObjectPool<TService>` from the DI, rent out the specified service or object from the pool and return it when the resolver is finished. The code you had to write to handle such complex cases is now reduced to a single registration line.

Last but not least, we also support now resolver level scoping, meaning you can register a service that shall be scoped to a resolver. In this case, we will create for these services in your resolver an `IServiceScope` from which we retrieve resolver-level services. After the resolver is completed, the scope is disposed of and with it the scoped services you have used.

We also wanted to clean up the attributes around services and allow for the same capabilities through the service attribute. That is why we introduced the `ServiceKind` also on the attribute.

```csharp
public async Task<ScheduleSessionPayload> ScheduleSessionAsync(
    ScheduleSessionInput input,
    [Service(ServiceKind.Synchronized)] ISessionService sessionService,
    [Service] ITopicEventSender eventSender)
{
    // code omitted for brevity
}
```

Whether you are using well-known services registered at the schema level or services declared with the attribute, you have the same capabilities and a new streamlined experience.

# Entity Framework Improvements

When redoing the services, we also looked at EF Core. The DBContext is a unique service that needs to be treated differently depending on how you registered it with your DI.

By default, if you just use `services.AddDbContext<MyDbContext>()`, your context will be registered in the DI as a scoped service. This means a single DBContext will be used for all resolvers of the request. Since a DBContext is not thread-safe, we need to ensure that only one resolver at a time can access this scoped service.

Like with the well-known services feature, we can now register a well-known DBContext on the schema level and tell the execution engine how this service shall be used. Since a scoped DBContext is the most common thing, we have decided to use it as the default whenever you register a well-known DBContext.

```csharp
builder.Services
    .AddDbContext<BookContext>(
        (s, o) => o
            .UseSqlite("Data Source=books.db")
            .UseLoggerFactory(s.GetRequiredService<ILoggerFactory>()))
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .RegisterDbContext<BookContext>();
```

The DBContext can be registered as a well-known DBContext with three different behaviors.

The first and the default is `DbContextKind.Synchronized` which will ensure that all resolvers that access such a DBContext synchronize their access through the query execution plan.

You also can use a pooled DBContext with the `DbContextKind.Pooled`. In this case, we will wrap a middleware around your resolver that will retrieve the DBContext through the DBContextFactory, inject the DBContext in your resolver and dispose of it once the resolver pipeline is finished executing.

```csharp
builder.Services
    .AddPooledDbContextFactory<BookContext>(
        (s, o) => o
            .UseSqlite("Data Source=books.db")
            .UseLoggerFactory(s.GetRequiredService<ILoggerFactory>()))
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .RegisterDbContext<BookContext>(kind: DbContextKind.Pooled);
```

The last way to use a well-known DBContext is as a resolver-level DBContext. In this case, we will treat it as a resolver-level service that is retrieved from a resolver service scope. With this, you essentially get a new DBContext per resolver without configuring anything special.

With the well-known DBContext, you now can switch the behavior of how resolvers interact with your DBContext with one line of code. With this, you essentially can start easy, and as traffic starts to grow and you get more pressure on your API, you can switch to DBContext pooling.

In combination with well-known services, you can also much easier handle DI behavior when your DBContext is walled off behind your business layer since we can scope and control your service objects.

We will further refine these features to integrate more use-cases and reduce the complexity even further.

# DateOnly and TimeOnly

One small note, we now support `DateOnly` and `TimeOnly`. They will now work with the current set of scalars and also with HotChocolate.Data.

We are still working on adding support NodaTime to HotChocolate.Data so that you can write filters that use NodaTime object beneath.

# Outlook

Work on 12.5 already is underway, and there are four notable things we are working on for this next iteration:

- Client Controlled Nullability (<https://github.com/graphql/graphql-spec/pull/895>)
- `OneOf` inputs and `OneOf` fields (<https://github.com/graphql/graphql-spec/pull/825>)
- OpenTelemetry and Elastic APM support
- Banana Cake Pop Themes

You can have a look at the milestone here:
<https://github.com/ChilliCream/graphql-platform/milestone/72>

We will also be working on the new stitching engine over Christmas and hope to have the first previews ready at the end of January.

Things are moving together and becoming more and more connected.

We hope you all enjoy this new version of Hot Chocolate and have some great holidays.

Join us on <https://slack.chillicream.com> and chime into the discussion around GraphQL on .NET!

> If you like our project help us by [starring it on GitHub](https://github.com/ChilliCream/graphql-platform/stargazers). A GitHub star is the easiest contribution you can give to an OSS project. Star the open source projects you use or love!
