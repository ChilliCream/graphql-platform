using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices;

internal static partial class BestPracticeData
{
    private static void AddDefiningTypesDocuments(List<BestPracticeDocument> docs)
    {
        docs.Add(
            new BestPracticeDocument
            {
                Id = "defining-types-enum",
                Title = "Defining Enum Types",
                Category = BestPracticeCategory.DefiningTypes,
                Tags = ["hot-chocolate-16", "code-first"],
                Styles = ["all"],
                Keywords = "enum enumeration options choices values status flags constants dropdown select",
                Abstract =
                    "How to expose C# enums as GraphQL enum types, including custom naming via [EnumValue] and excluding values via [GraphQLIgnore].",
                Body = """
                # Defining Enum Types

                ## When to Use

                Use GraphQL enum types whenever your schema needs a field with a fixed set of allowed values. C# enums are automatically mapped to GraphQL enum types by Hot Chocolate.

                Common uses include:
                - Status fields (`OrderStatus`, `UserRole`)
                - Filter options (`SortDirection`, `Priority`)
                - Configuration flags (`Visibility`, `AccessLevel`)

                Hot Chocolate applies a naming convention that transforms `PascalCase` C# enum values to `UPPER_SNAKE_CASE` in GraphQL by default.

                ## Implementation

                ### Basic Enum

                ```csharp
                namespace MyApp.Models;

                public enum OrderStatus
                {
                    Pending,
                    Processing,
                    Shipped,
                    Delivered,
                    Cancelled
                }
                ```

                This generates the following GraphQL enum:

                ```graphql
                enum OrderStatus {
                  PENDING
                  PROCESSING
                  SHIPPED
                  DELIVERED
                  CANCELLED
                }
                ```

                ### Custom Value Names

                Use `[GraphQLName]` to override the default naming for specific values:

                ```csharp
                public enum Priority
                {
                    [GraphQLName("LOW")]
                    Low,

                    [GraphQLName("MEDIUM")]
                    Medium,

                    [GraphQLName("HIGH")]
                    High,

                    [GraphQLName("P0_CRITICAL")]
                    Critical
                }
                ```

                ### Excluding Values

                Use `[GraphQLIgnore]` to hide enum values from the GraphQL schema:

                ```csharp
                public enum UserStatus
                {
                    Active,
                    Inactive,
                    Suspended,

                    [GraphQLIgnore]
                    Internal,  // Not exposed to API clients

                    [GraphQLIgnore]
                    Deleted    // Soft-delete state, hidden from API
                }
                ```

                ### Adding Descriptions

                ```csharp
                public enum ShippingMethod
                {
                    [GraphQLDescription("Standard shipping (5-7 business days)")]
                    Standard,

                    [GraphQLDescription("Express shipping (2-3 business days)")]
                    Express,

                    [GraphQLDescription("Next-day delivery")]
                    NextDay
                }
                ```

                ### Using Enums in Types and Inputs

                ```csharp
                public class Order
                {
                    public int Id { get; set; }
                    public OrderStatus Status { get; set; }
                    public ShippingMethod ShippingMethod { get; set; }
                }

                public record CreateOrderInput(
                    int ProductId,
                    int Quantity,
                    ShippingMethod ShippingMethod = ShippingMethod.Standard);

                public record OrderFilterInput(
                    OrderStatus? Status);
                ```

                ### Enum Type Extension for Additional Configuration

                ```csharp
                [EnumType<OrderStatus>]
                public static partial class OrderStatusType
                {
                }
                ```

                ## Anti-patterns

                **Using strings instead of enums:**

                ```csharp
                // BAD: Stringly-typed status field with no type safety
                public class Order
                {
                    public int Id { get; set; }
                    public string Status { get; set; } = "pending"; // No validation
                }
                ```

                **Using [Flags] enums without care:**

                ```csharp
                // BAD: [Flags] enums do not map naturally to GraphQL enums.
                // GraphQL enums are single values, not bitmasks.
                [Flags]
                public enum Permissions
                {
                    None = 0,
                    Read = 1,
                    Write = 2,
                    Delete = 4,
                    Admin = Read | Write | Delete // Combined value — confusing in GraphQL
                }
                // Instead, use a list field: permissions: [Permission!]!
                ```

                **Exposing internal-only values:**

                ```csharp
                // BAD: Internal values leak implementation details to API clients
                public enum TaskStatus
                {
                    Open,
                    InProgress,
                    Done,
                    FailedRetryableInternal,  // Implementation detail
                    QueuedForProcessing       // Implementation detail
                }
                ```

                ## Key Points

                - C# enums are automatically mapped to GraphQL enum types with `UPPER_SNAKE_CASE` naming
                - Use `[GraphQLName]` to customize individual value names
                - Use `[GraphQLIgnore]` to hide values from the schema
                - Use `[GraphQLDescription]` to add documentation to enum values
                - Avoid `[Flags]` enums in GraphQL — use a list field with a non-flags enum instead
                - Keep enum values client-facing — hide internal states with `[GraphQLIgnore]`

                ## Related Practices

                - [defining-types-object] — For object types that use enums
                - [defining-types-input] — For input types with enum fields
                - [schema-design-naming] — For naming conventions
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "defining-types-input",
                Title = "Defining Input Types with InputObjectType<T>",
                Category = BestPracticeCategory.DefiningTypes,
                Tags = ["hot-chocolate-16", "code-first"],
                Styles = ["all"],
                Keywords = "input form request data mutation arguments parameters dto payload create update",
                Abstract =
                    "How to define GraphQL input types using InputObjectType<T>, record types, and the [InputObjectType<T>] attribute. Covers validation and required fields.",
                Body = """
                # Defining Input Types with InputObjectType<T>

                ## When to Use

                Use input types whenever a mutation or query requires structured arguments. GraphQL input types cannot have resolvers or circular references, making them suitable for data that flows from client to server.

                In Hot Chocolate 16, use C# records for input types. Records provide immutability, value-based equality, and concise syntax. For simple cases, the framework automatically maps record properties to input fields without any annotation needed.

                ## Implementation

                ### Simple Record Input

                The most straightforward approach uses a plain record:

                ```csharp
                namespace MyApp.GraphQL.Inputs;

                public record CreateUserInput(
                    string Name,
                    string Email,
                    string? Phone);
                ```

                This automatically generates a GraphQL input type:

                ```graphql
                input CreateUserInput {
                  name: String!
                  email: String!
                  phone: String
                }
                ```

                ### Using in Mutations

                ```csharp
                [MutationType]
                public static class UserMutations
                {
                    public static async Task<User> CreateUserAsync(
                        CreateUserInput input,
                        AppDbContext dbContext,
                        CancellationToken cancellationToken)
                    {
                        var user = new User
                        {
                            Name = input.Name,
                            Email = input.Email,
                            Phone = input.Phone
                        };

                        dbContext.Users.Add(user);
                        await dbContext.SaveChangesAsync(cancellationToken);

                        return user;
                    }
                }
                ```

                ### Input Type Extensions for Customization

                Use `[InputObjectType<T>]` when you need to customize the input type:

                ```csharp
                public record UpdateUserInput(
                    [property: ID] int Id,
                    Optional<string> Name,
                    Optional<string?> Email);

                [InputObjectType<UpdateUserInput>]
                public static partial class UpdateUserInputType
                {
                    // Custom field descriptions or configurations can be added here
                }
                ```

                ### Optional Fields with the Optional Type

                Use `Optional<T>` to distinguish between "not provided" and "provided as null":

                ```csharp
                public record PatchUserInput(
                    [property: ID] int Id,
                    Optional<string> Name,
                    Optional<string?> Bio);

                [MutationType]
                public static class UserMutations
                {
                    public static async Task<User> PatchUserAsync(
                        PatchUserInput input,
                        AppDbContext dbContext,
                        CancellationToken cancellationToken)
                    {
                        var user = await dbContext.Users.FindAsync(input.Id);

                        if (input.Name.HasValue)
                        {
                            user!.Name = input.Name.Value!;
                        }

                        if (input.Bio.HasValue)
                        {
                            user!.Bio = input.Bio.Value;
                        }

                        await dbContext.SaveChangesAsync(cancellationToken);
                        return user!;
                    }
                }
                ```

                ### Default Values

                ```csharp
                public record CreateProductInput(
                    string Name,
                    decimal Price,
                    int Quantity = 0,
                    bool IsActive = true);
                ```

                ## Anti-patterns

                **Using mutable classes for input types:**

                ```csharp
                // BAD: Mutable class with setters is not idiomatic for input types
                public class CreateUserInput
                {
                    public string Name { get; set; } = default!;
                    public string Email { get; set; } = default!;
                }
                ```

                **Using domain entities directly as input types:**

                ```csharp
                // BAD: Exposing the domain entity as an input couples your API
                // to your database schema and may expose sensitive fields
                [MutationType]
                public static class UserMutations
                {
                    public static async Task<User> CreateUser(User user, AppDbContext db)
                    {
                        db.Users.Add(user);
                        await db.SaveChangesAsync();
                        return user;
                    }
                }
                ```

                **Using nullable reference types when Optional is needed:**

                ```csharp
                // BAD: Cannot distinguish "client sent null" from "client did not send field"
                public record UpdateUserInput(int Id, string? Name, string? Bio);
                // If Name is null, was it explicitly set to null or just not provided?
                ```

                ## Key Points

                - Use C# records for input types — they provide immutability and concise syntax
                - Non-nullable record parameters become `!` required fields in GraphQL
                - Nullable parameters (`string?`) become optional fields
                - Use `Optional<T>` to distinguish between "not provided" and "provided as null" in patch/update mutations
                - Use `[InputObjectType<T>]` only when you need to customize the input type beyond what records provide
                - Never use domain entities directly as input types

                ## Related Practices

                - [defining-types-object] — For output object types
                - [error-handling-mutation-conventions] — For mutation error handling
                - [security-authorization] — For authorized mutations
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "defining-types-interface",
                Title = "Defining Interfaces with InterfaceType<T>",
                Category = BestPracticeCategory.DefiningTypes,
                Tags = ["hot-chocolate-16", "code-first"],
                Styles = ["all"],
                Keywords = "interface contract abstraction polymorphism shared fields implements inheritance base",
                Abstract =
                    "How to define GraphQL interface types and implement them on object types using InterfaceType<T> and [InterfaceType<T>]. Covers abstract models and interface resolution.",
                Body = """
                # Defining Interfaces with InterfaceType<T>

                ## When to Use

                Use GraphQL interfaces when multiple object types share a common set of fields and you want clients to query those fields polymorphically. Common examples include:

                - `Node` interface for Relay global object identification
                - `Timestamped` interface for types that have `createdAt` / `updatedAt`
                - `Searchable` interface for types that appear in search results

                Interfaces are appropriate when you have a genuine "is-a" relationship and clients need to query shared fields without knowing the concrete type. If types simply share an implementation detail but clients do not need polymorphic access, do not create an interface.

                ## Implementation

                ### Define the Interface

                ```csharp
                namespace MyApp.Models;

                public interface ITimestamped
                {
                    DateTime CreatedAt { get; }
                    DateTime UpdatedAt { get; }
                }
                ```

                ```csharp
                namespace MyApp.GraphQL.Types;

                [InterfaceType<ITimestamped>]
                public static partial class TimestampedInterfaceType
                {
                }
                ```

                ### Implement on Object Types

                ```csharp
                public class Article : ITimestamped
                {
                    public int Id { get; set; }
                    public string Title { get; set; } = default!;
                    public string Content { get; set; } = default!;
                    public DateTime CreatedAt { get; set; }
                    public DateTime UpdatedAt { get; set; }
                }

                public class Comment : ITimestamped
                {
                    public int Id { get; set; }
                    public string Body { get; set; } = default!;
                    public int ArticleId { get; set; }
                    public DateTime CreatedAt { get; set; }
                    public DateTime UpdatedAt { get; set; }
                }
                ```

                Hot Chocolate automatically detects that `Article` and `Comment` implement `ITimestamped` and adds the interface to their GraphQL types.

                ### Abstract Type with Explicit Resolution

                When the interface is not tied to a C# interface, define it as an abstract class or use explicit type resolution:

                ```csharp
                [InterfaceType]
                public abstract class SearchResult
                {
                    public abstract string Title { get; }
                    public abstract string Excerpt { get; }
                }

                [ObjectType<Article>]
                public static partial class ArticleType
                {
                    [BindMember(nameof(Article.Title))]
                    public static string GetTitle([Parent] Article article)
                        => article.Title;

                    public static string GetExcerpt([Parent] Article article)
                        => article.Content.Length > 200
                            ? article.Content[..200] + "..."
                            : article.Content;
                }
                ```

                ### Querying Interfaces

                The generated schema allows polymorphic queries:

                ```graphql
                query {
                  recentActivity {
                    ... on Article {
                      title
                      content
                    }
                    ... on Comment {
                      body
                      articleId
                    }
                    # Shared fields from interface
                    createdAt
                    updatedAt
                  }
                }
                ```

                ## Anti-patterns

                **Creating interfaces for a single implementing type:**

                ```csharp
                // BAD: An interface with only one implementation adds complexity without value
                [InterfaceType<IUser>]
                public static partial class UserInterfaceType { }

                // Only one type implements it — just use the object type directly
                ```

                **Fat interfaces with many unrelated fields:**

                ```csharp
                // BAD: Too many fields — violates interface segregation
                public interface IEntity
                {
                    int Id { get; }
                    string Name { get; }
                    DateTime CreatedAt { get; }
                    DateTime UpdatedAt { get; }
                    string CreatedBy { get; }
                    bool IsActive { get; }
                    int Version { get; }
                }
                ```

                ## Key Points

                - Use `[InterfaceType<T>]` on static partial classes to define GraphQL interfaces from C# interfaces
                - Hot Chocolate automatically recognizes when a type implements a C# interface and adds it to the schema
                - Interfaces allow clients to query shared fields polymorphically with inline fragments for type-specific fields
                - Only create interfaces when clients genuinely need polymorphic access
                - Keep interfaces focused — prefer small, cohesive interfaces over large ones

                ## Related Practices

                - [defining-types-object] — For concrete object types
                - [defining-types-union] — For union types without shared fields
                - [schema-design-relay] — For the Node interface pattern
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "defining-types-object",
                Title = "Defining Object Types with ObjectType<T>",
                Category = BestPracticeCategory.DefiningTypes,
                Tags = ["hot-chocolate-16", "code-first"],
                Styles = ["all"],
                Keywords = "object type entity model class output record schema define create",
                Abstract =
                    "How to use ObjectType<T> and [ObjectType<T>] type extensions to define GraphQL object types from C# classes, including pure resolvers and field configuration.",
                Body = """
                # Defining Object Types with ObjectType<T>

                ## When to Use

                Use `[ObjectType<T>]` type extensions to define GraphQL object types in Hot Chocolate 16. This is the recommended pattern for all object types, whether they represent domain entities, DTOs, or query root types.

                Type extensions allow you to separate your GraphQL layer from your domain model. The C# class defines the shape of the data, and the type extension adds GraphQL-specific behavior like computed fields, renamed fields, and resolver logic.

                For simple cases where the C# class maps directly to the GraphQL type with no customization, you can annotate the class with `[ObjectType]` directly.

                ## Implementation

                ### Basic Type Extension

                ```csharp
                namespace MyApp.Models;

                public class User
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = default!;
                    public string Email { get; set; } = default!;
                    public int DepartmentId { get; set; }
                }
                ```

                ```csharp
                namespace MyApp.GraphQL.Types;

                [ObjectType<User>]
                public static partial class UserType
                {
                    public static async Task<Department?> GetDepartmentAsync(
                        [Parent] User user,
                        IDepartmentByIdDataLoader dataLoader,
                        CancellationToken cancellationToken)
                    {
                        return await dataLoader.LoadAsync(user.DepartmentId, cancellationToken);
                    }
                }
                ```

                ### Adding Computed Fields

                ```csharp
                [ObjectType<User>]
                public static partial class UserType
                {
                    public static string GetDisplayName([Parent] User user)
                        => $"{user.Name} ({user.Email})";
                }
                ```

                ### Ignoring Fields

                ```csharp
                [ObjectType<User>]
                public static partial class UserType
                {
                    [GraphQLIgnore]
                    public static int GetDepartmentId([Parent] User user)
                        => user.DepartmentId;
                }
                ```

                Or use `[GraphQLIgnore]` directly on the model property:

                ```csharp
                public class User
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = default!;
                    public string Email { get; set; } = default!;

                    [GraphQLIgnore]
                    public string PasswordHash { get; set; } = default!;
                }
                ```

                ### Direct Annotation (Simple Types)

                For types that need no customization, annotate the class directly:

                ```csharp
                [ObjectType]
                public class Address
                {
                    public string Street { get; set; } = default!;
                    public string City { get; set; } = default!;
                    public string ZipCode { get; set; } = default!;
                }
                ```

                ### Registering Types

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes();
                ```

                The `AddTypes()` method discovers all types annotated with source generation attributes in the assembly.

                ## Anti-patterns

                **Putting resolver logic directly in the domain model:**

                ```csharp
                // BAD: Domain model should not depend on GraphQL infrastructure
                public class User
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = default!;

                    public async Task<Department> GetDepartment(
                        IDepartmentByIdDataLoader loader,
                        CancellationToken ct)
                    {
                        return await loader.LoadAsync(DepartmentId, ct);
                    }
                }
                ```

                **Creating non-static type extension classes:**

                ```csharp
                // BAD: Type extensions should be static partial classes
                [ObjectType<User>]
                public partial class UserType  // Missing 'static'
                {
                    public string GetDisplayName([Parent] User user) => user.Name;
                }
                ```

                ## Key Points

                - Use `[ObjectType<T>]` on static partial classes for type extensions that add resolvers or computed fields
                - Use `[ObjectType]` on the class directly only for simple types with no additional resolvers
                - Type extension methods receive the parent object via `[Parent]` parameter
                - Use `[GraphQLIgnore]` to exclude properties from the schema
                - Call `AddTypes()` on the GraphQL server builder to auto-discover annotated types

                ## Related Practices

                - [defining-types-input] — For input types
                - [resolvers-field] — For field resolver patterns
                - [resolvers-parent] — For parent object access
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "defining-types-union",
                Title = "Defining Union Types",
                Category = BestPracticeCategory.DefiningTypes,
                Tags = ["hot-chocolate-16", "code-first"],
                Styles = ["all"],
                Keywords = "union polymorphism discriminated oneOf either or result search multiple types",
                Abstract =
                    "How to define GraphQL union types, including use with mutation error unions and discriminated union patterns using [UnionType].",
                Body = """
                # Defining Union Types

                ## When to Use

                Use GraphQL union types when a field can return one of several unrelated types that do not share common fields. Unlike interfaces, unions do not require shared fields between members.

                Common use cases include:
                - Search results that return different entity types
                - Mutation payloads with typed errors using mutation conventions
                - Activity feeds with heterogeneous event types
                - Polymorphic content blocks in a CMS

                If the types share common fields that clients need to query without type checks, use an interface instead.

                ## Implementation

                ### Marker Interface Pattern

                The simplest approach uses a marker interface:

                ```csharp
                namespace MyApp.Models;

                [UnionType("SearchResult")]
                public interface ISearchResult { }

                public class Article : ISearchResult
                {
                    public int Id { get; set; }
                    public string Title { get; set; } = default!;
                    public string Content { get; set; } = default!;
                }

                public class Product : ISearchResult
                {
                    public int Id { get; set; }
                    public string Name { get; set; } = default!;
                    public decimal Price { get; set; }
                }

                public class User : ISearchResult
                {
                    public int Id { get; set; }
                    public string DisplayName { get; set; } = default!;
                }
                ```

                ### Query Returning a Union

                ```csharp
                [QueryType]
                public static class SearchQueries
                {
                    public static async Task<IReadOnlyList<ISearchResult>> SearchAsync(
                        string query,
                        SearchService searchService,
                        CancellationToken cancellationToken)
                    {
                        return await searchService.SearchAsync(query, cancellationToken);
                    }
                }
                ```

                Clients query union types using inline fragments:

                ```graphql
                query {
                  search(query: "graphql") {
                    ... on Article {
                      id
                      title
                      content
                    }
                    ... on Product {
                      id
                      name
                      price
                    }
                    ... on User {
                      id
                      displayName
                    }
                  }
                }
                ```

                ### Union with Mutation Conventions

                Hot Chocolate's mutation conventions use unions to return typed errors:

                ```csharp
                [MutationType]
                public static class UserMutations
                {
                    [Error<UserNotFoundError>]
                    [Error<EmailAlreadyInUseError>]
                    public static async Task<User> UpdateUserEmailAsync(
                        int userId,
                        string newEmail,
                        AppDbContext dbContext,
                        CancellationToken cancellationToken)
                    {
                        var user = await dbContext.Users.FindAsync(userId);

                        if (user is null)
                        {
                            throw new UserNotFoundError(userId);
                        }

                        var existing = await dbContext.Users
                            .AnyAsync(u => u.Email == newEmail, cancellationToken);

                        if (existing)
                        {
                            throw new EmailAlreadyInUseError(newEmail);
                        }

                        user.Email = newEmail;
                        await dbContext.SaveChangesAsync(cancellationToken);
                        return user;
                    }
                }

                public class UserNotFoundError(int userId)
                {
                    public string Message => $"User with ID {userId} was not found.";
                }

                public class EmailAlreadyInUseError(string email)
                {
                    public string Message => $"Email '{email}' is already in use.";
                }
                ```

                ### Abstract Base Class Pattern

                ```csharp
                [UnionType("ContentBlock")]
                public abstract class ContentBlock { }

                public class TextBlock : ContentBlock
                {
                    public string Text { get; set; } = default!;
                }

                public class ImageBlock : ContentBlock
                {
                    public string Url { get; set; } = default!;
                    public string? AltText { get; set; }
                }

                public class CodeBlock : ContentBlock
                {
                    public string Code { get; set; } = default!;
                    public string Language { get; set; } = default!;
                }
                ```

                ## Anti-patterns

                **Using a single catch-all type with a discriminator:**

                ```csharp
                // BAD: Stringly-typed discriminator — loses type safety
                public class SearchResult
                {
                    public string Type { get; set; } = default!; // "article", "product", "user"
                    public string? Title { get; set; }
                    public string? Name { get; set; }
                    public decimal? Price { get; set; }
                    // Many nullable fields, most are null for any given result
                }
                ```

                **Creating a union with only one member:**

                ```csharp
                // BAD: A union with one type is pointless — just use the type directly
                [UnionType("Result")]
                public interface IResult { }

                public class SuccessResult : IResult
                {
                    public string Message { get; set; } = default!;
                }
                // Only one implementation — no need for a union
                ```

                ## Key Points

                - Use `[UnionType("Name")]` on marker interfaces or abstract base classes to define union types
                - Union members do not need to share any fields — clients use inline fragments to access type-specific fields
                - Mutation conventions (`[Error<T>]`) automatically create error union types
                - Prefer interfaces over unions when types share common fields that clients query polymorphically
                - Each member type of a union must be a distinct object type

                ## Related Practices

                - [defining-types-interface] — For types with shared fields
                - [error-handling-mutation-conventions] — For mutation error unions
                - [defining-types-object] — For the member object types
                """
            });
    }
}
