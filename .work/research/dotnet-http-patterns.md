# .NET HTTP Client Library Design Patterns

Research into best practices for designing configurable HTTP client libraries in .NET, with specific applicability to the Nitro CommandLine.Client library.

---

## 1. Microsoft.Extensions.Http: IHttpClientFactory Fundamentals

### How It Works

`IHttpClientFactory` manages the lifecycle of `HttpMessageHandler` instances, pooling and reusing them to avoid socket exhaustion while still reacting to DNS changes. Key points:

- **Handler pooling**: Handlers are cached per client name with a default 2-minute lifetime
- **HttpClient is transient**: Each `CreateClient()` call returns a new `HttpClient`, but the underlying handler is pooled
- **Separate DI scopes**: The factory creates a separate DI scope per `HttpMessageHandler` instance, independent from app scopes

### Three Registration Patterns

| Pattern | Best For | Key Characteristic |
|---------|----------|--------------------|
| **Named clients** | Multiple configs of same client type | String-keyed, resolved via `IHttpClientFactory.CreateClient("name")` |
| **Typed clients** | Strongly-typed API wrappers | Class receives `HttpClient` in constructor, registered as transient |
| **Generated clients** | Libraries like Refit/StrawberryShake | Framework generates the implementation, library registers via extension |

### IHttpClientBuilder

All `AddHttpClient` overloads return `IHttpClientBuilder`, enabling fluent configuration:

```csharp
services.AddHttpClient("MyClient", client =>
    {
        client.BaseAddress = new Uri("https://api.example.com/");
    })
    .AddHttpMessageHandler<AuthHandler>()          // Add delegating handler
    .ConfigurePrimaryHttpMessageHandler(() =>       // Configure primary handler
        new HttpClientHandler { UseDefaultCredentials = true })
    .SetHandlerLifetime(TimeSpan.FromMinutes(5))   // Override handler lifetime
    .ConfigureHttpClient((sp, client) => { ... })  // Additional HttpClient config
    .UseSocketsHttpHandler((handler, _) =>          // .NET 5+ socket handler
        handler.PooledConnectionLifetime = TimeSpan.FromMinutes(2));
```

#### IHttpClientBuilder Extension Methods (from Microsoft)

| Method | Description |
|--------|-------------|
| `AddHttpMessageHandler` | Adds a `DelegatingHandler` to the outgoing request pipeline |
| `AddTypedClient` | Binds a typed client to the named HttpClient |
| `ConfigureHttpClient` | Adds a delegate to configure the `HttpClient` instance |
| `ConfigurePrimaryHttpMessageHandler` | Configures the inner/primary handler |
| `RedactLoggedHeaders` | Controls header redaction in logs |
| `SetHandlerLifetime` | Sets how long a handler can be reused |
| `UseSocketsHttpHandler` | Configures `SocketsHttpHandler` as primary (.NET 5+) |

---

## 2. Library Author Patterns from the Ecosystem

### Pattern A: Refit - Return IHttpClientBuilder

Refit's approach is the simplest and most composable. The library's extension method returns `IHttpClientBuilder`, allowing consumers to chain any standard configuration:

```csharp
// Refit's extension method signature
public static IHttpClientBuilder AddRefitClient<T>(
    this IServiceCollection services,
    RefitSettings? settings = null) where T : class

// Consumer usage - full chaining
services.AddRefitClient<IGitHubApi>()
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.github.com"))
    .AddHttpMessageHandler<AuthHandler>()
    .AddStandardResilienceHandler();
```

**Key insight**: By returning `IHttpClientBuilder`, Refit doesn't need to reinvent configuration APIs. Consumers use the standard `IHttpClientBuilder` methods they already know.

**Refit's internal implementation**:
1. Calls `services.AddHttpClient(name)` to get an `IHttpClientBuilder`
2. Registers its own typed client implementation using `AddTypedClient`
3. Returns the `IHttpClientBuilder` for further chaining

### Pattern B: Azure SDK - Custom Builder with AddAzureClients

Azure SDK uses a completely custom builder (`AzureClientFactoryBuilder`) that wraps DI registration. This is a heavier pattern suited for SDKs with many clients, shared credentials, and configuration binding:

