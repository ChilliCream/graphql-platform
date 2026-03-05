using ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices;

internal static partial class BestPracticeData
{
    private static void AddTestingDocuments(List<BestPracticeDocument> docs)
    {
        docs.Add(
            new BestPracticeDocument
            {
                Id = "testing-dataloader",
                Title = "DataLoader Unit Testing",
                Category = BestPracticeCategory.Testing,
                Tags = ["hot-chocolate-16"],
                Styles = ["all"],
                Keywords = "test dataloader unit test batch method verify mock assert xunit",
                Abstract =
                    "How to unit-test the static batch methods produced by [DataLoader] source generation, verifying batching, missing key handling, and error behavior.",
                Body = """
                # DataLoader Unit Testing

                ## When to Use

                Unit-test DataLoader batch methods when you need to verify the data access logic independently from the GraphQL pipeline. Since `[DataLoader]` source generation produces static methods, they are straightforward to test by calling the method directly with test data.

                This is useful for verifying:
                - Correct batching behavior (all requested keys are fetched)
                - Missing key handling (keys that do not exist in the data source)
                - Error handling for database failures
                - Correct dictionary key mapping

                ## Implementation

                ### Testing a Basic DataLoader Method

                ```csharp
                using Microsoft.EntityFrameworkCore;

                namespace MyApp.GraphQL.Tests;

                public class UserDataLoaderTests
                {
                    [Fact]
                    public async Task GetUserById_Returns_All_Requested_Users()
                    {
                        // Arrange
                        await using var dbContext = CreateDbContext();
                        dbContext.Users.AddRange(
                            new User { Id = 1, Name = "Alice", Email = "alice@example.com" },
                            new User { Id = 2, Name = "Bob", Email = "bob@example.com" },
                            new User { Id = 3, Name = "Charlie", Email = "charlie@example.com" });
                        await dbContext.SaveChangesAsync();

                        // Act
                        var result = await UserDataLoaders.GetUserByIdAsync(
                            [1, 2, 3],
                            dbContext,
                            CancellationToken.None);

                        // Assert
                        Assert.Equal(3, result.Count);
                        Assert.Equal("Alice", result[1].Name);
                        Assert.Equal("Bob", result[2].Name);
                        Assert.Equal("Charlie", result[3].Name);
                    }

                    [Fact]
                    public async Task GetUserById_MissingKeys_Returns_Partial_Results()
                    {
                        // Arrange
                        await using var dbContext = CreateDbContext();
                        dbContext.Users.Add(
                            new User { Id = 1, Name = "Alice", Email = "alice@example.com" });
                        await dbContext.SaveChangesAsync();

                        // Act
                        var result = await UserDataLoaders.GetUserByIdAsync(
                            [1, 999],
                            dbContext,
                            CancellationToken.None);

                        // Assert
                        Assert.Single(result);
                        Assert.True(result.ContainsKey(1));
                        Assert.False(result.ContainsKey(999));
                    }

                    [Fact]
                    public async Task GetUserById_EmptyKeys_Returns_Empty_Dictionary()
                    {
                        // Arrange
                        await using var dbContext = CreateDbContext();

                        // Act
                        var result = await UserDataLoaders.GetUserByIdAsync(
                            [],
                            dbContext,
                            CancellationToken.None);

                        // Assert
                        Assert.Empty(result);
                    }

                    private static AppDbContext CreateDbContext()
                    {
                        var options = new DbContextOptionsBuilder<AppDbContext>()
                            .UseInMemoryDatabase("TestDb_" + Guid.NewGuid())
                            .Options;

                        return new AppDbContext(options);
                    }
                }
                ```

                ### Testing a Group DataLoader

                ```csharp
                public class OrderItemDataLoaderTests
                {
                    [Fact]
                    public async Task GetOrderItemsByOrderId_Groups_By_OrderId()
                    {
                        // Arrange
                        await using var dbContext = CreateDbContext();
                        dbContext.OrderItems.AddRange(
                            new OrderItem { Id = 1, OrderId = 100, ProductName = "Widget" },
                            new OrderItem { Id = 2, OrderId = 100, ProductName = "Gadget" },
                            new OrderItem { Id = 3, OrderId = 200, ProductName = "Gizmo" });
                        await dbContext.SaveChangesAsync();

                        // Act
                        var result = await OrderDataLoaders.GetOrderItemsByOrderIdAsync(
                            [100, 200],
                            dbContext,
                            CancellationToken.None);

                        // Assert
                        Assert.Equal(2, result[100].Count());
                        Assert.Single(result[200]);
                    }

                    [Fact]
                    public async Task GetOrderItemsByOrderId_MissingKey_Returns_Empty_Group()
                    {
                        // Arrange
                        await using var dbContext = CreateDbContext();

                        // Act
                        var result = await OrderDataLoaders.GetOrderItemsByOrderIdAsync(
                            [999],
                            dbContext,
                            CancellationToken.None);

                        // Assert
                        Assert.Empty(result[999]);
                    }

                    private static AppDbContext CreateDbContext()
                    {
                        var options = new DbContextOptionsBuilder<AppDbContext>()
                            .UseInMemoryDatabase("TestDb_" + Guid.NewGuid())
                            .Options;

                        return new AppDbContext(options);
                    }
                }
                ```

                ### Testing Composite Key DataLoaders

                ```csharp
                public class CompositeKeyDataLoaderTests
                {
                    [Fact]
                    public async Task GetUserRoleByKey_Returns_Matching_Records()
                    {
                        await using var dbContext = CreateDbContext();
                        dbContext.UserRoles.AddRange(
                            new UserRole { UserId = 1, RoleId = 10 },
                            new UserRole { UserId = 2, RoleId = 20 });
                        await dbContext.SaveChangesAsync();

                        var keys = new[]
                        {
                            new UserRoleKey(1, 10),
                            new UserRoleKey(2, 20),
                            new UserRoleKey(3, 30) // Does not exist
                        };

                        var result = await UserRoleDataLoaders.GetUserRoleByKeyAsync(
                            keys, dbContext, CancellationToken.None);

                        Assert.Equal(2, result.Count);
                        Assert.True(result.ContainsKey(new UserRoleKey(1, 10)));
                        Assert.False(result.ContainsKey(new UserRoleKey(3, 30)));
                    }
                }
                ```

                ## Anti-patterns

                **Testing through the full GraphQL pipeline when unit testing the batch method:**

                ```csharp
                // BAD: Too much ceremony for testing data access logic
                var result = await new ServiceCollection()
                    .AddDbContext<AppDbContext>(...)
                    .AddGraphQLServer()
                    .AddQueryType()
                    .ExecuteRequestAsync("{ users { name } }");
                // Just call the static method directly
                ```

                **Not testing missing key behavior:**

                ```csharp
                // BAD: Only testing the happy path
                [Fact]
                public async Task GetUserById_Works()
                {
                    // Only tests with keys that exist — never tests missing keys
                }
                ```

                ## Key Points

                - Test DataLoader static methods directly by calling them with test data
                - Use in-memory databases with unique names per test for isolation
                - Test three cases: all keys found, partial results (missing keys), and empty key list
                - For group DataLoaders (`ILookup`), verify correct grouping and empty group behavior
                - DataLoader unit tests complement resolver integration tests — use both
                - Use `Guid.NewGuid()` in database names to prevent test interference

                ## Related Practices

                - [testing-resolver] — For integration testing through the pipeline
                - [dataloader-basic] — For DataLoader implementation patterns
                - [dataloader-composite-keys] — For composite key DataLoaders
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "testing-resolver",
                Title = "Resolver Integration Testing",
                Category = BestPracticeCategory.Testing,
                Tags = ["hot-chocolate-16"],
                Styles = ["all"],
                Keywords = "test resolver unit test integration test execute query assert xunit",
                Abstract =
                    "How to write integration tests for resolvers that exercise the full execution pipeline: parsing, validation, execution, and result formatting.",
                Body = """
                # Resolver Integration Testing

                ## When to Use

                Use resolver integration tests when you want to verify the behavior of your GraphQL API through the execution pipeline. Integration tests send a GraphQL query, exercise the full pipeline (parsing, validation, field resolution, result formatting), and assert on the JSON response.

                This is the recommended testing approach for most scenarios because it tests your types, resolvers, and configuration together. Use unit tests only for isolated business logic.

                ## Implementation

                ### Basic Resolver Test

                ```csharp
                using CookieCrumble;
                using HotChocolate.Execution;
                using Microsoft.Extensions.DependencyInjection;

                namespace MyApp.GraphQL.Tests;

                public class UserQueryTests
                {
                    [Fact]
                    public async Task GetUser_Returns_User()
                    {
                        var result = await new ServiceCollection()
                            .AddDbContext<AppDbContext>(o =>
                                o.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()))
                            .AddGraphQLServer()
                            .AddQueryType()
                            .AddTypes()
                            .ExecuteRequestAsync(
                                QueryRequestBuilder.New()
                                    .SetQuery(@"
                                    {
                                        userById(id: 1) {
                                            id
                                            name
                                            email
                                        }
                                    }")
                                    .Create());

                        result.MatchSnapshot();
                    }
                }
                ```

                ### Testing with Seeded Data

                ```csharp
                public class OrderQueryTests
                {
                    [Fact]
                    public async Task GetOrders_Returns_Paginated_Results()
                    {
                        // Arrange: Seed test data
                        var services = new ServiceCollection();
                        services.AddDbContext<AppDbContext>(o =>
                            o.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));

                        var provider = services.BuildServiceProvider();
                        var dbContext = provider.GetRequiredService<AppDbContext>();

                        dbContext.Orders.AddRange(
                            new Order { Id = 1, Status = OrderStatus.Pending, CustomerId = 1 },
                            new Order { Id = 2, Status = OrderStatus.Shipped, CustomerId = 1 },
                            new Order { Id = 3, Status = OrderStatus.Delivered, CustomerId = 2 });
                        await dbContext.SaveChangesAsync();

                        // Act
                        var result = await provider
                            .GetRequiredService<IRequestExecutorResolver>()
                            .GetRequestExecutorAsync()
                            .Result
                            .ExecuteAsync(
                                QueryRequestBuilder.New()
                                    .SetQuery(@"
                                    {
                                        orders(first: 2) {
                                            nodes {
                                                id
                                                status
                                            }
                                            pageInfo {
                                                hasNextPage
                                            }
                                        }
                                    }")
                                    .Create());

                        // Assert
                        result.MatchSnapshot();
                    }
                }
                ```

                ### Testing Mutations

                ```csharp
                public class UserMutationTests
                {
                    [Fact]
                    public async Task CreateUser_Returns_Created_User()
                    {
                        var result = await new ServiceCollection()
                            .AddDbContext<AppDbContext>(o =>
                                o.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()))
                            .AddGraphQLServer()
                            .AddQueryType()
                            .AddMutationType()
                            .AddTypes()
                            .AddMutationConventions()
                            .ExecuteRequestAsync(
                                QueryRequestBuilder.New()
                                    .SetQuery(@"
                                    mutation {
                                        createUser(input: {
                                            name: ""John Doe""
                                            email: ""john@example.com""
                                        }) {
                                            user {
                                                name
                                                email
                                            }
                                        }
                                    }")
                                    .Create());

                        result.MatchSnapshot();
                    }
                }
                ```

                ### Testing Error Cases

                ```csharp
                public class UserMutationTests
                {
                    [Fact]
                    public async Task CreateUser_WithDuplicateEmail_Returns_Error()
                    {
                        var result = await new ServiceCollection()
                            .AddDbContext<AppDbContext>(o =>
                                o.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()))
                            .AddGraphQLServer()
                            .AddQueryType()
                            .AddMutationType()
                            .AddTypes()
                            .AddMutationConventions()
                            .ExecuteRequestAsync(
                                QueryRequestBuilder.New()
                                    .SetQuery(@"
                                    mutation {
                                        createUser(input: {
                                            name: ""Jane""
                                            email: ""existing@example.com""
                                        }) {
                                            user { name }
                                            errors {
                                                ... on EmailAlreadyInUseError {
                                                    message
                                                }
                                            }
                                        }
                                    }")
                                    .Create());

                        result.MatchSnapshot();
                    }
                }
                ```

                ## Anti-patterns

                **Testing resolvers in isolation with mocks:**

                ```csharp
                // BAD: Testing the resolver method directly misses type configuration,
                // middleware, and pipeline behavior
                [Fact]
                public async Task GetUser_DirectCall()
                {
                    var mockLoader = new Mock<IUserByIdDataLoader>();
                    var user = UserQueries.GetUserAsync(1, mockLoader.Object, default);
                    // This does not test the GraphQL pipeline at all
                }
                ```

                **Asserting on raw JSON strings:**

                ```csharp
                // BAD: Brittle assertions break on formatting changes
                var json = result.ToJson();
                Assert.Equal("{\"data\":{\"user\":{\"name\":\"John\"}}}", json);
                // Use snapshot testing instead
                ```

                ## Key Points

                - Use `ExecuteRequestAsync` to test through the full GraphQL pipeline
                - Use CookieCrumble `MatchSnapshot()` for readable assertions on query results
                - Seed test data using in-memory databases or test fixtures
                - Test both success and error paths for mutations
                - Prefer integration tests over unit tests for resolvers — they catch configuration issues
                - Use unique database names per test to avoid test interference

                ## Related Practices

                - [testing-schema-snapshot] — For schema-level snapshot testing
                - [testing-dataloader] — For DataLoader unit testing
                - [testing-snapshot-workflow] — For CookieCrumble workflow
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "testing-schema-snapshot",
                Title = "Schema Snapshot Testing",
                Category = BestPracticeCategory.Testing,
                Tags = ["hot-chocolate-16"],
                Styles = ["all"],
                Keywords = "snapshot test schema export SDL compare diff verify CookieCrumble",
                Abstract =
                    "How to write schema snapshot tests using CookieCrumble to detect accidental breaking changes by capturing the SDL as a versioned snapshot.",
                Body = """
                # Schema Snapshot Testing

                ## When to Use

                Use schema snapshot testing to detect accidental breaking changes to your GraphQL schema. A schema snapshot captures the full SDL (Schema Definition Language) output of your server configuration and stores it as a file. On subsequent test runs, the test compares the current schema against the stored snapshot and fails if there are differences.

                This is essential for:
                - Catching unintended field additions, removals, or type changes
                - Reviewing schema changes during code review (the diff shows exactly what changed)
                - Ensuring schema stability across refactoring

                ## Implementation

                ### Basic Schema Snapshot Test

                ```csharp
                using CookieCrumble;
                using HotChocolate.Execution;
                using Microsoft.Extensions.DependencyInjection;

                namespace MyApp.GraphQL.Tests;

                public class SchemaTests
                {
                    [Fact]
                    public async Task Schema_Should_Match_Snapshot()
                    {
                        var schema = await new ServiceCollection()
                            .AddGraphQLServer()
                            .AddQueryType()
                            .AddMutationType()
                            .AddTypes()
                            .AddFiltering()
                            .AddSorting()
                            .BuildSchemaAsync();

                        schema.MatchSnapshot();
                    }
                }
                ```

                ### Schema Test with Services

                When your schema requires registered services:

                ```csharp
                public class SchemaTests
                {
                    [Fact]
                    public async Task Schema_With_Services_Should_Match_Snapshot()
                    {
                        var schema = await new ServiceCollection()
                            .AddLogging()
                            .AddDbContext<AppDbContext>(o =>
                                o.UseInMemoryDatabase("SchemaTest"))
                            .AddGraphQLServer()
                            .AddQueryType()
                            .AddMutationType()
                            .AddSubscriptionType()
                            .AddTypes()
                            .AddFiltering()
                            .AddSorting()
                            .AddMutationConventions()
                            .BuildSchemaAsync();

                        schema.MatchSnapshot();
                    }
                }
                ```

                ### Updating Snapshots

                When you intentionally change the schema, update the snapshot:

                1. Run the test — it fails with a diff showing the changes
                2. Review the diff to ensure the changes are intentional
                3. Delete the old snapshot file from `__snapshots__/`
                4. Re-run the test — it creates a new snapshot
                5. Commit the updated snapshot file

                ### Project Setup

                Add the CookieCrumble package to your test project:

                ```xml
                <PackageReference Include="CookieCrumble" Version="16.*" />
                <PackageReference Include="HotChocolate.Execution" Version="16.*" />
                <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.*" />
                <PackageReference Include="xunit" Version="2.*" />
                <PackageReference Include="xunit.runner.visualstudio" Version="2.*" />
                ```

                ## Anti-patterns

                **Not including the snapshot file in source control:**

                ```
                # BAD: .gitignore excluding snapshot files
                __snapshots__/
                # Snapshots MUST be committed so CI can detect regressions
                ```

                **Testing schema with mock services that alter the schema:**

                ```csharp
                // BAD: Mock services may not register the same types,
                // producing a different schema than production
                var schema = await new ServiceCollection()
                    .AddSingleton<IUserService>(new FakeUserService())
                    .AddGraphQLServer()
                    .AddQueryType()
                    .BuildSchemaAsync();
                // Use the same service registration as production
                ```

                **Snapshot tests that are too granular:**

                ```csharp
                // BAD: Separate snapshot per type — one change updates many files
                [Fact]
                public async Task UserType_Snapshot() { /* ... */ }

                [Fact]
                public async Task OrderType_Snapshot() { /* ... */ }

                // Use a single schema snapshot test instead
                ```

                ## Key Points

                - Use `schema.MatchSnapshot()` from CookieCrumble to capture and compare SDL snapshots
                - Snapshot files are stored in `__snapshots__/` directories next to the test file
                - Always commit snapshot files to source control so CI detects regressions
                - Review snapshot diffs during code review to catch unintended schema changes
                - Use the same service registration as production for accurate schema capture
                - One schema snapshot test per schema is typically sufficient

                ## Related Practices

                - [testing-resolver] — For resolver integration testing
                - [testing-snapshot-workflow] — For CookieCrumble workflow details
                - [schema-design-naming] — For naming conventions tested by snapshots
                """
            });

        docs.Add(
            new BestPracticeDocument
            {
                Id = "testing-snapshot-workflow",
                Title = "CookieCrumble Snapshot Workflow",
                Category = BestPracticeCategory.Testing,
                Tags = ["hot-chocolate-16"],
                Styles = ["all"],
                Keywords = "snapshot update review approve mismatch CookieCrumble workflow accept reject",
                Abstract =
                    "How to work with the CookieCrumble snapshot testing framework: creating snapshots, updating them, reviewing diffs, and CI integration.",
                Body = """
                # CookieCrumble Snapshot Workflow

                ## When to Use

                Use CookieCrumble for snapshot testing whenever you need to assert on complex output like GraphQL schemas, query results, or error responses. Snapshot testing captures the output on first run and compares against it on subsequent runs, making it easy to detect changes without writing brittle string assertions.

                CookieCrumble is the snapshot testing framework used throughout the Hot Chocolate codebase and is recommended for all Hot Chocolate projects.

                ## Implementation

                ### Creating a Snapshot Test

                ```csharp
                using CookieCrumble;
                using HotChocolate.Execution;
                using Microsoft.Extensions.DependencyInjection;

                namespace MyApp.GraphQL.Tests;

                public class ProductQueryTests
                {
                    [Fact]
                    public async Task GetProducts_Snapshot()
                    {
                        var result = await new ServiceCollection()
                            .AddDbContext<AppDbContext>(o =>
                                o.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()))
                            .AddGraphQLServer()
                            .AddQueryType()
                            .AddTypes()
                            .ExecuteRequestAsync(@"
                            {
                                products(first: 5) {
                                    nodes {
                                        id
                                        name
                                        price
                                    }
                                }
                            }");

                        result.MatchSnapshot();
                    }
                }
                ```

                ### First Run

                On the first run, `MatchSnapshot()` creates a snapshot file:

                ```
                MyApp.GraphQL.Tests/
                  __snapshots__/
                    ProductQueryTests.GetProducts_Snapshot.snap
                ```

                The `.snap` file contains the serialized result.

                ### Reviewing Changes

                When the output changes, the test fails with a diff:

                ```
                Snapshot mismatch:
                - Expected
                + Actual

                  {
                    "data": {
                      "products": {
                        "nodes": [
                          {
                -           "name": "Widget",
                +           "name": "Super Widget",
                            "price": 9.99
                          }
                        ]
                      }
                    }
                  }
                ```

                ### Updating Snapshots

                When changes are intentional:

                1. Review the diff in the test output
                2. Delete the old `.snap` file from `__snapshots__/`
                3. Re-run the test — a new snapshot is created
                4. Commit the updated snapshot file

                ### Named Snapshots

                Use named snapshots when one test method has multiple assertions:

                ```csharp
                [Fact]
                public async Task ProductQuery_Variations()
                {
                    var executor = await new ServiceCollection()
                        .AddDbContext<AppDbContext>(o =>
                            o.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()))
                        .AddGraphQLServer()
                        .AddQueryType()
                        .AddTypes()
                        .BuildRequestExecutorAsync();

                    var allProducts = await executor.ExecuteAsync(@"{ products { nodes { name } } }");
                    allProducts.MatchSnapshot("AllProducts");

                    var filtered = await executor.ExecuteAsync(@"
                        { products(where: { price: { gt: 10 } }) { nodes { name } } }");
                    filtered.MatchSnapshot("ExpensiveProducts");
                }
                ```

                ### Snapshot File Organization

                Snapshot files are stored in `__snapshots__/` next to the test file:

                ```
                src/MyApp.GraphQL.Tests/
                  ProductQueryTests.cs
                  __snapshots__/
                    ProductQueryTests.GetProducts_Snapshot.snap
                    ProductQueryTests.ProductQuery_Variations_AllProducts.snap
                    ProductQueryTests.ProductQuery_Variations_ExpensiveProducts.snap
                ```

                ### CI Integration

                Ensure snapshot files are committed to source control. In CI:

                ```yaml
                - name: Run tests
                  run: dotnet test --no-build
                  # Tests fail if snapshots don't match — no special CI configuration needed
                ```

                ## Anti-patterns

                **Not committing snapshot files:**

                ```
                # BAD in .gitignore:
                *.snap
                __snapshots__/
                # Snapshots MUST be in source control for CI to work
                ```

                **Updating snapshots without reviewing the diff:**

                ```bash
                # BAD: Blindly deleting all snapshots to make tests pass
                rm -rf **/__snapshots__
                # Always review the diff to ensure changes are intentional
                ```

                **Snapshot tests with non-deterministic output:**

                ```csharp
                // BAD: Timestamps, GUIDs, and random values change every run
                result.MatchSnapshot(); // Fails because createdAt is always different
                // Use deterministic test data or snapshot extensions that ignore volatile fields
                ```

                ## Key Points

                - `MatchSnapshot()` creates a snapshot on first run and compares on subsequent runs
                - Snapshot files are stored in `__snapshots__/` directories alongside test files
                - Always commit snapshot files to source control
                - Review diffs carefully before updating snapshots
                - Use named snapshots for multiple assertions in one test
                - Ensure test data is deterministic to avoid flaky snapshot tests
                - Delete the old `.snap` file and re-run to update a snapshot

                ## Related Practices

                - [testing-schema-snapshot] — For schema snapshot testing
                - [testing-resolver] — For resolver integration testing
                - [testing-dataloader] — For DataLoader unit testing
                """
            });
    }
}
