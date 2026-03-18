---
title: "OpenAPI Adapter"
---

The OpenAPI adapter exposes your Hot Chocolate GraphQL schema as REST endpoints with automatic OpenAPI documentation. You define GraphQL operations annotated with `@http` directives, and the adapter generates HTTP endpoints that accept REST-style requests, execute the underlying GraphQL operation, and return the result as JSON. The generated endpoints appear in your OpenAPI specification alongside any other ASP.NET Core endpoints.

This is useful when you have a GraphQL API and need to provide a REST interface for clients that do not support GraphQL, or when you want to offer both GraphQL and REST access to the same backend.

# Setup

Install the `HotChocolate.Adapters.OpenApi` package:

```bash
dotnet add package HotChocolate.Adapters.OpenApi
```

Register the adapter on your GraphQL server and map the endpoints:

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddRouting()
    .AddOpenApi(options => options.AddGraphQLTransformer());

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddOpenApiDefinitionStorage(myStorage);

var app = builder.Build();

app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapOpenApi();
    endpoints.MapOpenApiEndpoints();
    endpoints.MapGraphQL();
});

app.Run();
```

`AddOpenApiDefinitionStorage()` registers the adapter services and provides the endpoint definitions. `AddGraphQLTransformer()` adds a document transformer that injects the generated endpoints into your OpenAPI specification. `MapOpenApiEndpoints()` registers the dynamic HTTP endpoints at runtime.

# Endpoint Definitions

Each REST endpoint is defined by a GraphQL operation annotated with an `@http` directive. You provide these operations through an `IOpenApiDefinitionStorage` implementation.

A GET endpoint that fetches a user by ID:

```graphql
"Fetches a user by their id"
query GetUserById($userId: ID!)
  @http(method: GET, route: "/users/{userId}") {
  userById(id: $userId) {
    id
    name
    email
  }
}
```

A POST endpoint that creates a user:

```graphql
"Creates a user"
mutation CreateUser($user: UserInput! @body)
  @http(method: POST, route: "/users") {
  createUser(user: $user) {
    id
    name
    email
  }
}
```

The `@http` directive specifies the HTTP method and route. Route parameters like `{userId}` map to GraphQL variables. The `@body` directive on a variable indicates that the HTTP request body maps to that variable.

# How It Works

The adapter translates between REST and GraphQL concepts:

| REST Concept                 | GraphQL Concept                       |
| ---------------------------- | ------------------------------------- |
| HTTP method (GET, POST, PUT) | Specified by `@http(method: ...)`     |
| Route path                   | `@http(route: "/path/{param}")`       |
| Route parameters             | GraphQL variables matched by name     |
| Query parameters             | Variables listed in `queryParameters` |
| Request body                 | Variable annotated with `@body`       |
| Response body                | Selected fields from the operation    |

When a client sends an HTTP request to a generated endpoint, the adapter extracts route parameters, query parameters, and the request body, maps them to GraphQL variables, executes the operation, and returns the root field's data as the response body.

# Route Parameters

Route parameters in curly braces map to GraphQL variables by name:

```graphql
query GetUser($userId: ID!) @http(method: GET, route: "/users/{userId}") {
  userById(id: $userId) {
    id
    name
  }
}
```

A request to `GET /users/42` sets `$userId` to `"42"`.

You can also map route parameters to nested fields of a variable using the `key:$variable.path` syntax:

```graphql
mutation UpdateUser($user: UserInput! @body)
@http(method: PUT, route: "/users/{userId:$user.id}") {
  updateUser(user: $user) {
    id
    name
  }
}
```

A PUT request to `/users/42` with a JSON body sets the `id` field of the `$user` variable to `"42"`, and the rest of the body fills in the remaining fields.

# Query Parameters

Use the `queryParameters` argument on the `@http` directive to expose GraphQL variables as URL query parameters:

```graphql
query GetUserDetails($userId: ID!, $includeAddress: Boolean!)
@http(
  method: GET
  route: "/users/{userId}/details"
  queryParameters: ["includeAddress"]
) {
  userById(id: $userId) {
    id
    name
    address @include(if: $includeAddress) {
      street
    }
  }
}
```

A request to `GET /users/1/details?includeAddress=true` sets `$includeAddress` to `true`.

Query parameters support the same `key:$variable.path` mapping syntax as route parameters.

# Request Body

The `@body` directive on a variable maps the entire HTTP request body to that variable:

```graphql
mutation CreateUser($user: UserInput! @body)
@http(method: POST, route: "/users") {
  createUser(user: $user) {
    id
    name
    email
  }
}
```

A POST request with a JSON body `{"id": "6", "name": "Alice", "email": "alice@example.com"}` sets `$user` to that object. The request must have a `Content-Type: application/json` header.

# Shared Fragments

You can define reusable GraphQL fragments as separate documents. The adapter resolves fragment references across documents:

```graphql
-- Document 1: endpoint definition
query GetUser($userId: ID!)
  @http(method: GET, route: "/users/{userId}") {
  userById(id: $userId) {
    ...UserFields
  }
}