```csharp
services.AddAzureClients(clientBuilder =>
{
    // Register individual clients
    clientBuilder.AddBlobServiceClient(new Uri("https://..."));
    clientBuilder.AddSecretClient(config.GetSection("KeyVault"));

    // Shared credential
    clientBuilder.UseCredential(new DefaultAzureCredential());

    // Global defaults
    clientBuilder.ConfigureDefaults(config.GetSection("AzureDefaults"));

    // Named clients
    clientBuilder.AddBlobServiceClient(config.GetSection("PrivateStorage"))
        .WithName("PrivateStorage");
});
```

**Key features**:
- Single entry point `AddAzureClients` with a builder callback
- Shared credential management via `UseCredential`
- Configuration binding from `IConfiguration` sections
- Named clients via `.WithName("name")`
- `ConfigureDefaults` for global retry/diagnostics settings
- Each `Add*Client` method returns a sub-builder for per-client config

**When to use this pattern**: Only when you have a large family of clients with shared concerns (credentials, retry policies, configuration sections). Overkill for simpler libraries.

### Pattern C: Microsoft.Extensions.Http.Resilience - Extend IHttpClientBuilder

The resilience library extends `IHttpClientBuilder` with additional methods, following the standard chaining pattern:

```csharp
services.AddHttpClient("my-client")
    .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://api.example.com"))
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 3;
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(10);
    });
```

This is the same pattern as Refit but from the other direction - it adds capabilities to `IHttpClientBuilder` rather than starting the chain.

### Pattern D: StrawberryShake (current Nitro pattern) - Internal Named Client

