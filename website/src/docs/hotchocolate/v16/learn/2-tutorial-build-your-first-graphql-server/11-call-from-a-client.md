---
title: "Call from a client"
description: "Send a GraphQL operation from a small C# program to the running tutorial server, pass variables, and inspect data and errors in the response."
---

In previous chapters, you built and tested a GraphQL server using queries, mutations, and subscriptions, verifying each step with Nitro, the built-in development UI.

This chapter demonstrates how to connect to your server from a separate program. You will send HTTP requests containing GraphQL operations, pass variables, and examine the response envelope. No changes to the LibraryServer project are needed.

By the end of this chapter, you will have:

- Verified the tutorial server endpoint
- Sent a GraphQL operation over HTTP from a C# console application
- Sent a request with a variable to select a specific book
- Read the `data` property from a successful response
- Read the `errors` property from a failed request
- Addressed common client call issues

For more on client architecture, typed clients, and code generation, see [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients) after this chapter.

# Ensure the tutorial server is running

Before sending requests from a client, make sure your tutorial server is running. In the terminal where you manage the LibraryServer project, start the server:

```bash
cd LibraryServer
dotnet run
```

Look for output like this:

```text
Now listening on: http://localhost:5095
```

Your port may differ from `5095` if your project uses a different launch setting or if another process was using that port. Always use the port shown in your terminal for the commands in this chapter.

Because `Program.cs` calls `app.MapGraphQL()`, the GraphQL endpoint is at `/graphql`. The full endpoint URL is:

```text
http://localhost:5095/graphql
```

Before continuing, check:

- The server process is running
- You have the correct endpoint URL
- Opening the endpoint URL in a browser shows Nitro or a GraphQL explorer

If the server is not running or Nitro does not load, rebuild and restart:

```bash
dotnet build
dotnet run
```

For more on endpoint configuration, see [Endpoints](/docs/hotchocolate/v16/server/endpoints).

# What does a client send?

A client sends a GraphQL operation document to the endpoint, typically as a POST request with a JSON body:

```http
POST /graphql HTTP/1.1
Content-Type: application/json
Accept: application/graphql-response+json

{
  "query": "query GetBooks { books(first: 3) { nodes { id title } } }"
}
```

The request body is a JSON object. The two properties you will use are:

| Request property | Required | Description |
| --- | --- | --- |
| `query` | Yes | The GraphQL operation as a string |
| `variables` | No | A JSON object with values for variables declared in the operation |

The `Content-Type: application/json` header tells Hot Chocolate how to parse the body. The `Accept: application/graphql-response+json` header requests the GraphQL over HTTP response format.

Nitro and generated clients use this same request shape, even if the code hides the JSON envelope.

The `query` property contains the operation text. The operation and the client are separate: the operation is the GraphQL document the server executes, while the client is the code that sends it and reads the result.

For full HTTP transport details, see [HTTP Transport](/docs/hotchocolate/v16/server/http-transport).

# Create a C# console client

Next, create a console app alongside your server project. Open a second terminal, leaving the server running.

```bash
dotnet new console --name LibraryClient
cd LibraryClient
```

This creates a new project in a `LibraryClient` folder. The running LibraryServer project is unaffected.

Replace the generated `Program.cs` with the following code. Update the port if your server is not on `5095`:

```csharp
using System.Net.Http.Json;

var endpoint = new Uri("http://localhost:5095/graphql");

using var client = new HttpClient();
client.DefaultRequestHeaders.Accept.ParseAdd("application/graphql-response+json");

var request = new
{
    query = """
        query GetBooks {
          books(first: 3) {
            nodes {
              id
              title
              author {
                id
                name
              }
            }
            pageInfo {
              hasNextPage
              endCursor
            }
          }
        }
        """
};

using var response = await client.PostAsJsonAsync(endpoint, request);
var body = await response.Content.ReadAsStringAsync();

Console.WriteLine(body);
```

This program creates an `HttpClient`, sets the `Accept` header, builds a request object with the operation text, and posts it to the GraphQL endpoint. The response body is printed to the console.

The `query` property uses a C# raw string literal. The operation queries the paginated `books` field, as in earlier chapters.

Build the client project:

```bash
dotnet build
```

