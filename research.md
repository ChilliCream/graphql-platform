# Research: HTTP Client Configuration API for CommandLine.Client

## Design Goal

- **Library** sets its own fixed headers: `Accept`, `GraphQL-Preflight`, `GraphQL-Client-Version`, `ccc-agent`, HTTP/2 policy
- **Consumer** configures everything auth-related: base address, `Authorization` or API key header
- No library-specific abstractions like `NitroAuthHeader` or `ResolveBaseAddress` — consumer works directly with `HttpClient`

---

## Recommended API: Return `IHttpClientBuilder`

```csharp
public static IHttpClientBuilder AddNitroClients(this IServiceCollection services)
```

Returns the `IHttpClientBuilder` for the named HTTP client the library uses internally. This is the standard .NET pattern used by libraries like Refit, Polly, and Azure SDK.

**Library sets (internally, in `AddHttpClient` callback):**
- `Accept: application/json`
- `GraphQL-Preflight: 1`
- `GraphQL-Client-Version: {version}`
- `ccc-agent: Nitro CLI/{version}`
- `client.DefaultRequestVersion = new Version(2, 0)`
- `client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower`

**Consumer sets (via returned builder):**
- `BaseAddress`
- `Authorization: Bearer {token}` or `CCC-api-key: {key}`
- Any custom `DelegatingHandler`s (retry, logging, etc.)

---

## Usage

### External consumer (simple)

```csharp
services
    .AddNitroClients()
    .ConfigureHttpClient(client =>
    {
        client.BaseAddress = new Uri("https://api.chillicream.com/graphql");
        client.DefaultRequestHeaders.Add("Authorization", "Bearer my-token");
    });
```

### External consumer (from config, with IServiceProvider)

```csharp
services
    .AddNitroClients()
    .ConfigureHttpClient((sp, client) =>
    {
        var config = sp.GetRequiredService<IConfiguration>();
        client.BaseAddress = new Uri(config["Nitro:ApiUrl"]!);
        client.DefaultRequestHeaders.Add("CCC-api-key", config["Nitro:ApiKey"]!);
    });
```

### CLI integration (session + CLI option overrides)

```csharp
services
    .AddNitroServices()
    .AddNitroClients()
    .ConfigureHttpClient((sp, client) =>
    {
        var session = sp.GetRequiredService<ISessionService>();
        var apiUrl = session.Session?.ApiUrl
            ?? throw new InvalidOperationException("Not authenticated.");
        client.BaseAddress = new Uri($"https://{apiUrl}/graphql");
        client.DefaultRequestHeaders.Add(
            "Authorization",
            $"Bearer {session.Session!.Tokens!.AccessToken}");
    })
    .AddNitroCommands();
```

The CLI can also add `--cloud-url` / `--api-key` override logic directly in this callback since it has access to `ParseResult` via `IServiceProvider` (or it can wrap this in a dedicated CLI extension method).

---

## Implementation Changes

### `NitroClientServiceCollectionExtensions`

```csharp
public static IHttpClientBuilder AddNitroClients(this IServiceCollection services)
{
    ArgumentNullException.ThrowIfNull(services);

    services
        .TryAddNitroApiClient()   // registers IApiClient + named HttpClient
        .TryAddSingleton<IApisClient, ApisClient>()
        .TryAddSingleton<IApiKeysClient, ApiKeysClient>()
        // ... all 12 clients ...

    // Return the builder so the consumer can configure auth
    return services.AddHttpClient(ApiClient.ClientName);
    // Note: the library's own config was applied inside TryAddNitroApiClient
}
```

`TryAddNitroApiClient` registers the named HTTP client with only library-owned headers. Auth configuration is layered on top by the consumer via the returned `IHttpClientBuilder`.

**Key insight on ordering:** `AddHttpClient` callbacks chain — the first registered runs first. The library registers its standard headers first, then the consumer's `ConfigureHttpClient` runs after. This means the consumer can also *override* a library header if needed.

### Remove

- `NitroApiClientOptions` class
- `NitroAuthHeader` record
- All `Action<NitroApiClientOptions>?` parameters from every `AddNitro*Client` method

### Keep

- `IHttpClientBuilder` return type on all `AddNitro*Client` methods (or only on `AddNitroClients` if per-client registration is less critical)

---

## Per-Client Registration Methods

Same pattern — each returns `IHttpClientBuilder`:

```csharp
public static IHttpClientBuilder AddNitroApisClient(this IServiceCollection services)
{
    TryAddNitroApiClient(services);
    services.TryAddSingleton<IApisClient, ApisClient>();
    return services.AddHttpClient(ApiClient.ClientName);
}
```

---

## File/REST Downloads

`ClientsClient`, `SchemasClient`, `FusionConfigurationClient` use `IHttpClientFactory.CreateClient(ApiClient.ClientName)` for REST calls. They get the same base address + auth because they use the same named client — no additional changes needed.

---

## Summary of Changes

| File | Change |
|------|--------|
| `NitroApiClientOptions.cs` | **Delete** |
| `NitroAuthHeader.cs` | **Delete** |
| `NitroClientServiceCollectionExtensions.cs` | Remove `configure` params, return `IHttpClientBuilder`, move only library headers into `AddHttpClient` callback |
| CommandLine `Program.cs` / new extension | Chain `.ConfigureHttpClient((sp, client) => { /* session-based auth */ })` |
