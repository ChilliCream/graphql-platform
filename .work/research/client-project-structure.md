# CommandLine.Client Project Structure Research

## Overview
The `ChilliCream.Nitro.Client` is a GraphQL client library built on StrawberryShake that provides domain-specific client interfaces for Nitro API operations. The project demonstrates a clean separation between public API surfaces (interfaces) and internal implementations.

## Assembly Details
- **Assembly Name**: ChilliCream.Nitro.Client
- **Root Namespace**: ChilliCream.Nitro.Client
- **Target Frameworks**: net8.0, net9.0, net10.0
- **Key Dependencies**:
  - StrawberryShake.Core
  - StrawberryShake.Transport.Http
  - HotChocolate.Fusion.Utilities
  - Microsoft.Extensions.Http
  - System.Linq.Async
  - System.Reactive

## Public API Surface

### Client Interfaces (All Public)
Each domain area exposes a public interface only - implementations are internal:

1. **IFusionConfigurationClient** - Fusion configuration operations (publish, validate, download)
2. **IApisClient** - API management (list, create, delete, show, update settings)
3. **IApiKeysClient** - API key management (list, create, delete, revoke)
4. **IClientsClient** - Client management (list, create, delete, download, upload, unpublish)
5. **IEnvironmentsClient** - Environment operations (list, create, set defaults)
6. **IMcpClient** - MCP feature collection operations (list, create, delete, upload)
7. **IMocksClient** - Mock schema operations (list, create, update, download)
8. **IOpenApiClient** - OpenAPI collection operations (list, create, delete, upload, validate)
9. **IPersonalAccessTokensClient** - Personal access token management (list, create, revoke)
10. **ISchemasClient** - Schema operations (list, download, upload, validate)
11. **IStagesClient** - Stage management (list, create, delete, edit)
12. **IWorkspacesClient** - Workspace operations (list, create, list path, show)

### Configuration & Options (Public)
- **NitroApiClientOptions** - Configuration class for base address and auth resolution
  - `Func<IServiceProvider, Uri>? ResolveBaseAddress` - Must be configured
  - `Func<IServiceProvider, NitroAuthHeader>? ResolveAuthHeader` - Must be configured
  - `Action<HttpClient>? ConfigureHttpClient` - Optional HTTP client customization
  - `Action<IHttpClientBuilder>? ConfigureHttpClientBuilder` - Optional builder customization

- **NitroAuthHeader** - Auth header record struct
  - `static NitroAuthHeader Bearer(string token)` - Creates "Authorization: Bearer {token}"
  - `static NitroAuthHeader ApiKey(string apiKey)` - Creates "CCC-api-key: {apiKey}"

### Shared Types (Public)
Located in `Shared/`:
- **MutationError** - Base abstract record for mutation errors
  - Concrete implementations: UnknownMutationError, ConcurrentOperationError, ProcessingTimeoutError, ReadyTimeoutError, SchemaVersionSyntaxError, etc.
- **SchemaChangeViolationError** - Detailed schema violation information
- **SchemaChangeSeverityKind** - Enum for change severity levels

### Models
Located in `Models/`:
- **ConnectionPage<T>** - Generic pagination container
- Various domain models (SourceMetadata, ValidationUpdate, PublishUpdate, etc.)

## Internal Implementation Details

### Client Implementations (All Internal)
Each public interface has a corresponding internal sealed class implementation:

```
Public Interface          Internal Implementation
─────────────────────────────────────────────────
IFusionConfigurationClient    → FusionConfigurationClient
IApisClient                   → ApisClient
IApiKeysClient                → ApiKeysClient
IClientsClient                → ClientsClient
IEnvironmentsClient           → EnvironmentsClient
IMcpClient                    → McpClient
IMocksClient                  → MocksClient
IOpenApiClient                → OpenApiClient
IPersonalAccessTokensClient   → PersonalAccessTokensClient
ISchemasClient                → SchemasClient
IStagesClient                 → StagesClient
IWorkspacesClient             → WorkspacesClient
```

### Dependency Injection Architecture

#### HTTP Client Factory Integration
All clients use `IHttpClientFactory` - specifically a **named client** approach:

```csharp
// From FusionConfigurationClient.cs (line 203)
using var httpClient = httpClientFactory.CreateClient(ApiClient.ClientName);
```

The HTTP client is registered with the name: `ApiClient.ClientName` (constant from generated ApiClient)

#### Constructor Patterns
Clients fall into two categories:

**Category 1: IApiClient Consumers (Most Clients)**
```csharp
internal sealed class ApisClient(IApiClient apiClient) : IApisClient
internal sealed class ApiKeysClient(IApiClient apiClient) : IApiKeysClient
internal sealed class MocksClient(IApiClient apiClient) : IMocksClient
```

These clients take only `IApiClient` and use it to execute GraphQL operations.

