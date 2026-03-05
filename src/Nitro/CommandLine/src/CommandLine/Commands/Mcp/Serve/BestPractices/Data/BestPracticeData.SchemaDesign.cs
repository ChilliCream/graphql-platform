using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices;

internal static partial class BestPracticeData
{
    private static void AddSchemaDesignDocuments(List<BestPracticeDocument> docs)
    {
        docs.Add(
            new BestPracticeDocument
            {
                Id = "schema-design-naming",
                Title = "Naming Conventions for GraphQL Fields",
                Category = BestPracticeCategory.SchemaDesign,
                Tags = ["hot-chocolate-16", "code-first"],
                Styles = ["all"],
                Keywords = "naming convention camelCase PascalCase method field name transformation rename",
                Abstract =
                    "Hot Chocolate naming transformation rules: how C# method names become GraphQL field names. Conventions for queries, mutations, types, and enums.",
                Body = """
                # Naming Conventions for GraphQL Fields

                ## When to Use

                Understanding naming conventions is essential for every Hot Chocolate project. Hot Chocolate automatically transforms C# names to GraphQL-idiomatic names. Knowing these rules helps you predict the generated schema and name your C# methods accordingly.

                These conventions apply to all type extensions, query types, mutation types, and input types.

                ## Implementation

                ### Field Name Transformation

                Hot Chocolate converts C# method and property names to `camelCase` GraphQL fields:

                | C# Name | GraphQL Name |
                |---|---|
                | `Name` (property) | `name` |
                | `GetUsers` (method) | `users` |
                | `GetUserById` (method) | `userById` |
                | `IsActive` (property) | `isActive` |

                The `Get` prefix is stripped from method names automatically.

                ### Query and Mutation Naming

                ```csharp
                [QueryType]
                public static class Query
                {
                    // GraphQL: users
                    public static IQueryable<User> GetUsers(AppDbContext db) => db.Users;

                    // GraphQL: userById
                    public static async Task<User?> GetUserByIdAsync(
                        int id,
                        IUserByIdDataLoader loader,
                        CancellationToken ct)
                        => await loader.LoadAsync(id, ct);
                }

                [MutationType]
                public static class Mutation
                {
                    // GraphQL: createUser
                    public static async Task<User> CreateUserAsync(
                        CreateUserInput input,
                        AppDbContext db,
                        CancellationToken ct)
                    {
                        // ...
                    }

                    // GraphQL: deleteUser
                    public static async Task<bool> DeleteUserAsync(
                        int id,
                        AppDbContext db,
                        CancellationToken ct)
                    {
                        // ...
                    }
                }
                ```

                ### Type Names

                C# class names become GraphQL type names directly (no transformation):

                | C# Class | GraphQL Type |
                |---|---|
                | `User` | `User` |
                | `CreateUserInput` | `CreateUserInput` |
                | `OrderStatus` | `OrderStatus` |

                ### Enum Value Naming

                Enum values are converted to `UPPER_SNAKE_CASE`:

                | C# Enum Value | GraphQL Enum Value |
                |---|---|
                | `Pending` | `PENDING` |
                | `InProgress` | `IN_PROGRESS` |
                | `PaymentFailed` | `PAYMENT_FAILED` |

                ### Overriding Names

                Use `[GraphQLName]` to override any automatic naming:

                ```csharp
                [QueryType]
                public static class Query
                {
                    [GraphQLName("me")]
                    public static async Task<User?> GetCurrentUserAsync(
                        [GlobalState("UserId")] string userId,
                        IUserByIdDataLoader loader,
                        CancellationToken ct)
                        => await loader.LoadAsync(int.Parse(userId), ct);
                }
                ```

                ```csharp
                [GraphQLName("ProductItem")]
                public class Product
                {
                    public int Id { get; set; }

                    [GraphQLName("sku")]
                    public string ProductCode { get; set; } = default!;
                }
                ```

                ### Argument Naming

                Method parameters become `camelCase` arguments:

                ```csharp
                // GraphQL: userById(id: Int!): User
                public static async Task<User?> GetUserByIdAsync(
                    int id,                           // Argument: id
                    IUserByIdDataLoader loader,       // Not an argument (service)
                    CancellationToken ct)             // Not an argument (special)
                ```

                ## Anti-patterns

                **Using GraphQL naming in C# code:**

                ```csharp
                // BAD: Using camelCase in C# to match GraphQL output
                public static IQueryable<User> users(AppDbContext db) => db.Users;
                // Use PascalCase C# conventions — Hot Chocolate transforms automatically
                ```

                **Inconsistent naming patterns:**

                ```csharp
                // BAD: Mixing naming patterns for similar operations
                public static Task<User?> GetUserByIdAsync(...) { }     // userById
                public static Task<Order?> FetchOrderAsync(...) { }      // fetchOrder (not getOrder)
                public static Task<Product?> RetrieveProductAsync(...) { } // retrieveProduct

                // Use consistent Get* prefix for all queries
                ```

                **Redundant type suffixes:**

                ```csharp
                // BAD: Type suffix is redundant in GraphQL
                public class UserType        // GraphQL: UserType (not User)
                {
                    public string Name { get; set; }
                }
                // Name the class 'User', not 'UserType'
                ```

                ## Key Points

                - Hot Chocolate automatically converts C# `PascalCase` to GraphQL `camelCase` for fields
                - Method `Get` prefix is stripped: `GetUsers` becomes `users`
                - Enum values are converted to `UPPER_SNAKE_CASE`: `InProgress` becomes `IN_PROGRESS`
                - Type names are not transformed: C# `User` becomes GraphQL `User`
                - Use `[GraphQLName("name")]` to override automatic naming when needed
                - Follow consistent naming patterns across all queries and mutations

                ## Related Practices

                - [defining-types-object] — For object type definitions
                - [defining-types-enum] — For enum naming
                - [schema-design-nullability] — For nullability conventions
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "schema-design-nullability",
                Title = "Nullability Design in GraphQL Schemas",
                Category = BestPracticeCategory.SchemaDesign,
                Tags = ["hot-chocolate-16", "code-first"],
                Styles = ["all"],
                Keywords = "nullable null required optional non-null default value NRT reference type question mark",
                Abstract =
                    "How to design non-nullable fields correctly, when nullable is appropriate, and how Hot Chocolate's nullability settings interact with C# nullable reference types.",
                Body = """
                # Nullability Design in GraphQL Schemas

                ## When to Use

                Nullability design is one of the most important schema decisions. Every field in your GraphQL schema is either nullable or non-nullable, and this decision affects client code generation, error behavior, and API reliability.

                Hot Chocolate 16 uses C# nullable reference types (NRT) to determine GraphQL nullability. A non-nullable C# property produces a non-nullable GraphQL field (`String!`), and a nullable property produces a nullable field (`String`).

                Understanding these rules is essential for designing a schema that is both correct and user-friendly.

                ## Implementation

                ### Default Nullability Mapping

                ```csharp
                public class User
                {
                    public int Id { get; set; }           // Int! (value types are non-nullable)
                    public string Name { get; set; }      // String! (NRT: non-nullable reference)
                    public string? Bio { get; set; }      // String (nullable)
                    public int? Age { get; set; }         // Int (Nullable<int>)
                    public DateTime CreatedAt { get; set; } // DateTime! (value type)
                }
                ```

                Generated schema:

                ```graphql
                type User {
                  id: Int!
                  name: String!
                  bio: String
                  age: Int
                  createdAt: DateTime!
                }
                ```

                ### When to Make Fields Non-Nullable

                Make a field non-nullable (`!`) when:

                - The value is always present and has no reason to be absent
                - It is a primary key or required business field
                - It is computed and always returns a value

                ```csharp
                public class Product
                {
                    [ID]
                    public int Id { get; set; }           // Always present
                    public string Name { get; set; }      // Required business field
                    public decimal Price { get; set; }    // Always has a value
                    public ProductStatus Status { get; set; } // Always has a status
                }
                ```

                ### When to Make Fields Nullable

                Make a field nullable when:

                - The value might not exist (e.g., optional profile fields)
                - The resolver might fail and you want partial results
                - The field represents a relationship that may not exist
                - The data may not be available due to authorization

                ```csharp
                [ObjectType<User>]
                public static partial class UserType
                {
                    // Nullable: user may not have a manager
                    public static async Task<User?> GetManagerAsync(
                        [Parent] User user,
                        IUserByIdDataLoader loader,
                        CancellationToken ct)
                    {
                        if (user.ManagerId is null) return null;
                        return await loader.LoadAsync(user.ManagerId.Value, ct);
                    }
                }
                ```

                ### List Nullability

                Lists have three levels of nullability:

                ```csharp
                // [String!]! — non-nullable list of non-nullable strings
                public List<string> Tags { get; set; } = [];

                // [String!] — nullable list of non-nullable strings
                public List<string>? Tags { get; set; }

                // [String]! — non-nullable list of nullable strings
                public List<string?> Tags { get; set; } = [];
                ```

                ### Resolver Nullability

                Return type of resolvers determines field nullability:

                ```csharp
                [ObjectType<Order>]
                public static partial class OrderType
                {
                    // Non-nullable: Task<User> → User!
                    public static async Task<User> GetCustomerAsync(
                        [Parent] Order order,
                        IUserByIdDataLoader loader,
                        CancellationToken ct)
                        => (await loader.LoadAsync(order.CustomerId, ct))!;

                    // Nullable: Task<User?> → User
                    public static async Task<User?> GetAssignedAgentAsync(
                        [Parent] Order order,
                        IUserByIdDataLoader loader,
                        CancellationToken ct)
                    {
                        if (order.AssignedAgentId is null) return null;
                        return await loader.LoadAsync(order.AssignedAgentId.Value, ct);
                    }
                }
                ```

                ## Anti-patterns

                **Making everything nullable as a safety net:**

                ```csharp
                // BAD: All nullable fields provide no contract to clients
                public class User
                {
                    public int? Id { get; set; }       // ID should never be null
                    public string? Name { get; set; }  // Required field should not be nullable
                    public string? Email { get; set; } // Required field should not be nullable
                }
                ```

                **Making relationship fields non-nullable when they can fail:**

                ```csharp
                // BAD: If the DataLoader returns null, a non-nullable field causes
                // the entire parent object to become null (null propagation)
                [ObjectType<Order>]
                public static partial class OrderType
                {
                    public static async Task<User> GetCustomerAsync(  // Non-nullable!
                        [Parent] Order order,
                        IUserByIdDataLoader loader,
                        CancellationToken ct)
                    {
                        return (await loader.LoadAsync(order.CustomerId, ct))!;
                        // If customer was deleted, this throws or returns null,
                        // which nullifies the entire Order object
                    }
                }
                ```

                **Ignoring null propagation behavior:**

                ```csharp
                // BAD: In GraphQL, if a non-nullable field resolves to null,
                // the null propagates up to the nearest nullable parent.
                // This can cause entire sections of the response to become null.
                ```

                ## Key Points

                - Hot Chocolate uses C# nullable reference types to determine GraphQL nullability
                - Non-nullable C# types produce non-nullable GraphQL fields (`!`)
                - Make fields non-nullable when the value is always guaranteed
                - Make fields nullable when the value may be absent or when resilience matters
                - Be aware of null propagation: a null non-nullable field nullifies its parent
                - For lists, consider nullability at both the list and item level
                - Enable `<Nullable>enable</Nullable>` in your project for correct NRT behavior

                ## Related Practices

                - [schema-design-naming] — For naming conventions
                - [schema-design-relay] — For ID field design
                - [error-handling-mutation-conventions] — For nullable error payloads
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "schema-design-relay",
                Title = "Relay Global Object Identification",
                Category = BestPracticeCategory.SchemaDesign,
                Tags = ["hot-chocolate-16", "relay"],
                Styles = ["all"],
                Keywords = "relay node global id connection specification cursor identifier opaque unique refetch",
                Abstract =
                    "How to implement the Relay node interface using [Node], [ID], and node resolver methods. Covers ID encoding, the node query, and Relay compliance.",
                Body = """
                # Relay Global Object Identification

                ## When to Use

                Implement the Relay Global Object Identification specification when:

                - Your clients use Relay or any framework that relies on the `node` query for cache management
                - You want a consistent, globally unique ID scheme across all types
                - You want to support the `node(id: ID!)` and `nodes(ids: [ID!]!)` root queries

                The Relay spec requires that every node type implements the `Node` interface with a globally unique `id` field, and the schema provides `node` and `nodes` root queries for re-fetching any object by its ID.

                ## Implementation

                ### Basic Node Type

                ```csharp
                [ObjectType<User>]
                public static partial class UserType
                {
                    [NodeResolver]
                    public static async Task<User?> GetAsync(
                        int id,
                        IUserByIdDataLoader dataLoader,
                        CancellationToken cancellationToken)
                    {
                        return await dataLoader.LoadAsync(id, cancellationToken);
                    }
                }

                public class User
                {
                    [ID]
                    public int Id { get; set; }
                    public string Name { get; set; } = default!;
                    public string Email { get; set; } = default!;
                }
                ```

                The `[ID]` attribute tells Hot Chocolate to encode the `Id` as a globally unique, opaque Relay ID. The `[NodeResolver]` method enables the `node` query to re-fetch this type.

                ### Generated Schema

                ```graphql
                type Query {
                  node(id: ID!): Node
                  nodes(ids: [ID!]!): [Node]!
                }

                interface Node {
                  id: ID!
                }

                type User implements Node {
                  id: ID!
                  name: String!
                  email: String!
                }
                ```

                ### Using [ID] on Arguments

                Reference node IDs in arguments:

                ```csharp
                [QueryType]
                public static class Query
                {
                    public static async Task<User?> GetUserAsync(
                        [ID(nameof(User))] int id,
                        IUserByIdDataLoader dataLoader,
                        CancellationToken cancellationToken)
                    {
                        return await dataLoader.LoadAsync(id, cancellationToken);
                    }
                }
                ```

                The `[ID(nameof(User))]` ensures the argument accepts a User-encoded Relay ID and decodes it to the raw `int` before the resolver executes.

                ### Enable Node Queries

                ```csharp
                builder.Services
                    .AddGraphQLServer()
                    .AddQueryType()
                    .AddTypes()
                    .AddGlobalObjectIdentification();
                ```

                ### Multiple Node Types

                ```csharp
                [ObjectType<Product>]
                public static partial class ProductType
                {
                    [NodeResolver]
                    public static async Task<Product?> GetAsync(
                        int id,
                        IProductByIdDataLoader dataLoader,
                        CancellationToken cancellationToken)
                    {
                        return await dataLoader.LoadAsync(id, cancellationToken);
                    }
                }

                [ObjectType<Order>]
                public static partial class OrderType
                {
                    [NodeResolver]
                    public static async Task<Order?> GetAsync(
                        int id,
                        IOrderByIdDataLoader dataLoader,
                        CancellationToken cancellationToken)
                    {
                        return await dataLoader.LoadAsync(id, cancellationToken);
                    }
                }
                ```

                ### Client Usage

                ```graphql
                # Re-fetch any node by its global ID
                query {
                  node(id: "VXNlcjox") {
                    ... on User {
                      name
                      email
                    }
                  }
                }

                # Batch re-fetch multiple nodes
                query {
                  nodes(ids: ["VXNlcjox", "T3JkZXI6NDI="]) {
                    ... on User { name }
                    ... on Order { status }
                  }
                }
                ```

                ## Anti-patterns

                **Exposing raw database IDs:**

                ```csharp
                // BAD: Raw integer IDs are not globally unique
                public class User
                {
                    public int Id { get; set; } // No [ID] — exposed as Int!, not ID!
                }
                // User ID 1 and Order ID 1 are indistinguishable
                ```

                **Decoding IDs manually:**

                ```csharp
                // BAD: Manual Base64 decoding of Relay IDs
                public static async Task<User?> GetUserAsync(string encodedId, ...)
                {
                    var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(encodedId));
                    var id = int.Parse(decoded.Split(':')[1]);
                    // Use [ID(nameof(User))] instead — it handles decoding automatically
                }
                ```

                **Forgetting the NodeResolver:**

                ```csharp
                // BAD: Type has [ID] but no node resolver — node query cannot re-fetch it
                public class Product
                {
                    [ID]
                    public int Id { get; set; }
                }
                // Without [NodeResolver], this type is not queryable via the node field
                ```

                ## Key Points

                - Use `[ID]` on entity ID properties to generate opaque, globally unique Relay IDs
                - Use `[NodeResolver]` to define how each type is re-fetched by the `node` query
                - Use `[ID(nameof(TypeName))]` on arguments to decode Relay IDs automatically
                - Call `AddGlobalObjectIdentification()` to enable the `node` and `nodes` root queries
                - Every node type must implement the `Node` interface with a globally unique `id: ID!`
                - Relay IDs are opaque to clients — they should not parse or construct them

                ## Related Practices

                - [defining-types-object] — For object type definitions
                - [defining-types-interface] — For interface types including Node
                - [pagination-cursor] — For Relay connection types
                """
            });
    }
}
