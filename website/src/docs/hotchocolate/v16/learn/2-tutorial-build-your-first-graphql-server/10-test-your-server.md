---
title: "Test your server"
description: "Add a small regression test suite for the tutorial server with a schema snapshot, an executor test, and one HTTP integration test."
---

In the previous chapter, you added real-time functionality to your server. Now, your API can read books, paginate results, add new books, and publish subscription events.

This chapter will guide you through adding a focused test suite to protect your work. The aim is not exhaustive coverage, but to catch regressions that would make your API unsafe to change.

By the end, you will have:

- Created a test project for `LibraryServer`
- Added CookieCrumble snapshot assertions
- Captured the GraphQL schema as a reviewed snapshot
- Executed a real GraphQL query without starting the web host
- Sent a GraphQL request through the ASP.NET Core endpoint
- Run `dotnet test` as a checkpoint

# Select the First Tests for Your GraphQL Server

Begin with three essential tests:

| Test                 | What it protects                                                                 |
|----------------------|---------------------------------------------------------------------------------|
| Schema snapshot      | Field names, argument names, type names, nullability, and the public contract    |
| Executor operation   | Parsing, validation, execution, middleware, DataLoader, filtering, paging, wiring|
| HTTP integration     | Hosting, dependency injection, routing, JSON serialization, `/graphql` endpoint  |

Each test covers a different entry point. The schema snapshot guards the contract. The executor test exercises the GraphQL engine without starting Kestrel. The HTTP test interacts with the server as a client would in the next chapter.

Keep the suite focused for now. Unit tests for resolvers are helpful when business logic is involved, but for this tutorial, a few tests that run the GraphQL pipeline and fail on API shape or host wiring changes provide the most value.

# Set Up the Test Project and Dependencies

Open a terminal in the parent directory containing the `LibraryServer` project.

Create an xUnit test project:

```bash
dotnet new xunit --name LibraryServer.Tests
```

Add a reference to your tutorial server:

```bash
dotnet add LibraryServer.Tests reference LibraryServer
```

Add the required packages:

```bash
cd LibraryServer.Tests
dotnet add package CookieCrumble.Xunit
dotnet add package CookieCrumble.HotChocolate
dotnet add package HotChocolate.Types
dotnet add package HotChocolate.Data
dotnet add package HotChocolate.Subscriptions.InMemory
dotnet add package Microsoft.AspNetCore.Mvc.Testing
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
```

Hot Chocolate v16 provides execution APIs in `HotChocolate.Types`. Ensure all Hot Chocolate package versions match those in `LibraryServer.csproj`. If the CLI installs a different version, edit `LibraryServer.Tests.csproj` so all Hot Chocolate packages use the same version.

Build the test project:

```bash
dotnet build
```

You should see:

```text
Build succeeded.
```

The next sections add files under `LibraryServer.Tests`. CookieCrumble will create snapshot files in `__snapshots__` folders beside your test files.

# Share the test executor setup

The schema and executor tests need the same GraphQL setup that `Program.cs` uses. Add one helper so the tests do not drift apart.

Create `GraphQLTestServer.cs` in `LibraryServer.Tests`:

```csharp
using HotChocolate.Execution;
using LibraryServer.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryServer.Tests;

public sealed class GraphQLTestServer : IAsyncDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private ServiceProvider? _services;

    public async Task<IRequestExecutor> CreateExecutorAsync()
    {
        await EnsureServicesAsync();

        return await _services!.GetRequestExecutorAsync();
    }

    private async Task EnsureServicesAsync()
    {
        if (_services is not null)
        {
            return;
        }

        await _connection.OpenAsync();

        var services = new ServiceCollection();

        services.AddDbContext<LibraryDbContext>(
            options => options.UseSqlite(_connection));

        services
            .AddGraphQL()
            .AddFiltering()
            .AddMutationConventions(applyToAllMutations: true)
            .AddInMemorySubscriptions()
            .AddTypes();

        _services = services.BuildServiceProvider();

        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

        await db.Database.EnsureCreatedAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_services is not null)
        {
            await _services.DisposeAsync();
        }

        await _connection.DisposeAsync();
    }
}
```

This helper uses an in-memory SQLite database, keeping the connection open for the test server's lifetime. Your tests get a real relational database with the seed data from `LibraryDbContext`, without writing any files.

The GraphQL builder matches the tutorial's `Program.cs`:

