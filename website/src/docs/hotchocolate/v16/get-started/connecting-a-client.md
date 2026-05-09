---
title: "Connect a client"
description: "Send a GraphQL operation to a running Hot Chocolate endpoint with HTTP, pass variables, and read the response."
---

Once your Hot Chocolate server is running, the next step is to confirm that an external process can interact with it. This page guides you through connecting a client to your GraphQL endpoint, sending operations, passing variables, and interpreting responses. You will:

- Identify your GraphQL endpoint URL
- Send a GraphQL operation using HTTP POST
- Pass values with GraphQL variables
- Repeat the request from a minimal .NET app
- Read the `data` and `errors` properties in the response
- Decide when to use raw HTTP versus a typed client

The examples here use minimal clients to help you understand the structure of a GraphQL request. This foundation applies to command-line tools, backend services, browser apps, mobile apps, and generated clients.

# Find your GraphQL endpoint

Check the terminal output from your running ASP.NET Core app. The default project prints something like:

```text
Now listening on: http://localhost:5095
Application started. Press Ctrl+C to shut down.
```

By default, the template maps GraphQL to `/graphql` using `app.MapGraphQL()`. For the example above, your endpoint is:

```text
http://localhost:5095/graphql
```

If your terminal shows a different port, use that port. If you mapped GraphQL to a custom route (for example, `app.MapGraphQL("/api/graphql")`), use that path in all requests on this page.

Before continuing, make sure:

- The server process is running
- Opening the endpoint in a browser loads Nitro or your configured GraphQL route
- You have the correct endpoint URL for the following commands