**Category 2: Dual Dependencies (FusionConfigurationClient)**
```csharp
internal sealed class FusionConfigurationClient(
    IApiClient apiClient,
    IHttpClientFactory httpClientFactory)
```

This client needs both:
- `IApiClient` for GraphQL operations (mutations/subscriptions)
- `IHttpClientFactory` for direct HTTP calls (archive downloads/uploads)

#### The IApiClient Resolution
From `NitroClientServiceCollectionExtensions.TryAddNitroApiClient()`:

```csharp
// Step 1: Register named HttpClient
var clientBuilder = services.AddHttpClient(
    ApiClient.ClientName,
    static (serviceProvider, client) => ConfigureApiHttpClient(serviceProvider, client));

// Step 2: Create IApiClient from the named HttpClient
services.TryAddSingleton<IApiClient>(CreateApiClient);

private static IApiClient CreateApiClient(IServiceProvider serviceProvider)
{
    var services = new ServiceCollection();
    services.AddSingleton(serviceProvider.GetRequiredService<IHttpClientFactory>());
    services.AddApiClient();  // From StrawberryShake

    return services.BuildServiceProvider().GetRequiredService<IApiClient>();
}
```

This creates a **nested service provider** that builds the StrawberryShake-generated `IApiClient`.

### HTTP Client Configuration
All HTTP clients configured with:
- **Base Address**: Resolved via `NitroApiClientOptions.ResolveBaseAddress(IServiceProvider)`
- **Auth Header**: Resolved via `NitroApiClientOptions.ResolveAuthHeader(IServiceProvider)`
- **Default Headers**:
  - `Accept: application/json`
  - `GraphQL-Client-Version: {version}`
  - `ccc-agent: Nitro CLI/{version}`
  - `GraphQL-Preflight: 1`
- **HTTP/2 enabled** with fallback to HTTP/1.1

### Shared Helper Classes (Internal)
Located in `Shared/`:
- **OperationResultHelper** - Unwraps GraphQL operation results
- **SourceMetadataMapper** - Maps domain source metadata to API input
- **MutationErrorMapper** - Maps API errors to domain error types
- **CancellationTokenExtensions** - Cancellation utilities

## Dependency Injection Extensions API

### Monolithic Registration
```csharp
public static IServiceCollection AddNitroClients(
    this IServiceCollection services,
    Action<NitroApiClientOptions>? configure = null)
```

Registers all 12 clients plus the base IApiClient in one call.

### Individual Client Registration Methods
Each client has its own extension method:
```csharp
public static IServiceCollection AddNitroApisClient(
    this IServiceCollection services,
    Action<NitroApiClientOptions>? configure = null)
```

