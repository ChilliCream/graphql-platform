---
title: ".Net  Middleware"
---

By default, when you map your GraphQL endpoints using `MapGraphQL()`, Nitro is automatically served at the `/graphql` endpoint.

```csharp
app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQL();
});
```

In the example above, the GraphQL service and Nitro are both mapped to the `/graphql` endpoint.

If you want to serve Nitro on a separate endpoint, you can use `MapNitroApp()` method:

```csharp
app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQL();
    endpoints.MapNitroApp("/my-graphql-ui");
});
```

In this configuration, the GraphQL service remains at the `/graphql` endpoint, and Nitro is served at the `/my-graphql-ui` endpoint.

# Disable the Middleware

In some scenarios, you may not want to serve Nitro, e.g., in a production environment. You can disable Nitro by setting the `Enable` property to `false`:

```csharp
endpoints
  .MapGraphQL()
  .WithOptions(
    new GraphQLServerOptions
    {
      Tool =
      {
        Enable = false
      }
    });
```

# Serve Modes

The `ServeMode` property controls which version of Nitro to serve. The default mode is `Latest`, serving the most recent version of Nitro from a CDN.
You can also serve the embedded version (`Embedded`) of Nitro, which is included in the package.

- `Latest`: Serves the latest version of Nitro from a CDN.
- `Insider`: Serves the insider version of Nitro from a CDN, allowing preview of upcoming features.
- `Embedded`: Serves the embedded version of Nitro that comes with the package.
- `Version(string version)`: Serves a specific version of Nitro from the CDN.

Depending on your environment or preferences, you can choose the appropriate mode:

```csharp
endpoints
  .MapNitroApp()
  .WithOptions(new GraphQLToolOptions
  {
      ServeMode = GraphQLToolServeMode.Embedded
  });
```

# Configuration Options

You can tailor Nitro to your needs by setting various options via the `GraphQLToolOptions` class. You can specify these options using the `WithOptions()` method in both `MapGraphQL()` and `MapNitroApp()` methods.

| Property                       | Type                   | Description                                                                                                                                       |
| ------------------------------ | ---------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| Enable                         | `bool`                 | If `false`, disables the Nitro tool.                                                                                                              |
| ServeMode                      | `GraphQLToolServeMode` | Defines how Nitro is served. Options include `Latest` (default), `Insider`, `Embedded`, and `Version(string version)`.                            |
| Title                          | `string`               | Specifies the title of the Nitro page.                                                                                                            |
| Document                       | `string`               | Specifies the default document content.                                                                                                           |
| IncludeCookies                 | `bool`                 | If `true`, includes cookies in the HTTP call to the GraphQL backend.                                                                              |
| HttpHeaders                    | `IHeaderDictionary`    | Specifies the default HTTP headers for Nitro.                                                                                                     |
| HttpMethod                     | `DefaultHttpMethod`    | Specifies the default HTTP method to use.                                                                                                         |
| GraphQLEndpoint                | `string`               | Specifies the GraphQL endpoint. If `UseBrowserUrlAsGraphQLEndpoint` is `true`, it must be a relative path; otherwise, it must be an absolute URL. |
| UseBrowserUrlAsGraphQLEndpoint | `bool`                 | If `true`, the schema endpoint URL is inferred from the browser URL.                                                                              |

Here is an example of how to set these options:

```csharp
endpoints
  .MapNitroApp()
  .WithOptions(new GraphQLToolOptions
  {
      ServeMode = GraphQLToolServeMode.Insider,
      Title = "My GraphQL API",
      Document = "Query { hello }",
      GraphQLEndpoint = "/api/graphql",
      IncludeCookies = true,
      Enable = true
  });
```