You should see:

```text
Build succeeded.
```

# Send your first query

With the LibraryServer running, run the client project:

```bash
dotnet run
```

Expected output:

```json
{"data":{"books":{"nodes":[{"id":1,"title":"The Left Hand of Darkness","author":{"id":1,"name":"Ursula K. Le Guin"}},{"id":2,"title":"A Wizard of Earthsea","author":{"id":1,"name":"Ursula K. Le Guin"}},{"id":3,"title":"Kindred","author":{"id":2,"name":"Octavia E. Butler"}}],"pageInfo":{"hasNextPage":true,"endCursor":"Mg=="}}}}
```

The JSON appears on one line, which is expected for this minimal client.

Check that `data.books.nodes` contains the first three books from the tutorial catalog and that `pageInfo.hasNextPage` is `true`.

This client is calling the same endpoint and returning data from the same paginated `books` field as in the pagination chapter. The difference is that a separate program, not Nitro, is making the call.

To confirm the client is calling the live server, stop the server and run the client again. You should see a connection error. Restart the server before continuing.

# Passing variables in a request

Variables allow you to separate the operation text from the values you supply. The operation text remains the same, while variable values can change with each request.

Update `Program.cs` to request a single book by its identifier. The tutorial seed data includes a book with `id` 3, titled "Kindred" by Octavia E. Butler.

Replace `Program.cs` with this code:

```csharp
using System.Net.Http.Json;

var endpoint = new Uri("http://localhost:5095/graphql");

using var client = new HttpClient();
client.DefaultRequestHeaders.Accept.ParseAdd("application/graphql-response+json");

var request = new
{
    query = """
        query GetBookById($id: Int!) {
          bookById(id: $id) {
            id
            title
            author {
              id
              name
            }
          }
        }
        """,
    variables = new
    {
        id = 3
    }
};

using var response = await client.PostAsJsonAsync(endpoint, request);
var body = await response.Content.ReadAsStringAsync();

Console.WriteLine(body);
```

There are two key parts to check:

- The operation declares `$id` as a variable:

  ```graphql
  query GetBookById($id: Int!) {
    bookById(id: $id) {
      ...
    }
  }
  ```

- The request body supplies `id` in the `variables` object:

  ```json
  {
    "variables": {
      "id": 3
    }
  }
  ```

Variable names in the operation and in the `variables` object must match. The value must have the correct JSON type. Since `bookById` expects `Int!`, the variable value is a JSON number (not a string).

Run the updated client:

```bash
dotnet run
```

Expected output:

```json
{"data":{"bookById":{"id":3,"title":"Kindred","author":{"id":2,"name":"Octavia E. Butler"}}}}
```

Now, change the `id` value from `3` to `1` in the `variables` object and run again:

```csharp
    variables = new
    {
        id = 1
    }
```

Expected output for `id: 1`:

```json
{"data":{"bookById":{"id":1,"title":"The Left Hand of Darkness","author":{"id":1,"name":"Ursula K. Le Guin"}}}}
```

Changing the variable value changes the result, without modifying the operation text.

Checkpoint: the `id` variable determines which book is returned, and the operation string remains unchanged between runs.

# Reading data and errors from the response

A GraphQL response is a JSON object with two top-level properties: `data` and `errors`.

| Response property | Description |
| --- | --- |
| `data` | Contains successful field results, shaped like the operation selection set. Present when execution succeeds. |
| `errors` | Contains GraphQL errors from parsing, validation, or execution. Present when the operation or part of it fails. |

A successful response includes `data`:

```json
{
  "data": {
    "bookById": {
      "id": 1,
      "title": "The Left Hand of Darkness",
      "author": {
        "id": 1,
        "name": "Ursula K. Le Guin"
      }
    }
  }
}
```

If a field name in the operation does not match the schema, Hot Chocolate returns a validation error. For example, change the field name from `title` to `titel` in the operation text in `Program.cs`:

```csharp
    query = """
        query GetBookById($id: Int!) {
          bookById(id: $id) {
            id
            titel
          }
        }
        """,
```

Run the client:

```bash
dotnet run
```

Expected output shape:

