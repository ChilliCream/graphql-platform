---
title: "MCP Adapter"
---

The MCP (Model Context Protocol) adapter exposes your Hot Chocolate GraphQL schema as MCP tools, prompts, and resources. AI agents and LLMs that support MCP can discover and call your GraphQL operations without any manual tool definition. The adapter reads your schema, converts GraphQL operations into MCP tool definitions with full input and output JSON schemas, and serves them over the MCP protocol.

This is useful when you want AI assistants to interact with your existing GraphQL API. Instead of writing custom integrations, you register the adapter and it handles the translation between MCP and GraphQL automatically.

# Setup

Install the `HotChocolate.Adapters.Mcp` package:

```bash
dotnet add package HotChocolate.Adapters.Mcp
```

Register the MCP adapter on your GraphQL server and map the MCP endpoint:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddMcp()
    .AddMcpStorage(myStorage);

var app = builder.Build();

app.UseRouting();
app.UseEndpoints(endpoints => endpoints.MapGraphQLMcp());

app.Run();
```

`AddMcp()` registers the MCP protocol handlers and directive types. `AddMcpStorage()` provides the tool and prompt definitions. `MapGraphQLMcp()` maps the MCP endpoint at `/graphql/mcp` by default.

# Tool Definitions

Each MCP tool is defined by a GraphQL operation document. You create an `OperationToolDefinition` with a parsed GraphQL document that contains exactly one operation:

```csharp
// Services/MyMcpStorage.cs
using HotChocolate.Adapters.Mcp.Storage;
using HotChocolate.Language;

var tool = new OperationToolDefinition(
    Utf8GraphQLParser.Parse(
        """
        query GetBooks {
            books {
                title
            }
        }
        """));
```

The adapter derives the tool name from the operation name, converting it to `snake_case`. The operation `GetBooks` becomes the MCP tool `get_books`. The operation's variable definitions become the tool's input parameters, and the selected fields define the output schema.

# How It Works

The adapter translates between GraphQL and MCP concepts:

| GraphQL Concept       | MCP Concept                           |
| --------------------- | ------------------------------------- |
| Query operation       | Read-only tool (`readOnlyHint: true`) |
| Mutation operation    | Tool (`readOnlyHint: false`)          |
| Operation variables   | Tool input parameters (JSON Schema)   |
| Selected fields       | Tool output schema (JSON Schema)      |
| Operation name        | Tool name (converted to `snake_case`) |
| Operation description | Tool description                      |

When an AI agent calls an MCP tool, the adapter takes the JSON arguments, maps them to GraphQL variables, executes the operation against your schema, and returns the result as structured JSON content. The response includes both `data` and `errors` fields, following the standard GraphQL response format.

# Storage

The `IMcpStorage` interface provides tool and prompt definitions to the adapter. You implement this interface to load definitions from any source: a file system, database, or in-memory collection.

```csharp
// Services/FileMcpStorage.cs
using HotChocolate.Adapters.Mcp.Storage;

public class FileMcpStorage : IMcpStorage
{
    public async ValueTask<IEnumerable<OperationToolDefinition>>
        GetOperationToolDefinitionsAsync(
            CancellationToken cancellationToken = default)
    {
        // Load tool definitions from your preferred source.
        var graphql = await File.ReadAllTextAsync(
            "tools/get-books.graphql", cancellationToken);
        var document = Utf8GraphQLParser.Parse(graphql);
        return [new OperationToolDefinition(document)];
    }

    public ValueTask<IEnumerable<PromptDefinition>>
        GetPromptDefinitionsAsync(
            CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(
            Enumerable.Empty<PromptDefinition>());
    }

    // IMcpStorage also extends IObservable for both tool and
    // prompt events, enabling hot-reload when definitions change.
    public IDisposable Subscribe(
        IObserver<OperationToolStorageEventArgs> observer)
        => /* your subscription logic */;

    public IDisposable Subscribe(
        IObserver<PromptStorageEventArgs> observer)
        => /* your subscription logic */;
}
```

The storage implements `IObservable<OperationToolStorageEventArgs>` and `IObservable<PromptStorageEventArgs>`. When you push change events through these observables, connected MCP clients receive a `tools/list_changed` or `prompts/list_changed` notification. This enables hot-reload: you can add, update, or remove tools at runtime without restarting the server.

# Tool Annotations

MCP tool annotations provide hints to AI agents about a tool's behavior. The adapter infers default annotations from the operation type, but you can override them.

**Default behavior:**

- Mutation operations default to `destructiveHint: true` and `idempotentHint: false`.
- Query operations default to `readOnlyHint: true`.

**Override on the tool definition:**

```csharp
var tool = new OperationToolDefinition(
    Utf8GraphQLParser.Parse(
        """
        mutation AddBook($title: String!) {
            addBook(title: $title) { title }
        }
        """))
{
    DestructiveHint = false,
    IdempotentHint = true,
    OpenWorldHint = false
};
```

**Override in the GraphQL schema:**

You can annotate resolver methods with `[McpToolAnnotations]` to set hints at the schema level:

```csharp
// Types/Mutation.cs
using HotChocolate.Adapters.Mcp.Directives;

