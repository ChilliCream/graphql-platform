---
title: "Mutations"
---

Mutations are the entry points for writing data in a GraphQL schema. Use them for actions that create, update, delete, or otherwise change application state.

A mutation field is defined on the root `Mutation` type:

```graphql
type Mutation {
  createBook(input: CreateBookInput!): CreateBookPayload!
}
```

Clients invoke this field using a `mutation` operation and select the changed data they want from the result:

```graphql
mutation CreateBook($input: CreateBookInput!) {
  createBook(input: $input) {
    book {
      id
      title
    }
  }
}
```

This page covers mutation operation fields and schema design. For details on input modeling, see [Input Object Types](./input-object-types). For Relay-specific payload `query` fields, refer to [Query Field in Mutation Payloads](/docs/hotchocolate/v16/build/schema-elements/relay#query-field-in-mutation-payloads).

# How mutation execution works

Hot Chocolate exposes write operations through the root `Mutation` type. The root field initiates the write, and fields selected beneath the root field read from the returned object or payload.

```graphql
mutation PublishAndRename($bookId: ID!) {
  publishBook(input: { bookId: $bookId }) {
    book {
      id
      publishedAt
    }
  }
  renameBook(input: { bookId: $bookId, title: "GraphQL in Practice" }) {
    book {
      id
      title
    }
  }
}
```

Within a single mutation operation:

| Part of the operation                            | Execution behavior                                        | Design guidance                                                                            |
| ------------------------------------------------ | --------------------------------------------------------- | ------------------------------------------------------------------------------------------ |
| Root mutation fields                             | Executed serially in the order they appear in the request | Use this ordering when one root field must observe state changed by an earlier root field. |
| Child selections below a mutation field          | Executed like normal object fields for the returned value | Treat these fields as reads that shape the response.                                       |
| Database commits, messages, emails, and webhooks | Not controlled by GraphQL ordering alone                  | Coordinate them in application code or transaction configuration.                          |

Serial execution provides ordering, but it is not a database transaction and does not roll back external side effects. Prefer a single root mutation field for each client action, and let your application service manage the unit of work.

A typical request flow looks like this:

```text
Client mutation operation
        |
        v
Root Mutation field, ordered with sibling root mutation fields
        |
        v
Resolver method, GraphQL arguments plus injected services
        |
        v
Application or domain service, validation and persistence
        |
        v
Payload or changed object returned to the client
        |
        v
Selected child fields, normal read-style resolver execution
```

# Adding a mutation field with `[MutationType]`

Use `[MutationType]` to add fields to the root `Mutation` type with implementation-first schema building. In v16 source-generator examples, make the type `partial`.

```csharp
#nullable enable

using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace Books.Types;

[MutationType]
public static partial class BookMutations
{
    public static async Task<Book> CreateBookAsync(
        CreateBookInput input,
        BookService books,
        CancellationToken cancellationToken)
    {
        return await books.CreateAsync(
            input.Title,
            input.AuthorId,
            cancellationToken);
    }
}

public sealed record CreateBookInput(
    string Title,
    [property: ID<Author>] int AuthorId);

public sealed record Book(
    [property: ID<Book>] int Id,
    string Title,
    [property: ID<Author>] int AuthorId);
```

Hot Chocolate uses the resolver method name to generate the field name. The `Async` suffix is removed, so `CreateBookAsync` becomes `createBook`.

Expected SDL without mutation conventions:

```graphql
type Mutation {
  createBook(input: CreateBookInput!): Book!
}

input CreateBookInput {
  title: String!
  authorId: ID!
}

type Book {
  id: ID!
  title: String!
  authorId: ID!
}
```

Clients can call the field directly:

```graphql
mutation CreateBook($input: CreateBookInput!) {
  createBook(input: $input) {
    id
    title
  }
}
```

Keep resolver methods focused. They should adapt GraphQL input to application logic, call a service, and return the changed object or payload.

# Which resolver parameters become arguments?

Hot Chocolate maps client input parameters to GraphQL arguments. Registered services and framework values are not exposed in the public schema.

```csharp
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace Books.Types;

[MutationType]
public static partial class BookMutations
{
    public static async Task<Book> RenameBookAsync(
        [ID<Book>] int bookId,
        string title,
        BookService books,
        CancellationToken cancellationToken)
    {
        return await books.RenameAsync(bookId, title, cancellationToken);
    }
}
```

Expected SDL without mutation conventions:

```graphql
type Mutation {
  renameBook(bookId: ID!, title: String!): Book!
}
```

| Resolver parameter                                        | GraphQL schema effect                                   | Notes                                                                                                |
| --------------------------------------------------------- | ------------------------------------------------------- | ---------------------------------------------------------------------------------------------------- |
| Scalar parameter, such as `string title`                  | Becomes a field argument.                               | See [Arguments](./arguments) for naming, defaults, IDs, and nullability.                             |
| Input object parameter, such as `CreateBookInput input`   | Becomes a field argument whose type is an input object. | See [Input Object Types](./input-object-types) for records, validation, defaults, and `Optional<T>`. |
| Registered service parameter, such as `BookService books` | No argument.                                            | Register the service with dependency injection.                                                      |
| `CancellationToken`                                       | No argument.                                            | Pass it to async work.                                                                               |

If a service appears as an argument, Hot Chocolate did not bind it as a service. Check the service registration and parameter binding.

# Registering a code-first mutation root type

If you prefer descriptor-based configuration, register a mutation root type with `.AddMutationType<T>()`.

```csharp
// Types/BookMutations.cs
namespace Books.Types;

public sealed class BookMutations
{
    public async Task<Book> CreateBookAsync(
        CreateBookInput input,
        BookService books,
        CancellationToken cancellationToken)
    {
        return await books.CreateAsync(
            input.Title,
            input.AuthorId,
            cancellationToken);
    }
}
```

```csharp
// Types/BookMutationsType.cs
using HotChocolate.Types;

namespace Books.Types;

public sealed class BookMutationsType : ObjectType<BookMutations>
{
    protected override void Configure(
        IObjectTypeDescriptor<BookMutations> descriptor)
    {
        descriptor.Field(t => t.CreateBookAsync(default!, default!, default));
    }
}
```

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddMutationType<BookMutationsType>();
```

Use descriptor configuration when you need explicit binding, names, descriptions, middleware, or type overrides. For basic schema composition, `[MutationType]` keeps the mutation field close to its resolver.

# Splitting mutation fields across features

You can define mutation fields in multiple `[MutationType]` classes. Hot Chocolate merges them into a single GraphQL `Mutation` root type.

```csharp
// Types/BookMutations.cs
using HotChocolate.Types;

namespace Books.Types;

[MutationType]
public static partial class BookMutations
{
    public static async Task<Book> CreateBookAsync(
        CreateBookInput input,
        BookService books,
        CancellationToken cancellationToken)
        => await books.CreateAsync(input.Title, input.AuthorId, cancellationToken);
}
```

```csharp
// Types/AuthorMutations.cs
using HotChocolate.Types;

namespace Books.Types;

[MutationType]
public static partial class AuthorMutations
{
    public static async Task<Author> CreateAuthorAsync(
        CreateAuthorInput input,
        AuthorService authors,
        CancellationToken cancellationToken)
        => await authors.CreateAsync(input.Name, cancellationToken);
}
```

Expected SDL:

```graphql
type Mutation {
  createBook(input: CreateBookInput!): Book!
  createAuthor(input: CreateAuthorInput!): Author!
}
```

Organize mutation classes by feature, aggregate, or bounded context. Avoid a single large mutation class as the schema grows. If two methods map to the same GraphQL field name after naming conventions are applied, schema creation fails. Rename one method or configure the field name explicitly.

# Shaping fields with mutation conventions

Mutation conventions generate a consistent shape for clients:

- A single `input` argument that groups client values
- A payload object that can evolve over time
- Optional typed payload errors for expected domain failures

Enable conventions globally if your schema should use this shape for all mutation fields:

```csharp
// Program.cs
builder
    .AddGraphQL()
    .AddMutationConventions(applyToAllMutations: true);
```

With conventions enabled, you can write a resolver with plain client input parameters. Hot Chocolate groups those parameters into a generated input type and wraps the return value in a generated payload type.

```csharp
#nullable enable

using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace Accounts.Types;

[MutationType]
public static partial class UserMutations
{
    public static async Task<User?> UpdateUserNameAsync(
        [ID<User>] int userId,
        string username,
        UserService users,
        CancellationToken cancellationToken)
    {
        return await users.UpdateNameAsync(
            userId,
            username,
            cancellationToken);
    }
}

public sealed record User(
    [property: ID<User>] int Id,
    string Username);
```

Expected SDL with mutation conventions:

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

type User {
  id: ID!
  username: String!
}
```

`UserService` and `CancellationToken` are excluded from `UpdateUserNameInput` because they are resolver infrastructure, not client input.

Client mutation:

```graphql
mutation UpdateUserName($input: UpdateUserNameInput!) {
  updateUserName(input: $input) {
    user {
      id
      username
    }
  }
}
```

Variables:

```json
{
  "input": {
    "userId": "VXNlcjox",
    "username": "ada"
  }
}
```

## Opting in or out per field

If you want to use conventions only on selected mutation fields, register conventions without applying them to every field:

```csharp
builder
    .AddGraphQL()
    .AddMutationConventions(applyToAllMutations: false);
```

Then opt in per field:

```csharp
[UseMutationConvention]
public static async Task<User?> UpdateUserNameAsync(
    [ID<User>] int userId,
    string username,
    UserService users,
    CancellationToken cancellationToken)
{
    return await users.UpdateNameAsync(userId, username, cancellationToken);
}
```

When global conventions are active, opt out for a field that should keep its direct argument and return shape:

```csharp
[UseMutationConvention(Disable = true)]
public static async Task<Book> ImportBookAsync(
    CreateBookInput input,
    BookImportService imports,
    CancellationToken cancellationToken)
{
    return await imports.ImportAsync(input, cancellationToken);
}
```

Mutation fields that use `[UseMutationConvention]` must return a value. A `void`, `Task`, or `ValueTask` return cannot produce a payload.

## Configuring convention names

Use `MutationConventionOptions` to change generated names across the schema.

```csharp
using HotChocolate.Types;

builder
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
        });
```

| API or option                                         | Purpose                                                                             |
| ----------------------------------------------------- | ----------------------------------------------------------------------------------- |
| `.AddMutationConventions()`                           | Enables mutation conventions. The default applies conventions to all mutations.     |
| `.AddMutationConventions(applyToAllMutations: false)` | Registers conventions and requires per-field opt-in with `[UseMutationConvention]`. |
| `[UseMutationConvention]`                             | Applies or customizes conventions on one mutation field.                            |
| `[UseMutationConvention(Disable = true)]`             | Excludes one field from global conventions.                                         |
| `InputArgumentName`                                   | Renames the generated single argument.                                              |
| `InputTypeNamePattern`                                | Changes generated input type names.                                                 |
| `PayloadTypeNamePattern`                              | Changes generated payload type names.                                               |
| `PayloadErrorTypeNamePattern`                         | Changes generated error union type names.                                           |
| `PayloadErrorsFieldName`                              | Renames the payload errors field.                                                   |
| `ApplyToAllMutations`                                 | Applies conventions to every mutation field when set to `true`.                     |

If your resolver already accepts a custom input type or returns a custom payload type that matches the convention names, Hot Chocolate can use your types instead of generating replacements. For detailed input object design, see [Input Object Types](./input-object-types).

# Choosing names that express client intent

Mutation field names should read like client actions. Prefer verb phrases that describe the business or workflow result.

| Prefer            | Avoid when it leaks implementation                       |
| ----------------- | -------------------------------------------------------- |
| `createBook`      | `insertBookRow`                                          |
| `renameTrack`     | `updateTrackNameColumn`                                  |
| `scheduleSession` | `setSessionStartTime` when scheduling is the user action |
| `checkInAttendee` | `updateAttendeeStatus` when check-in is the workflow     |
| `cancelOrder`     | `deleteOrder` when the order remains in history          |

Use inputs and payloads to clarify the contract. For example, an update mutation often needs an ID plus changed values. Partial updates may require `Optional<T>` to distinguish omitted values from explicit `null`. For those details, see [Input Object Types](./input-object-types).

# Returning payloads that clients can select from

A payload allows clients to select the state they need after the write. Mutation conventions can generate a payload for the returned object, and custom payloads are useful when one action returns several values.

```csharp
public sealed record ScheduleSessionPayload(
    Session Session,
    Track Track);
```

```graphql
type ScheduleSessionPayload {
  session: Session!
  track: Track!
}
```

Expected domain failures should be modeled as typed payload errors, not as unexpected top-level GraphQL errors. Hot Chocolate supports APIs such as `[Error]`, `[Error<T>]`, `.AddErrorInterfaceType<T>()`, `FieldResult<...>`, and `IPayloadErrorFactory<TException, TError>` for this pattern.

A small payload error shape:

```graphql
type CreateBookPayload {
  book: Book
  errors: [CreateBookError!]
}

union CreateBookError = BookTitleTakenError | AuthorNotFoundError
```

For complete error modeling and exception mapping, see [Error Handling](/docs/hotchocolate/v16/_leagcy/guides/error-handling).

# Authorize mutation fields

Authorization is field-level behavior. Put authorization attributes on mutation fields that require authenticated users or policies.

```csharp
using HotChocolate.Authorization;
using HotChocolate.Types;

namespace Accounts.Types;

[MutationType]
public static partial class AccountMutations
{
    [Authorize]
    public static async Task<User> AddAddressAsync(
        AddAddressInput input,
        AccountService accounts,
        CancellationToken cancellationToken)
    {
        return await accounts.AddAddressAsync(input, cancellationToken);
    }

    [AllowAnonymous]
    public static async Task<User> RegisterAsync(
        RegisterInput input,
        AccountService accounts,
        CancellationToken cancellationToken)
    {
        return await accounts.RegisterAsync(input, cancellationToken);
    }
}
```

Use Hot Chocolate's `[AllowAnonymous]` attribute, not `Microsoft.AspNetCore.Authorization.AllowAnonymousAttribute`. For policies, roles, request-level authorization, and setup, see [Authorization](/docs/hotchocolate/v16/build/security/authorization).

# Coordinate transactions and side effects

GraphQL serializes top-level mutation fields for ordering. Transactions are a separate application or Hot Chocolate configuration concern. External side effects, such as email, message publishing, and webhooks, may not roll back with database state.

Current v16 mutation guidance includes transaction scope handler APIs for mutation requests. Treat them as integration points, not as the default unit-of-work design for every application.

| API                                    | Purpose                                                                                 |
| -------------------------------------- | --------------------------------------------------------------------------------------- |
| `.AddDefaultTransactionScopeHandler()` | Adds the default transaction scope handler when it fits your hosting and data provider. |
| `.AddTransactionScopeHandler<T>()`     | Registers a custom transaction scope handler.                                           |
| `ITransactionScopeHandler`             | Defines custom transaction scope behavior.                                              |

Verify package availability, hosting, data provider, and transaction requirements before applying transaction scope handlers broadly. In many applications, the mutation resolver calls an application service that owns the unit of work.

# Keep Relay payload query fields separate

Relay-style payload `query: Query` is an optional enhancement for clients that need to refetch related state from a mutation payload. Keep that configuration on the Relay page.

Discoverability APIs:

| API or option                        | Purpose                                             |
| ------------------------------------ | --------------------------------------------------- |
| `.AddQueryFieldToMutationPayloads()` | Adds a `query` field to matching mutation payloads. |
| `QueryFieldName`                     | Renames the generated payload query field.          |
| `MutationPayloadPredicate`           | Chooses which payload types receive the field.      |

See [Query Field in Mutation Payloads](/docs/hotchocolate/v16/build/schema-elements/relay#query-field-in-mutation-payloads) for the Relay-specific walkthrough.

# Troubleshoot mutation fields

| Symptom                                                                     | Likely cause                                                                                                  | Fix                                                                                                                                |
| --------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------- |
| A service parameter appears as a GraphQL argument or generated input field. | Hot Chocolate did not bind it as a service.                                                                   | Check DI registration and parameter binding. See [Dependency Injection](/docs/hotchocolate/v16/build/resolvers/service-injection). |
| A mutation class does not contribute fields.                                | The implementation-first class is not marked for the mutation root or is not handled by the source generator. | Use `[MutationType]` on a partial class and verify the file is part of the project.                                                |
| Multiple mutation classes create a name conflict.                           | Two resolver methods map to the same GraphQL field name.                                                      | Rename one method or configure the field name explicitly.                                                                          |
| A payload is not generated with an `input` argument.                        | Mutation conventions are not enabled or are disabled for the field.                                           | Check `.AddMutationConventions(...)` and `[UseMutationConvention]`.                                                                |
| A domain exception appears in top-level `errors`.                           | The exception is not declared as a payload error or conventions are not active.                               | Model expected failures as payload errors. See [Error Handling](/docs/hotchocolate/v16/_leagcy/guides/error-handling).             |
| A mutation with several root fields does not roll back as expected.         | Serial execution was mistaken for a transaction.                                                              | Coordinate transactions in application code or transaction configuration.                                                          |
| `[AllowAnonymous]` does not bypass authorization.                           | The ASP.NET Core attribute may have been imported.                                                            | Use Hot Chocolate's authorization attribute.                                                                                       |
| A payload does not contain `query`.                                         | Relay payload query field configuration is absent or the predicate does not match.                            | See [Query Field in Mutation Payloads](/docs/hotchocolate/v16/build/schema-elements/relay#query-field-in-mutation-payloads).       |

# API reference summary

| API                                       | Purpose                                                                                   | Where to go next                                                                                                         |
| ----------------------------------------- | ----------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------ |
| `[MutationType]`                          | Contributes fields to the root `Mutation` type with implementation-first schema building. | This page.                                                                                                               |
| `[Mutation]`                              | Marks a static method or property as a mutation root field in operation classes.          | Attribute reference or this page when comparing root-field options.                                                      |
| `.AddMutationType<T>()`                   | Registers a descriptor-based mutation root type.                                          | This page.                                                                                                               |
| `.AddMutationConventions(...)`            | Enables generated input and payload conventions.                                          | This page.                                                                                                               |
| `[UseMutationConvention]`                 | Opts a field into conventions or customizes a conventional field.                         | This page.                                                                                                               |
| `[UseMutationConvention(Disable = true)]` | Opts a field out of global conventions.                                                   | This page.                                                                                                               |
| `MutationConventionOptions`               | Configures input, payload, and error naming conventions.                                  | This page.                                                                                                               |
| `[Error]` and `[Error<T>]`                | Declares expected domain errors for payload error generation.                             | [Error Handling](/docs/hotchocolate/v16/_leagcy/guides/error-handling).                                                  |
| `FieldResult<...>`                        | Returns either a success value or a typed domain error value.                             | [Error Handling](/docs/hotchocolate/v16/_leagcy/guides/error-handling).                                                  |
| `[Authorize]`                             | Protects mutation fields.                                                                 | [Authorization](/docs/hotchocolate/v16/build/security/authorization).                                                    |
| `[AllowAnonymous]`                        | Allows public mutation fields.                                                            | [Authorization](/docs/hotchocolate/v16/build/security/authorization).                                                    |
| `.AddDefaultTransactionScopeHandler()`    | Adds default transaction scope handling for mutation requests.                            | Transaction section on this page.                                                                                        |
| `.AddTransactionScopeHandler<T>()`        | Adds custom transaction scope handling.                                                   | Transaction section on this page.                                                                                        |
| `.AddQueryFieldToMutationPayloads(...)`   | Adds a Relay-style query field to payloads.                                               | [Query Field in Mutation Payloads](/docs/hotchocolate/v16/build/schema-elements/relay#query-field-in-mutation-payloads). |

# Next steps

- Use [Arguments](./arguments) to refine resolver parameters, argument names, defaults, and IDs.
- Use [Input Object Types](./input-object-types) to design mutation inputs, records, defaults, `Optional<T>`, and `@oneOf` input choices.
- Use [Object Types](./object-types) to design custom payload objects and child selections.
- Use [Authorization](/docs/hotchocolate/v16/build/security/authorization) to protect write fields.
- Use [Error Handling](/docs/hotchocolate/v16/_leagcy/guides/error-handling) to model typed payload errors.