```json
{
  "errors": [
    {
      "message": "The field `titel` does not exist on the type `Book`.",
      "locations": [{ "line": 4, "column": 7 }],
      "extensions": {
        "type": "Book",
        "field": "titel"
      }
    }
  ]
}
```

The exact message and extension properties may vary by version, but the important part is the top-level `errors` array. When validation fails before execution, there is no `data` property.

Both `data` and `errors` can appear together if only some fields fail. For client code, this means:

- Read the GraphQL response body and inspect `data` and `errors` separately
- Use the HTTP status code for transport or request status
- Treat `errors` as part of the GraphQL result, even if the HTTP status code is successful
- A response may contain both `data` and `errors`

For more on error handling, see [Error Handling](/docs/hotchocolate/v16/guides/error-handling) and [Errors](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/errors).

Restore the correct field name (`title`) before moving on.

# Troubleshooting common client call issues

Use this table to diagnose and resolve issues you may encounter:

| Symptom | Likely cause | Fix | How to verify |
| --- | --- | --- | --- |
| Connection refused or timeout | The LibraryServer project is not running | Start the server with `dotnet run` in the `LibraryServer` folder | The server terminal prints `Now listening on:` and the client returns a JSON response |
| Response is `404 Not Found` | Incorrect port or path | Confirm the port from the server terminal and use `/graphql` as the path | The response body is a GraphQL response, not an HTML error page |
| `415 Unsupported Media Type` | Missing `Content-Type` header | Ensure `PostAsJsonAsync` is used, which sets `Content-Type: application/json` automatically | The server accepts the request and returns a GraphQL response |
| Response body is not valid JSON | Unescaped characters in the `query` property | Use a C# raw string literal (`"""..."""`) or escape inner quotes | The server executes the operation and returns `data` or `errors` |
| `Variable '$id' got invalid value` or similar | Mismatched variable name or incorrect JSON type | Ensure the key in `variables` matches the `$name` in the operation exactly, including case. For `Int!`, the value must be a JSON number | The operation receives the intended value and the response contains `data` |
| `Cannot query field 'x' on type 'Y'` | Field does not exist in the schema | Check the field list in Nitro and update the operation to use the correct field name | The response contains `data` and no `errors` |
| Browser call fails but console app works | CORS policy blocks the browser origin | Configure CORS in `Program.cs` before calling the server from browser JavaScript | The operation succeeds from the browser after the host allows the origin |

For endpoint behavior, see [Endpoints](/docs/hotchocolate/v16/server/endpoints). For HTTP transport details, see [HTTP Transport](/docs/hotchocolate/v16/server/http-transport). For general setup issues, see [Troubleshooting](/docs/hotchocolate/v16/get-started/troubleshooting).

# Checkpoint: calling the server from a client

You have made these changes:

| File | Change |
| --- | --- |
| `LibraryClient/Program.cs` | Created a console app that calls `/graphql` with a query and with variables |

No changes were made to the LibraryServer project in this chapter.

You are ready for the next chapter when:

- The LibraryServer project runs and responds at `/graphql`
- Running `dotnet run` in the `LibraryClient` project returns a JSON response with `data`
- The `books` operation returns three books with `pageInfo.hasNextPage: true`
- Changing the `id` variable in the `bookById` operation returns a different book without changing the operation text
- You can identify whether a response contains `data`, `errors`, or both

The key concept: a client sends an operation document to a GraphQL endpoint over HTTP. The operation is a string in a JSON body. Variables are sent in a separate `variables` property. The server responds with a JSON envelope containing `data`, `errors`, or both.

For more on client types, typed client code generation, and client architecture, see [Clients](/docs/hotchocolate/v16/learn/3-thinking-in-graphql/clients). For generated .NET clients, see [Strawberry Shake](/docs/strawberryshake/v16). For the full HTTP transport specification and advanced options, see [HTTP Transport](/docs/hotchocolate/v16/server/http-transport).

In the next chapter, you will add security to the tutorial API.

If your local project does not match these checkpoints, compare your files with the tutorial checkpoint guidance in [Source code and checkpoints](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/00-source-code-and-checkpoints/) or visit [Stuck?](/docs/hotchocolate/v16/learn/2-tutorial-build-your-first-graphql-server/stuck/).