public class Mutation
{
    [McpToolAnnotations(DestructiveHint = false, IdempotentHint = true)]
    public Book AddBook(string title) => new(title);
}
```

You can also apply annotations using the fluent descriptor API:

```csharp
// Types/MutationType.cs
using HotChocolate.Adapters.Mcp.Extensions;

public class MutationType : ObjectType<Mutation>
{
    protected override void Configure(
        IObjectTypeDescriptor<Mutation> descriptor)
    {
        descriptor
            .Field(m => m.AddBook(default!))
            .McpToolAnnotations(
                destructiveHint: false,
                idempotentHint: true);
    }
}
```

Annotations set on the `OperationToolDefinition` take priority over annotations set in the schema.

# Tool Customization

You can customize the display title and icons of a tool:

```csharp
var tool = new OperationToolDefinition(
    Utf8GraphQLParser.Parse("query GetBooks { books { title } }"))
{
    Title = "Search Books",
    Icons =
    [
        new IconDefinition(new Uri("https://example.com/books.png"))
        {
            MimeType = "image/png",
            Sizes = ["48x48"],
            Theme = "light"
        }
    ]
};
```

# Prompts

The adapter supports MCP prompts, which are reusable prompt templates that AI agents can discover and use. Define prompts through your `IMcpStorage` implementation:

````csharp
var prompt = new PromptDefinition("code_review")
{
    Title = "Code Review",
    Description =
        "Asks the LLM to analyze code quality and suggest improvements.",
    Arguments =
    [
        new PromptArgumentDefinition("code")
        {
            Title = "Code to Review",
            Description = "The code to review",
            Required = true
        }
    ],
    Messages =
    [
        new PromptMessageDefinition(
            RoleDefinition.User,
            new TextContentBlockDefinition(
                """
                Please review this code:

                ```
                {code}
                ```
                """))
    ]
};
````

# Custom MCP Server Options

You can configure the underlying MCP server options and add custom (non-GraphQL) tools through the `AddMcp` overload:

```csharp
// Program.cs
builder.Services
    .AddGraphQL()
    .AddQueryType<Query>()
    .AddMcp(
        configureServerOptions: options =>
        {
            options.InitializationTimeout = TimeSpan.FromSeconds(10);
        },
        configureServer: server =>
        {
            server.WithTools([typeof(MyCustomTool)]);
        })
    .AddMcpStorage(myStorage);
```

Custom tools registered through `configureServer` appear alongside the GraphQL-derived tools. This lets you mix GraphQL operations and native MCP tools in the same server.

# Endpoint Configuration

The MCP endpoint defaults to `/graphql/mcp`. You can change the path:

```csharp
app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQLMcp("/api/mcp");
});
```

The adapter supports two MCP transport modes:

- **Streamable HTTP** (default): POST, GET, and DELETE on the base path.
- **HTTP with SSE** (legacy): GET on `/sse` and POST on `/message` sub-paths.

In stateless mode, only the Streamable HTTP POST endpoint is available.

# Fusion Integration

The MCP adapter works with Fusion gateway servers. Instead of `AddGraphQL()`, use `AddGraphQLGatewayServer()` and the rest of the configuration remains the same:

```csharp
// Program.cs
builder.Services
    .AddGraphQLGatewayServer()
    .AddInMemoryConfiguration(compositeSchema)
    .AddHttpClientConfiguration("Subgraph", subgraphUri)
    .AddMcp()
    .AddMcpStorage(myStorage);
```

When the Fusion gateway schema changes (for example, a new subgraph is composed), connected MCP clients receive tool list changed notifications automatically.

# Troubleshooting

**"You must call AddMcp()" error when starting the application**

You called `MapGraphQLMcp()` without registering the MCP services. Add `.AddMcp()` and `.AddMcpStorage()` to your GraphQL builder chain before calling `MapGraphQLMcp()`.

**Tools do not appear in the MCP client**

Verify that your `IMcpStorage` implementation returns the tool definitions from `GetOperationToolDefinitionsAsync`. If a tool's GraphQL operation references fields that do not exist on the schema, the adapter silently skips it and logs validation errors. Attach an `McpDiagnosticEventListener` to inspect validation errors:

```csharp
builder.Services
    .AddGraphQL()
    .AddMcp()
    .AddMcpStorage(myStorage)
    .AddDiagnosticEventListener(_ => new MyMcpListener());

public class MyMcpListener : McpDiagnosticEventListener
{
    public override void ValidationErrors(IReadOnlyList<IError> errors)
    {
        foreach (var error in errors)
        {
            Console.WriteLine(error.Message);
        }
    }
}
```

**Tool list does not update after changing storage**

Ensure your `IMcpStorage` implementation pushes events through the `IObservable<OperationToolStorageEventArgs>` interface when definitions change. Without these events, connected MCP clients are not notified.

# Next Steps

- [Error Handling](/docs/hotchocolate/v16/guides/error-handling) to customize how GraphQL errors appear in MCP tool results.
