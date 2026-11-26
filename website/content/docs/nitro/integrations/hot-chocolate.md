---
title: ".Net Middleware"
description: "Serve Nitro from your Hot Chocolate server: MapGraphQL() hosts it at /graphql by default, or use MapNitroApp() to expose the IDE on a separate endpoint."
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
  .WithOptions(o => o.Enable = false);
```

# Serve Modes

The `ServeMode` property controls which version of Nitro to serve from the Nitro CDN. The default mode is `Latest`, serving the most recent version of Nitro.

- `Latest`: Serves the latest version of Nitro from the CDN.
- `Insider`: Serves the insider version of Nitro from the CDN, allowing preview of upcoming features.
- `Version(string version)`: Serves a specific version of Nitro from the CDN.

Depending on your environment or preferences, you can choose the appropriate mode:

```csharp
endpoints
  .MapNitroApp()
  .WithOptions(o => o.ServeMode = ServeMode.Insider);
```

# Serve the Embedded Nitro App

Instead of serving Nitro from the CDN, you can serve the version of Nitro that ships in the `ChilliCream.Nitro.App.Embed` package. Call `WithEmbeddedNitroAssets()` on the endpoint:

```csharp
app.UseEndpoints(endpoints =>
{
    endpoints.MapGraphQL().WithEmbeddedNitroAssets();
    endpoints.MapNitroApp("/my-graphql-ui").WithEmbeddedNitroAssets();
});
```

On `MapGraphQL()`, Nitro stays on the same path as the GraphQL endpoint. Call `WithEmbeddedNitroAssets()` on a route group to serve the embedded Nitro app on every endpoint in the group:

```csharp
var api = app.MapGroup("/api").WithEmbeddedNitroAssets();
api.MapGraphQL();
```

All other Nitro options work the same way for the embedded Nitro app. As with other options that set the serve mode, the last call wins: calling `WithOptions(o => o.ServeMode = ServeMode.Version("..."))` after `WithEmbeddedNitroAssets()` serves that version from the CDN.

> [!NOTE]
> Setting `ServeMode.Embedded` still serves the embedded Nitro app, but it will be removed in Hot Chocolate 17. From Hot Chocolate 17, reference the `ChilliCream.Nitro.App.Embed` package and call `WithEmbeddedNitroAssets()`.

# Configuration Options

You can tailor Nitro to your needs by setting various options via `NitroAppOptions`. You can specify these options using the `WithOptions()` method in both `MapGraphQL()` and `MapNitroApp()` methods.

| Property                       | Type                | Description                                                                                                                                       |
| ------------------------------ | ------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| Enable                         | `bool`              | If `false`, disables the Nitro tool.                                                                                                              |
| ServeMode                      | `ServeMode`         | Defines which version of Nitro is served from the CDN: `Latest` (default), `Insider`, or `Version(string version)`. See `WithEmbeddedNitroAssets()`. |
| Title                          | `string`            | Specifies the title of the Nitro page.                                                                                                            |
| Document                       | `string`            | Specifies the default document content.                                                                                                           |
| IncludeCookies                 | `bool?`             | If `true`, includes cookies in the HTTP call to the GraphQL backend.                                                                              |
| HttpHeaders                    | `IHeaderDictionary` | Specifies the default HTTP headers for Nitro.                                                                                                     |
| UseGet                         | `bool`              | If `true`, uses HTTP GET as the default request method.                                                                                           |
| GraphQLEndpoint                | `string`            | Specifies the GraphQL endpoint. If `UseBrowserUrlAsGraphQLEndpoint` is `true`, it must be a relative path; otherwise, it must be an absolute URL. |
| UseBrowserUrlAsGraphQLEndpoint | `bool`              | If `true`, the schema endpoint URL is inferred from the browser URL.                                                                              |

Here is an example of how to set these options:

```csharp
endpoints
  .MapNitroApp()
  .WithOptions(o =>
  {
      o.ServeMode = ServeMode.Insider;
      o.Title = "My GraphQL API";
      o.Document = "Query { hello }";
      o.GraphQLEndpoint = "/api/graphql";
      o.IncludeCookies = true;
      o.Enable = true;
  });
```