StrawberryShake (which Nitro's `CommandLine.Client` uses) generates an `AddApiClient` method that:
1. Calls `services.AddHttpClient(ClientName)` internally
2. Registers all its internal services (store, serializers, operation types)
3. Does NOT return `IHttpClientBuilder` - it returns `IClientBuilder<T>`

This is a more opinionated/closed pattern because StrawberryShake manages its own DI internally.

---

## 3. Recommended Pattern for Library Authors

### The Golden Rule: Return IHttpClientBuilder

The .NET ecosystem convention is clear: **library extension methods should return `IHttpClientBuilder`** to enable consumer customization. This is the pattern used by:
- Refit (`AddRefitClient<T>()`)
- gRPC (`AddGrpcClient<T>()`)
- Microsoft.Extensions.Http.Resilience (`AddStandardResilienceHandler()`)
- Polly integration (`AddTransientHttpErrorPolicy()`)

### Recommended Extension Method Shape

```csharp
public static class NitroClientServiceCollectionExtensions
{
    // Single entry point - returns IHttpClientBuilder for chaining
    public static IHttpClientBuilder AddNitroClient(
        this IServiceCollection services,
        Action<NitroClientOptions> configureOptions)
    {
        // 1. Configure options via the Options pattern
        services.Configure(configureOptions);
        // or: services.AddOptions<NitroClientOptions>().Configure(configureOptions);

        // 2. Register the named HttpClient, get back IHttpClientBuilder
        var builder = services.AddHttpClient("NitroApi", (sp, client) =>
        {
            var options = sp.GetRequiredService<IOptions<NitroClientOptions>>().Value;
            client.BaseAddress = options.BaseAddress;
            // Set default headers...
        });

        // 3. Register all typed client interfaces as singletons
        //    (they use IHttpClientFactory internally, not injected HttpClient)
        services.TryAddSingleton<IApisClient, ApisClient>();
        services.TryAddSingleton<ISchemasClient, SchemasClient>();
        // ... etc

        // 4. Return IHttpClientBuilder so consumers can chain
        return builder;
    }
}
```

### Consumer Usage

```csharp
// Simple
services.AddNitroClient(options =>
{
    options.BaseAddress = new Uri("https://api.chillicream.com");
    options.ApiKey = "my-api-key";
});

// With full customization
services.AddNitroClient(options =>
{
    options.BaseAddress = new Uri("https://api.chillicream.com");
    options.Token = "bearer-token";
})
.AddHttpMessageHandler<MyCustomLoggingHandler>()
.AddStandardResilienceHandler()
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    Proxy = new WebProxy("http://proxy:8080")
});
```

---

## 4. DelegatingHandler Pattern for Auth

The recommended way to handle authentication in `IHttpClientFactory` is via a `DelegatingHandler`, not by setting headers in the `ConfigureHttpClient` delegate:

```csharp
// Library provides a built-in auth handler
internal class NitroAuthHandler : DelegatingHandler
{
    private readonly IOptions<NitroClientOptions> _options;

    public NitroAuthHandler(IOptions<NitroClientOptions> options)
    {
        _options = options;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var options = _options.Value;
        request.Headers.Add(options.AuthHeaderName, options.AuthHeaderValue);
        return await base.SendAsync(request, cancellationToken);
    }
}

// Register it inside the library's AddNitroClient
builder.AddHttpMessageHandler<NitroAuthHandler>();
```

**Why DelegatingHandler for auth**:
- Auth headers are set per-request, not once on the HttpClient
- Handlers can be replaced/wrapped by consumers
- Scoped dependencies (e.g., per-request tokens) work correctly
- Microsoft explicitly recommends this: "CONSIDER encapsulating all scope-related (e.g., authentication) logic in a separate DelegatingHandler"

---

## 5. Options Pattern Integration

### Use IOptions<T> Instead of Direct Func Delegates

The current Nitro implementation uses `Func<IServiceProvider, Uri>` and `Func<IServiceProvider, NitroAuthHeader>` on the options object. The standard .NET pattern uses `IOptions<T>` / `IConfigureOptions<T>`:

```csharp
public class NitroClientOptions
{
    public Uri? BaseAddress { get; set; }
    public string? ApiKey { get; set; }
    public string? BearerToken { get; set; }
    // ConfigureHttpClient is fine as a callback for "escape hatch" customization
    public Action<HttpClient>? ConfigureHttpClient { get; set; }
}
```

### Benefits of Options pattern:
1. **Configuration binding**: `services.Configure<NitroClientOptions>(config.GetSection("Nitro"))`
2. **Post-configuration**: `services.PostConfigure<NitroClientOptions>(o => o.Validate())`
3. **Named options**: Support multiple instances with different configs
4. **Validation**: `services.AddOptions<NitroClientOptions>().ValidateDataAnnotations()`
5. **Reload on change**: Automatic reload from `IConfiguration` changes

### Configuration from appsettings.json

```json
{
  "Nitro": {
    "BaseAddress": "https://api.chillicream.com",
    "ApiKey": "my-key"
  }
}
```

```csharp
services.AddNitroClient(options =>
    builder.Configuration.GetSection("Nitro").Bind(options));
```

---

## 6. Typed Clients vs Named Clients for Library Design

### For library authors: Use named clients internally

The Nitro library should use **named clients** (not typed clients) internally because:

1. **Typed clients are transient** - They receive a new `HttpClient` via constructor injection and cannot be captured in singletons. The current Nitro clients (e.g., `ApisClient`) are registered as singletons and use `IApiClient` (StrawberryShake's generated client) rather than raw `HttpClient`.

2. **Named clients work with singletons** - A singleton service can inject `IHttpClientFactory` and call `CreateClient("NitroApi")` whenever it needs an `HttpClient`. This is the correct pattern for long-lived services.

3. **Single HttpClient name** - All Nitro clients share the same API endpoint and auth, so one named client is sufficient. Each domain client (`IApisClient`, `ISchemasClient`, etc.) is a higher-level abstraction on top.

### Current Nitro architecture (already correct):
```
IHttpClientFactory (creates HttpClient by name)
  -> IApiClient (StrawberryShake generated, uses the named HttpClient)
    -> IApisClient, ISchemasClient, etc. (domain wrappers around IApiClient)
```

The domain clients are singletons that depend on `IApiClient`, which uses `IHttpClientFactory` internally. This is the right approach.

---

## 7. Analysis of Current Nitro Implementation

### Current Pattern (NitroClientServiceCollectionExtensions.cs)

```csharp
public static IServiceCollection AddNitroClients(
    this IServiceCollection services,
    Action<NitroApiClientOptions>? configure = null)
```

**Issues identified**:

1. **Returns `IServiceCollection` instead of `IHttpClientBuilder`** - Consumers cannot chain `AddHttpMessageHandler`, `AddStandardResilienceHandler`, etc. This is the biggest deviation from .NET conventions.

2. **`Func<IServiceProvider, T>` delegates on options** - `ResolveBaseAddress` and `ResolveAuthHeader` use `Func<IServiceProvider, T>` which is non-standard. The Options pattern with `IOptions<T>` and `IConfigureOptions<T>` is the conventional approach. If service-provider-based resolution is needed, use `IConfigureOptions<T>` which gets the provider via DI.

3. **Options not registered in DI** - `NitroApiClientOptions` is registered as a singleton manually (implied by `GetRequiredService<NitroApiClientOptions>()`), but not through the Options pattern (`IOptions<T>`). This prevents configuration binding, validation, and post-configuration.

4. **`TryAddNitroApiClient` doesn't use configure delegate** - The `configure` parameter is accepted but never applied in `TryAddNitroApiClient`. The `Action<NitroApiClientOptions>` is passed through but not invoked.

5. **`ConfigureHttpClientBuilder` on options is never used** - The options class has `Action<IHttpClientBuilder>? ConfigureHttpClientBuilder` but it's never called in the registration code.

6. **Separate service provider for IApiClient** - `CreateApiClient` builds a new `ServiceCollection` and `ServiceProvider` just to resolve `IApiClient`. This is unusual and could cause issues.

### Recommended Changes

Return `IHttpClientBuilder` from the main registration method so consumers can customize the HTTP pipeline:

```csharp
public static IHttpClientBuilder AddNitroClients(
    this IServiceCollection services,
    Action<NitroApiClientOptions> configure)
{
    services.Configure(configure);

    var builder = services.AddHttpClient(
        ApiClient.ClientName,
        static (sp, client) => ConfigureApiHttpClient(sp, client));

    // Register all domain clients
    services.TryAddSingleton<IApisClient, ApisClient>();
    services.TryAddSingleton<ISchemasClient, SchemasClient>();
    // ... etc

    return builder;
}
```

---

## 8. Summary: Recommended Approach for Nitro Client Library

| Aspect | Recommendation | Pattern Source |
|--------|---------------|----------------|
| **Entry point** | Single `AddNitroClients()` extension on `IServiceCollection` | Azure SDK, Refit |
| **Return type** | `IHttpClientBuilder` for consumer chaining | Refit, gRPC, Resilience |
| **Options** | `IOptions<NitroClientOptions>` via Options pattern | .NET conventions |
| **Auth** | Built-in `DelegatingHandler`, replaceable by consumer | Microsoft guidance |
| **Client registration** | Named client internally, singleton domain clients | IHttpClientFactory docs |
| **Customization** | Consumers chain `.AddHttpMessageHandler()`, `.ConfigurePrimaryHttpMessageHandler()`, etc. | Standard IHttpClientBuilder |
| **Configuration binding** | Support `IConfiguration` sections | Azure SDK, .NET conventions |
| **Validation** | `ValidateDataAnnotations()` or custom `IValidateOptions<T>` | .NET Options pattern |

---

## Sources

- [Use the IHttpClientFactory - .NET (Microsoft Learn)](https://learn.microsoft.com/en-us/dotnet/core/extensions/httpclient-factory)
- [Make HTTP requests using IHttpClientFactory in ASP.NET Core (Microsoft Learn)](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-10.0)
- [HttpClient guidelines for .NET (Microsoft Learn)](https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines)
- [Dependency injection with the Azure SDK for .NET (Microsoft Learn)](https://learn.microsoft.com/en-us/dotnet/azure/sdk/dependency-injection)
- [Azure client library integration for ASP.NET Core (Microsoft Learn)](https://learn.microsoft.com/en-us/dotnet/api/overview/azure/microsoft.extensions.azure-readme?view=azure-dotnet)
- [.NET Azure SDK Design Guidelines](https://azure.github.io/azure-sdk/dotnet_introduction.html)
- [Refit GitHub - HttpClientFactoryExtensions.cs](https://github.com/reactiveui/refit/blob/main/Refit.HttpClientFactory/HttpClientFactoryExtensions.cs)
- [Build resilient HTTP apps - .NET (Microsoft Learn)](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience)
- [Extending HttpClient with Delegating Handlers (Milan Jovanovic)](https://www.milanjovanovic.tech/blog/extending-httpclient-with-delegating-handlers-in-aspnetcore)
- [.NET HttpClient & Delegating Handlers (Duende)](https://duendesoftware.com/blog/20250902-dotnet-httpclient-and-delegating-handlers)
- [Exploring the code behind IHttpClientFactory (Andrew Lock)](https://andrewlock.net/exporing-the-code-behind-ihttpclientfactory/)