```csharp
.AddFiltering()
.AddMutationConventions(applyToAllMutations: true)
.AddInMemorySubscriptions()
.AddTypes()
```

If your `Program.cs` includes additional GraphQL registrations, add them to `GraphQLTestServer` as needed.

# Add a Schema Snapshot Test

The schema is the contract your clients depend on. A schema snapshot test will catch accidental changes, such as:

- Renaming `books` or `addBook`
- Removing `author` from `Book`
- Changing field nullability
- Dropping paging or filtering arguments
- Losing the `Subscription` root

Create `SchemaTests.cs`:

```csharp
using CookieCrumble.HotChocolate;
using Xunit;

namespace LibraryServer.Tests;

public sealed class SchemaTests
{
    [Fact]
    public async Task Schema_Matches_Snapshot()
    {
        // arrange
        await using var server = new GraphQLTestServer();
        var executor = await server.CreateExecutorAsync();

        // act and assert
        executor.Schema.MatchSnapshot();
    }
}
```

Run the test:

```bash
dotnet test --filter Schema_Matches_Snapshot
```

On the first run, CookieCrumble creates a snapshot baseline. Review the generated SDL before committing. You should see the tutorial's root types and shapes, such as:

```graphql
type Query {
  books(
    first: Int
    after: String
    last: Int
    before: String
    where: BookFilterInput
  ): BooksConnection
  bookById(id: Int!): Book
}

type Mutation {
  addBook(input: AddBookInput!): AddBookPayload!
}

type Subscription {
  onBookAdded: Book!
}
```

Your SDL may include descriptions, directives, generated connection types, and error payload types. The important point is that the schema matches your tutorial server.

If this test fails later, review the diff. If the schema changed intentionally, update the snapshot after reviewing. If the change is unexpected, fix the schema setup or the code that altered the contract.

# Add an executor test for a real operation

Now test one operation through Hot Chocolate's execution pipeline. This does not start the ASP.NET Core host. It still parses the GraphQL document, validates it against the schema, runs middleware, resolves data, and produces a GraphQL response.

Create `BookQueryTests.cs`:

```csharp
using CookieCrumble;
using HotChocolate.Execution;
using Xunit;

namespace LibraryServer.Tests;

public sealed class BookQueryTests
{
    [Fact]
    public async Task GetBooks_Returns_First_Page_With_Authors()
    {
        // arrange
        await using var server = new GraphQLTestServer();
        var executor = await server.CreateExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            OperationRequestBuilder.New()
                .SetDocument(
                    """
                    query GetBooks($first: Int!) {
                      books(first: $first) {
                        nodes {
                          id
                          title
                          author {
                            name
                          }
                        }
                        pageInfo {
                          hasNextPage
                        }
                      }
                    }
                    """)
                .SetVariableValues(new Dictionary<string, object?>
                {
                    ["first"] = 2
                })
                .Build());

        // assert
        Assert.Null(result.ExpectOperationResult().Errors);
        result.MatchSnapshot();
    }
}
```

Run the test:

```bash
dotnet test --filter GetBooks_Returns_First_Page_With_Authors
```

Review the result snapshot. It should contain a stable response with the first two seeded books:

```json
{
  "data": {
    "books": {
      "nodes": [
        {
          "id": 1,
          "title": "The Left Hand of Darkness",
          "author": {
            "name": "Ursula K. Le Guin"
          }
        },
        {
          "id": 2,
          "title": "A Wizard of Earthsea",
          "author": {
            "name": "Ursula K. Le Guin"
          }
        }
      ],
      "pageInfo": {
        "hasNextPage": true
      }
    }
  }
}
```

This test covers several tutorial features at once:

- The `books` field exposes the paged connection shape
- Variables bind to arguments
- EF Core reads the seed data
- The `author` resolver works through DataLoader
- The response contains no GraphQL errors

# Add an integration test for the GraphQL endpoint

The executor test proves the GraphQL pipeline works. Add one host-level test to prove the ASP.NET Core endpoint is reachable.

`WebApplicationFactory` needs access to the app entry point. Open `LibraryServer/Program.cs` and add this declaration at the end of the file:

```csharp
public partial class Program
{
}
```

The partial class declaration makes the top-level program visible to the test project. It does not change the running server.

Create `GraphQLHttpTests.cs` in `LibraryServer.Tests`:

```csharp
using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LibraryServer.Tests;

public sealed class GraphQLHttpTests
{
    [Fact]
    public async Task GraphQLEndpoint_Returns_Data()
    {
        // arrange
        await using var factory = new WebApplicationFactory<global::Program>();
        using var client = factory.CreateClient();

        var request = new
        {
            query = """
                query {
                  bookById(id: 1) {
                    id
                    title
                  }
                }
                """
        };

        // act
        using var response = await client.PostAsJsonAsync("/graphql", request);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"data\"", body);
        Assert.Contains("\"bookById\"", body);
        Assert.DoesNotContain("\"errors\"", body);
    }
}
```

Run the test:

```bash
dotnet test --filter GraphQLEndpoint_Returns_Data
```

You should see:

```text
Passed!  - Failed: 0, Passed: 1
```

This test is intentionally small, as host-level tests are more expensive than executor tests. It confirms that a client can post JSON to `/graphql` and receive a GraphQL response envelope from the running ASP.NET Core app.

If you changed the endpoint path in `Program.cs`, use that path instead of `/graphql`.

# Run All Tests and Review Snapshots

Run the full test project:

```bash
dotnet test
```

You should see:

```text
Passed!  - Failed: 0
```

CookieCrumble snapshot files should now exist under a `__snapshots__` folder in `LibraryServer.Tests`. Commit these snapshot files with your tests. They are the reviewed baselines that make future diffs meaningful.

If a snapshot test fails:

1. Read the diff
2. Decide if the schema or response changed intentionally
3. If the change is correct, update the snapshot as CookieCrumble instructs
4. Run `dotnet test` again

Always review diffs before updating snapshots. A failing snapshot is a prompt for review.

# Checkpoint: Regression Safety for Your Tutorial Server

You have completed this chapter when these checks pass:

- `LibraryServer.Tests` references `LibraryServer`
- `GraphQLTestServer` builds an executor with the same GraphQL features as `Program.cs`
- `Schema_Matches_Snapshot` has a reviewed schema baseline
- `GetBooks_Returns_First_Page_With_Authors` passes and has a reviewed result baseline
- `GraphQLEndpoint_Returns_Data` posts to `/graphql` and receives data
- `dotnet test` succeeds from the test project

This suite now protects the main tutorial contract, a representative read operation, and the HTTP endpoint. It does not cover every resolver, mutation branch, subscription WebSocket behavior, authorization, or production readiness. You can add those as your API grows.

# Troubleshooting Test Failures

Use the symptom to guide your next fix:

| Symptom                                      | Likely cause                                                                 | Fix                                                                                                   |
|-----------------------------------------------|------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------|
| Schema snapshot test fails on first run       | CookieCrumble created the first baseline or asks for approval                | Review the generated SDL, accept the baseline if correct, and rerun the test                          |
| `AddTypes` missing in test project            | Test project cannot see the generated extension method from `LibraryServer`  | Build `LibraryServer`, confirm the project reference, and check Hot Chocolate versions                |
| Executor cannot build                         | Test helper is missing a service registered in `Program.cs`                  | Compare `GraphQLTestServer` with `Program.cs` and add any missing registrations                       |
| Executor test returns GraphQL `errors`        | Operation, variables, or schema differ from the chapter state                | Run the query in Nitro, check field names, update the test or fix the schema                          |
| Executor result order changes                 | `books` resolver no longer orders by `Id`                                    | Restore stable `OrderBy(b => b.Id)` from the pagination chapter                                       |
| HTTP test returns `404`                       | Endpoint path is not `/graphql`                                              | Use the path mapped by `MapGraphQL` in `Program.cs`                                                   |
| HTTP test returns `500`                       | Host starts, but a service or configuration fails during startup or execution | Read the test output, compare startup with `Program.cs`, and keep the test host self-contained         |
| HTTP response has `errors`                    | HTTP succeeded, but GraphQL execution failed                                 | Read the response body and fix the GraphQL operation or resolver error                                |

After each fix, rerun the failing test, then rerun `dotnet test`.

# Next Steps

Continue to the next chapter: [Call from a client](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/11-call-from-a-client/).

For more advanced testing patterns, see [Testing](/docs/hotchocolate/v16/guides/testing/).

For further host customization, refer to Microsoft's [`WebApplicationFactory` integration testing guidance](https://learn.microsoft.com/aspnet/core/test/integration-tests).