-- Document 2: shared fragment
fragment UserFields on User {
  id
  name
  email
  address {
    ...AddressFields
  }
}

-- Document 3: another shared fragment
fragment AddressFields on Address {
  street
}
```

Each document is a separate entry in your `IOpenApiDefinitionStorage`. Fragment-only documents are treated as shared models.

# Storage

The `IOpenApiDefinitionStorage` interface provides endpoint and fragment definitions to the adapter:

```csharp
// Services/MyOpenApiStorage.cs
using HotChocolate.Adapters.OpenApi;
using HotChocolate.Language;

public class MyOpenApiStorage : IOpenApiDefinitionStorage
{
    public event EventHandler? Changed;

    public ValueTask<IEnumerable<IOpenApiDefinition>>
        GetDefinitionsAsync(
            CancellationToken cancellationToken = default)
    {
        var documents = new List<IOpenApiDefinition>();

        var getUserDoc = Utf8GraphQLParser.Parse(
            """
            query GetUser($userId: ID!)
              @http(method: GET, route: "/users/{userId}") {
              userById(id: $userId) {
                id
                name
              }
            }
            """);
        documents.Add(OpenApiDefinitionParser.Parse(getUserDoc));

        return ValueTask.FromResult<IEnumerable<IOpenApiDefinition>>(
            documents);
    }
}
```

Register it with your GraphQL server:

```csharp
// Program.cs
var storage = new MyOpenApiStorage();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddOpenApiDefinitionStorage(storage);
```

The storage raises its `Changed` event when definitions are modified. The adapter picks up changes at runtime, adding, updating, or removing HTTP endpoints without a restart. This hot-reload behavior extends to the OpenAPI specification.

# OpenAPI Specification

The adapter integrates with ASP.NET Core's built-in OpenAPI support. After you register `AddOpenApi(options => options.AddGraphQLTransformer())`, the generated endpoints appear in the OpenAPI document at `/openapi/v1.json`.

Each endpoint definition's description becomes the OpenAPI operation summary. Route and query parameters become OpenAPI parameters with types inferred from the GraphQL schema. Request body schemas are generated from the GraphQL input types.

# Fusion Integration

The OpenAPI adapter works with Fusion gateway servers. Replace `AddGraphQLServer()` with `AddGraphQLGatewayServer()` and the rest of the configuration remains the same:

```csharp
// Program.cs
builder.Services
    .AddGraphQLGatewayServer()
    .AddInMemoryConfiguration(compositeSchema)
    .AddHttpClientConfiguration("Subgraph", subgraphUri)
    .AddOpenApiDefinitionStorage(myStorage);
```

The Fusion gateway composes schemas from multiple subgraphs. The OpenAPI adapter generates REST endpoints that execute operations against the composed schema, so a single REST endpoint can fetch data from multiple subgraphs transparently.

# Troubleshooting

**Endpoint returns 404**

Verify that your `IOpenApiDefinitionStorage` returns the definitions and that the route in the `@http` directive matches the URL you are requesting. Check that you called both `MapOpenApiEndpoints()` and `MapGraphQL()` in your endpoint configuration. If you added a definition at runtime, wait for the hot-reload cycle to complete.

**Request body is not parsed**

The adapter requires a `Content-Type: application/json` header on POST and PUT requests. Other content types are rejected. Ensure the `@body` directive is present on the variable that should receive the request body.

**Endpoint returns 500 with validation errors**

The operation references a field or type that does not exist on the GraphQL schema. The adapter validates definitions on startup and logs errors. Attach an `OpenApiDiagnosticEventListener` to inspect validation details:

```csharp
builder.Services
    .AddGraphQLServer()
    .AddOpenApiDefinitionStorage(myStorage)
    .AddDiagnosticEventListener(_ => new MyOpenApiListener());

public class MyOpenApiListener : OpenApiDiagnosticEventListener
{
    public override void ValidationErrors(
        IReadOnlyList<OpenApiDefinitionValidationError> errors)
    {
        foreach (var error in errors)
        {
            Console.WriteLine(error.Message);
        }
    }
}
```

# Next Steps

- [MCP Adapter](/docs/hotchocolate/v16/guides/mcp-adapter) to expose your GraphQL schema as MCP tools for AI agents.
- [Error Handling](/docs/hotchocolate/v16/guides/error-handling) to customize error responses in generated endpoints.