For more on endpoint configuration, see [Endpoints](/docs/hotchocolate/v16/server/endpoints#mapgraphql).

# Choose your client approach

Different tools expose different parts of a GraphQL request. This page starts with raw HTTP so you can see the request envelope before moving to a client library.

| If your goal is... | Use this path |
| --- | --- |
| Verify that another process can reach the server | Continue with the HTTP request below. |
| Learn what a GraphQL client sends | Continue with the HTTP request below. |
| Add a small call from .NET code | Continue to the `HttpClient` example. |
| Generate .NET client types from operations | Start with [Strawberry Shake](/docs/strawberryshake/v16) after you understand the request shape. |
| Study GET requests, batching, multipart, streaming, uploads, or response negotiation | Read [HTTP Transport](/docs/hotchocolate/v16/server/http-transport). |

Nitro remains useful for exploring the schema, running operations, and inspecting responses. Here, the focus is on sending the same operation from an external client.

# What does a GraphQL client send?

A GraphQL client sends an operation document to the endpoint, typically as a POST request with a JSON body:

```http
POST /graphql
Content-Type: application/json
Accept: application/graphql-response+json

{
  "query": "query ReadBook { book { title author { name } } }"
}
```

This JSON body is the GraphQL request. The main fields are:

| Request property | Required? | Description |
| --- | --- | --- |
| `query` | Yes | The GraphQL operation text (query, mutation, or subscription). |
| `variables` | No | A JSON object with values for declared variables. |
| `operationName` | Sometimes | Specifies which operation to run if the document contains multiple named operations. |

The `Content-Type: application/json` header tells Hot Chocolate how to parse the body. The `Accept: application/graphql-response+json` header requests the GraphQL over HTTP response format.

Nitro and generated clients both send requests in this format, even if they hide the JSON envelope behind their APIs.

For full transport details, see the [GraphQL over HTTP specification](https://graphql.github.io/graphql-over-http/) and [Hot Chocolate HTTP Transport](/docs/hotchocolate/v16/server/http-transport).

# Send a query using HTTP POST

Try sending the `ReadBook` operation from your terminal. Replace the URL if your server uses a different port or route.

```bash
curl -s -X POST http://localhost:5095/graphql \
  -H "Content-Type: application/json" \
  -H "Accept: application/graphql-response+json" \
  -d '{"query":"query ReadBook { book { title author { name } } }"}'
```

You should receive a response like:

```json
{
  "data": {
    "book": {
      "title": "C# in depth.",
      "author": {
        "name": "Jon Skeet"
      }
    }
  }
}
```

The response structure matches your selection set:

- Requesting `book.title` returns `data.book.title`
- Requesting `book.author.name` returns `data.book.author.name`
- Only the fields you select appear in the response

If your schema is different (for example, if you added Hot Chocolate to an existing app), use a field that exists in your schema. You can discover available fields in Nitro, then send the operation with `curl`.

# Passing values with variables

Variables allow you to keep the operation text stable and supply changing values separately. Declare variables in the operation and provide their values in the request body.

The starter schema does not include an argument-bearing `book` field, so this example uses the built-in introspection field `__type` to query the schema for a type by name:

```bash
curl -s -X POST http://localhost:5095/graphql \
  -H "Content-Type: application/json" \
  -H "Accept: application/graphql-response+json" \
  -d '{"query":"query TypeFields($typeName: String!) { __type(name: $typeName) { name fields { name } } }","variables":{"typeName":"Book"}}'
```

Expected response:

```json
{
  "data": {
    "__type": {
      "name": "Book",
      "fields": [
        { "name": "title" },
        { "name": "author" }
      ]
    }
  }
}
```

If you previously added a `publishedYear` field to `Book`, it will appear in the `fields` array as well.

There are two key parts:

```graphql
query TypeFields($typeName: String!) {
  __type(name: $typeName) {
    name
    fields {
      name
    }
  }
}
```

The operation declares `$typeName` as a variable.

```json
{
  "variables": {
    "typeName": "Book"
  }
}
```

The request provides a value for `typeName` as JSON.

Variable names must match between the operation and the request. Variable values must conform to the expected GraphQL type. Always supply user input through `variables` rather than building operation strings by concatenation.

# Call the endpoint from .NET with `HttpClient`

Application code sends the same request envelope as the command-line example. Create a small console app in a new terminal while your Hot Chocolate server is running:

```bash
dotnet new console --name GraphQLClientDemo
cd GraphQLClientDemo
```

Replace the generated `Program.cs` with the following code. Update the endpoint URL if needed:

```csharp
using System.Net.Http.Json;

var endpoint = new Uri("http://localhost:5095/graphql");

using var client = new HttpClient();
client.DefaultRequestHeaders.Accept.ParseAdd("application/graphql-response+json");

var request = new
{
    query = """
        query ReadBook {
          book {
            title
            author {
              name
            }
          }
        }
        """
};

using var response = await client.PostAsJsonAsync(endpoint, request);
var body = await response.Content.ReadAsStringAsync();

Console.WriteLine(body);
```

Run the client:

```bash
dotnet run
```

You should see output like:

```json
{"data":{"book":{"title":"C# in depth.","author":{"name":"Jon Skeet"}}}}
```

The JSON may appear on a single line. The important part is that `data.book.title` and `data.book.author.name` are present.

To confirm the console app is calling the running API, stop the Hot Chocolate server and run the client again. It should fail with a connection error. Restart the server before continuing.

# Reading `data` and `errors` in the response

A GraphQL response is a JSON object with two top-level properties you will use most often: `data` and `errors`.

| Response property | Description |
| --- | --- |
| `data` | Contains successful field results, shaped like the operation's selection set. |
| `errors` | Contains GraphQL errors from parsing, validation, authorization, or execution. |

A successful response includes `data`:

```json
{
  "data": {
    "book": {
      "title": "C# in depth.",
      "author": {
        "name": "Jon Skeet"
      }
    }
  }
}
```

If you request a field that does not exist in the schema, Hot Chocolate returns a GraphQL error. For example, this operation misspells `title`:

```bash
curl -s -X POST http://localhost:5095/graphql \
  -H "Content-Type: application/json" \
  -H "Accept: application/graphql-response+json" \
  -d '{"query":"query ReadBook { book { titel } }"}'
```

Expected response:

```json
{
  "errors": [
    {
      "message": "The field `titel` does not exist on the type `Book`."
    }
  ]
}
```

The error payload may include locations, paths, extensions, or a different message format. The key point is that this is a GraphQL validation error, not a network failure.

Both `data` and `errors` can appear together if part of the operation succeeds and part fails. When handling responses in client code:

- First, check if the HTTP request reached the server
- Then, inspect the GraphQL response body
- Treat `errors` as part of the GraphQL result, not as a network exception
- Do not assume that every response with `data` is error-free

For more on error handling, see [Error Handling](/docs/hotchocolate/v16/guides/error-handling) and [GraphQL Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors).

# Deciding between raw HTTP and a typed client

You now have a reproducible external request. Continue using this approach when it fits your needs, or move to a client library when your application requires more structure.

| Choose... | When... |
| --- | --- |
| Raw HTTP | For learning, smoke tests, scripts, diagnosing endpoint behavior, or narrow integrations. |
| `HttpClient` with a small wrapper | For a few stable operations and minimal dependencies. |
| A typed GraphQL client | When your application depends on repeated operations, generated result types, variable types, or schema-driven workflows. |
| HTTP transport reference | For GET requests, batching, file uploads, subscriptions, response negotiation, streaming, or status code details. |

A client sends [operations](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/operations): GraphQL documents that the server parses, validates, and executes. The client is any code or tool that sends these documents and reads the results.

For a deeper look at client responsibilities, see [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients). For generated .NET clients, start with [Strawberry Shake](/docs/strawberryshake/v16). For transport details, see [HTTP Transport](/docs/hotchocolate/v16/server/http-transport).

# Troubleshooting requests

If your request does not work, use the symptom to guide your next step:

| Symptom | Likely cause | Fix | Verification |
| --- | --- | --- | --- |
| Connection refused or timeout | Server is not running, wrong scheme, host, or port | Start the ASP.NET Core app and copy the current URL from the terminal | App logs show the same URL as the request |
| `404 Not Found` | Request is going to the app root, an old route, or a route that does not map GraphQL | Use the route configured by `MapGraphQL`, usually `/graphql` | Request URL matches the endpoint mapping |
| HTTPS certificate warning or TLS failure | Local ASP.NET Core development certificate is not trusted | Trust the development certificate or use HTTP localhost for this tutorial | Request reaches the server without TLS errors |
| `415 Unsupported Media Type` or body not processed | `Content-Type` header is missing or incorrect | Send `Content-Type: application/json` | Hot Chocolate parses the request envelope |
| `400 Bad Request` before GraphQL execution | Malformed JSON body or envelope is not a JSON object | Send a JSON object with `query` and optional `variables` | A valid operation reaches GraphQL validation |
| `Cannot query field ...` | Operation does not match the current schema | Compare field names and selection set with the schema in Nitro | A corrected operation returns `data` |
| Variable missing or type mismatch | Variable declaration and `variables` object do not match | Supply every declared variable with a JSON value of the expected shape | Changing the variable value changes the operation input without changing the operation text |
| Top-level `errors` with no `data` | Operation failed during parsing, validation, authorization, or execution | Read the error message, correct the operation or context, and retry | Response contains the expected `data` shape |
| Browser client fails while `curl` works | CORS, credentials, or authentication headers differ | Configure the ASP.NET Core host and client for the required origin, credentials, and headers | The same operation succeeds from the browser after configuration |

For broader setup issues, see [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting). For endpoint behavior, read [Endpoints](/docs/hotchocolate/v16/server/endpoints). For HTTP request and response details, see [HTTP Transport](/docs/hotchocolate/v16/server/http-transport).

# Next steps

You have now sent a GraphQL operation from outside Nitro and repeated the call from .NET code.

Continue with the page that matches your goal:

- Read the [generated project explanation](/docs/hotchocolate/v16/get-started/what-just-happened) to see how the first request moved through Hot Chocolate.
- [Make it yours](/docs/hotchocolate/v16/get-started/make-it-yours) shows where to start changing the sample schema.
- [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients) explains client responsibilities and operation contracts in more depth.
- [Strawberry Shake](/docs/strawberryshake/v16) introduces generated .NET GraphQL clients.