All methods:
- Call `TryAddNitroApiClient()` to ensure base infrastructure (they `TryAdd`, so redundant calls don't duplicate)
- Register the domain client as a singleton
- Accept optional configuration lambda

### Configuration Pattern
```csharp
services.AddNitroClients(options =>
{
    options.ResolveBaseAddress = sp => new Uri("https://api.example.com");
    options.ResolveAuthHeader = sp => NitroAuthHeader.Bearer(token);
    options.ConfigureHttpClient = client => { /* customize */ };
});
```

## Operational Patterns

### GraphQL Operation Pattern
Standard across all domain clients:
```csharp
var result = await apiClient.SomeOperation.ExecuteAsync(inputs, cancellationToken);
var data = OperationResultHelper.EnsureData(result);
return data.SomeProperty;
```

### Stream/Subscription Pattern
FusionConfigurationClient demonstrates reactive streaming:
```csharp
IAsyncEnumerable<IOnFusionConfigurationPublishingTaskChanged_...>
    SubscribeToFusionConfigurationPublishingTaskChangedAsync(...)
{
    using var stopSignal = new ReplaySubject<Unit>(1);
    await using var _ = cancellationToken.Register(stopSignal);

    var subscription = apiClient.OnFusionConfigurationPublishingTaskChanged
        .Watch(requestId, ExecutionStrategy.NetworkOnly)
        .TakeUntil(stopSignal);

    await foreach (var @event in subscription.ToAsyncEnumerable())
    {
        yield return OperationResultHelper.EnsureData(@event)
            .OnFusionConfigurationPublishingTaskChanged;
    }
}
```

### Direct HTTP Pattern (Archive Operations)
FusionConfigurationClient uses direct HTTP for binary operations:
```csharp
using var httpClient = httpClientFactory.CreateClient(ApiClient.ClientName);
using var response = await httpClient.SendAsync(request, cancellationToken);
// Handle response with specific status codes
```

## Code Organization

### Directory Structure
```
CommandLine.Client/
├── Extensions/
│   └── NitroClientServiceCollectionExtensions.cs  (Public API registration)
├── [Domain]/
│   ├── I[Domain]Client.cs                        (Public interface)
│   ├── [Domain]Client.cs                         (Internal implementation)
│   ├── Models/                                   (Domain-specific models)
│   └── Operations/                               (Generated GraphQL operations)
├── Shared/
│   ├── MutationErrors.cs                        (Public error types)
│   ├── OperationResultHelper.cs
│   ├── SourceMetadataMapper.cs
│   ├── MutationErrorMapper.cs
│   └── CancellationTokenExtensions.cs
├── Exceptions/
│   ├── NitroClientException.cs
│   ├── NitroClientAuthorizationException.cs
│   └── NitroClientNotFoundException.cs
├── Generated/
│   └── ApiClient.Client.cs                       (StrawberryShake generated)
├── Models/
│   ├── ConnectionPage.cs
│   └── [Domain models]
├── NitroAuthHeader.cs                           (Public)
├── NitroApiClientOptions.cs                     (Public)
├── InternalsVisibleTo.cs                        (Exposes internals to tests)
└── ChilliCream.Nitro.Client.csproj
```

### Namespace Hierarchy
- `ChilliCream.Nitro.Client` - Root (public config, auth, exceptions)
- `ChilliCream.Nitro.Client.Apis` - APIs domain
- `ChilliCream.Nitro.Client.ApiKeys` - API keys domain
- `ChilliCream.Nitro.Client.Clients` - Clients domain
- `ChilliCream.Nitro.Client.Environments` - Environments domain
- `ChilliCream.Nitro.Client.FusionConfiguration` - Fusion domain
- `ChilliCream.Nitro.Client.Mcp` - MCP domain
- `ChilliCream.Nitro.Client.Mocks` - Mocks domain
- `ChilliCream.Nitro.Client.OpenApi` - OpenAPI domain
- `ChilliCream.Nitro.Client.PersonalAccessTokens` - Personal access tokens domain
- `ChilliCream.Nitro.Client.Schemas` - Schemas domain
- `ChilliCream.Nitro.Client.Stages` - Stages domain
- `ChilliCream.Nitro.Client.Workspaces` - Workspaces domain

## InternalsVisibleTo
The project exposes internal types to:
- `ChilliCream.Nitro.CommandLine.Tests` - For testing the command line
- `DynamicProxyGenAssembly2` - For Moq proxy generation (mocking frameworks)

This allows tests to:
- Instantiate internal client implementations directly
- Mock internal clients for unit testing
- Access OperationResultHelper and other shared utilities

## Key Design Patterns

### 1. Facade Pattern
Each domain client (e.g., `ApisClient`) acts as a facade over StrawberryShake's low-level GraphQL operations, providing type-safe domain methods.

### 2. Single Responsibility
- Domain clients: Business operation orchestration
- FusionConfigurationClient: Hybrid - both GraphQL and binary HTTP operations
- OperationResultHelper: Result unwrapping and error detection
- MutationErrorMapper: Error translation

### 3. Options Pattern
`NitroApiClientOptions` follows the ASP.NET Core options pattern for DI configuration.

### 4. Named HTTP Client
Single named client (`ApiClient.ClientName`) supports multiple domain clients, reducing socket exhaustion risk.

### 5. Reactive Streaming
Uses System.Reactive for subscription-based operations with clean cancellation handling.

## Configuration Validation
`NitroApiClientOptions.EnsureValid()` is called during DI setup to validate:
- `ResolveBaseAddress` is configured
- `ResolveAuthHeader` is configured
- Resolved base address is absolute
- Auth header name/value are non-empty

## Exception Hierarchy
All client exceptions inherit from `NitroClientException`:
- `NitroClientException` - Base, general transport/server errors
- `NitroClientAuthorizationException` - HTTP 401/403 responses
- `NitroClientNotFoundException` - HTTP 404 responses (entity not found)

Domain clients throw these based on HTTP response status codes or GraphQL error analysis.

## HTTP Client Customization Points

1. **Via NitroApiClientOptions**:
   ```csharp
   options.ConfigureHttpClient = client =>
   {
       client.Timeout = TimeSpan.FromSeconds(30);
       client.DefaultRequestHeaders.Add("X-Custom", "value");
   };
   ```

2. **Via IHttpClientBuilder**:
   ```csharp
   options.ConfigureHttpClientBuilder = builder =>
   {
       builder.AddPolicyHandler(/* Polly policy */);
   };
   ```

## Summary of Key Findings

| Aspect | Details |
|--------|---------|
| **Client Count** | 12 domain clients + 1 base IApiClient |
| **HTTP Factory Usage** | Named client approach: `ApiClient.ClientName` |
| **Constructor Pattern** | Most take only IApiClient; FusionConfigurationClient also takes IHttpClientFactory |
| **Registrations** | One monolithic `AddNitroClients()` + 12 individual extensions |
| **Public API** | Only interfaces, options, auth headers, and error types |
| **Visibility** | Implementations are internal; exposed to tests via InternalsVisibleTo |
| **GraphQL Library** | StrawberryShake 11.0.0 generated |
| **Configuration Required** | ResolveBaseAddress and ResolveAuthHeader are mandatory |
