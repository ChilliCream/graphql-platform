# Scout Report: HttpClient Configuration Registration & CLI Options

**Date**: 2026-03-29
**Status**: Complete scouting for planning phase
**Context**: Redesigning HttpClient registration to return `IHttpClientBuilder` instead of `NitroApiClientOptions`.

---

## 1. NitroClientServiceCollectionExtensions Analysis

**File**: `/Users/tobiastengler/src/ai/platform-2/src/Nitro/CommandLine/src/CommandLine.Client/Extensions/NitroClientServiceCollectionExtensions.cs`

### Per-Client Methods Signatures

All 12 client registration methods follow identical pattern:
```csharp
public static IServiceCollection AddNitro{Service}Client(
    this IServiceCollection services,
    Action<NitroApiClientOptions>? configure = null)
```

Methods:
- `AddNitroApisClient`
- `AddNitroApiKeysClient`
- `AddNitroClientsClient`
- `AddNitroEnvironmentsClient`
- `AddNitroMcpClient`
- `AddNitroMocksClient`
- `AddNitroOpenApiClient`
- `AddNitroPersonalAccessTokensClient`
- `AddNitroSchemasClient`
- `AddNitroStagesClient`
- `AddNitroWorkspacesClient`
- `AddNitroFusionConfigurationClient`

All wrap `TryAddNitroApiClient(services, configure)` plus register the client interface.

### Critical Issue: Broken Pattern

**`TryAddNitroApiClient` (line 188-200)**:
```csharp
internal static IServiceCollection TryAddNitroApiClient(
    this IServiceCollection services,
    Action<NitroApiClientOptions>? configure)
{
    var clientBuilder = services.AddHttpClient(
        ApiClient.ClientName,
        static (serviceProvider, client) => ConfigureApiHttpClient(serviceProvider, client));

    services.TryAddSingleton<IApiClient>(CreateApiClient);
    services.AddSingleton<ApiClientRegistrationMarker>();

    return services;
}
```

**Problem**:
- Line 192: `AddHttpClient()` is called and returns `IHttpClientBuilder`, but it is **assigned to `clientBuilder` and never used**
- The `configure` parameter is **never invoked**
- `ConfigureHttpClientBuilder` property of `NitroApiClientOptions` is **never called**
- `NitroApiClientOptions` is **never registered** in the service collection, yet it's required by `ConfigureApiHttpClient()`

### `ConfigureApiHttpClient` Analysis (line 202-244)

Attempts to get `NitroApiClientOptions` from the service provider:
```csharp
var options = serviceProvider.GetRequiredService<NitroApiClientOptions>();
var baseAddress = options.ResolveBaseAddress!(serviceProvider);
var authHeader = options.ResolveAuthHeader!(serviceProvider);
```

**This will FAIL at runtime** — `NitroApiClientOptions` is never registered in DI.

Sets headers from `NitroApiClientOptions`:
- User agent
- GraphQL Client Version
- CCC Agent
- GraphQL Preflight
- Accept: application/json
- Auth header

Calls `options.ConfigureHttpClient?.Invoke(client)` (line 243) — this is invoked but `ConfigureHttpClientBuilder` is ignored.

---

## 2. NitroApiClientOptions Definition

**File**: `/Users/tobiastengler/src/ai/platform-2/src/Nitro/CommandLine/src/CommandLine.Client/NitroApiClientOptions.cs`

```csharp
public sealed class NitroApiClientOptions
{
    public Func<IServiceProvider, Uri>? ResolveBaseAddress { get; set; }
    public Func<IServiceProvider, NitroAuthHeader>? ResolveAuthHeader { get; set; }
    public Action<HttpClient>? ConfigureHttpClient { get; set; }
    public Action<IHttpClientBuilder>? ConfigureHttpClientBuilder { get; set; }

    internal void EnsureValid()
    {
        if (ResolveBaseAddress is null)
            throw new InvalidOperationException(
                $"{nameof(ResolveBaseAddress)} must be configured.");
        if (ResolveAuthHeader is null)
            throw new InvalidOperationException(
                $"{nameof(ResolveAuthHeader)} must be configured.");
    }
}
```

**Status**:
- `ResolveBaseAddress` and `ResolveAuthHeader` are **required** and checked in `EnsureValid()`
- `ConfigureHttpClient` is **invoked** (line 243)
- `ConfigureHttpClientBuilder` is **defined but never used** — dead code

---

## 3. CLI Options: Cloud URL & API Key

### OptionalCloudUrlOption
**File**: `/Users/tobiastengler/src/ai/platform-2/src/Nitro/CommandLine/src/CommandLine/Options/OptionalCloudUrlOption.cs`

```csharp
internal sealed class OptionalCloudUrlOption : Option<string>
{
    public OptionalCloudUrlOption() : base("--cloud-url")
    {
        Description = "The URL of the API.";
        Required = false;
        this.DefaultFromEnvironmentValue("CLOUD_URL",
            defaultValue: Constants.ApiUrl["https://".Length..]);
        // Defaults to "api.chillicream.com"
    }
}
```

**Type**: `Option<string>` (System.CommandLine)
**Default**: `"api.chillicream.com"` (from `Constants.ApiUrl`)
**Environment**: `CLOUD_URL`
**Scope**: Global (added to all commands via `AddGlobalNitroOptions()`)

### OptionalApiKeyOption
**File**: `/Users/tobiastengler/src/ai/platform-2/src/Nitro/CommandLine/src/CommandLine/Options/OptionalApiKeyOption.cs`

```csharp
internal sealed class OptionalApiKeyOption : Option<string>
{
    public const string OptionName = "--api-key";

    public OptionalApiKeyOption() : base(OptionName)
    {
        Description = "The API key that is used for the authentication";
        Required = false;
        this.DefaultFromEnvironmentValue("API_KEY");
    }
}
```

**Type**: `Option<string>` (System.CommandLine)
**Default**: None (only from environment)
**Environment**: `API_KEY`
**Scope**: Global (added to all commands via `AddGlobalNitroOptions()`)
**OptionName Constant**: Reusable in code for string-based lookups

### No Separate "Required" ApiKeyOption Found

Search results show only `OptionalApiKeyOption`. No distinct `ApiKeyOption` class exists.

---

## 4. Current Usage in Command Handlers

### How Global Options Are Added

**File**: `/Users/tobiastengler/src/ai/platform-2/src/Nitro/CommandLine/src/CommandLine/Extensions/CommandExtensions.cs` (line 56-63)

```csharp
public static Command AddGlobalNitroOptions(this Command command)
{
    command.Options.Add(Opt<OptionalCloudUrlOption>.Instance);
    command.Options.Add(Opt<OptionalApiKeyOption>.Instance);
    command.Options.Add(Opt<OptionalOutputFormatOption>.Instance);

    return command;
}
```

**Scope**: Every command calls `this.AddGlobalNitroOptions()` in constructor.
**Current Usage**: Options are **parsed by System.CommandLine** but **never accessed in handlers**.

### Example: CreateApiKeyCommand

**File**: `/Users/tobiastengler/src/ai/platform-2/src/Nitro/CommandLine/src/CommandLine/Commands/ApiKeys/CreateApiKeyCommand.cs` (line 29)

```csharp
this.AddGlobalNitroOptions();
```

The global options are added but **never read** from `parseResult` in `ExecuteAsync()`.

### Authentication Pattern in Commands

**Current approach** (observed in `CreateApiKeyCommand`):
1. Line 53: `parseResult.AssertHasAuthentication(sessionService)` — checks if user is logged in
2. No `--cloud-url` or `--api-key` override is used
3. All commands use whatever is configured in `ISessionService` (session-based auth)

**No API-key or cloud-url CLI override is currently implemented** — the global options are parsed but not applied.

---

## 5. Session Service & Authentication Flow

### ISessionService

Not fully examined, but from usage in commands:
- `sessionService.GetWorkspaceId()`
- Session represents logged-in user state
- All commands defer to session for auth details

### Missing Piece: Where CLI Options → HttpClient

**Current state**:
- `--api-key` and `--cloud-url` are parsed globally
- But they are **never passed to the HttpClient configuration**
- `NitroApiClientOptions` is never created with these values

---

## 6. BuildSecrets Generation

**File**: `/Users/tobiastengler/src/ai/platform-2/src/Nitro/CommandLine/src/CommandLine/Directory.Build.targets`

Generates `Secrets.g.cs` at compile time with:
```csharp
internal static class BuildSecrets
{
    internal const string NitroApiClientId = "$(NitroApiClientId)";
    internal const string NitroIdentityClientId = "$(NitroIdentityClientId)";
    internal const string NitroIdentityScopes = "$(NitroIdentityScopes)";
}
```

**Status**:
- `NitroApiClientId` is **generated but never used** (no references found)
- `NitroIdentityClientId` and `NitroIdentityScopes` are used in `OidcConfiguration`

### Headers.cs Usage

**File**: `/Users/tobiastengler/src/ai/platform-2/src/Nitro/CommandLine/src/CommandLine/Configuration/Headers.cs`

```csharp
internal static class Headers
{
    public static readonly string ApiKey = "CCC-api-key";
    public static readonly string GraphQLPreflight = "GraphQL-Preflight";
    public static readonly string CCCAgent = "ccc-agent";
    public static readonly string GraphQLClientId = "GraphQL-Client-Id";
    public static readonly string GraphQLClientVersion = "GraphQL-Client-Version";
}
```

**Status**:
- `GraphQLClientId` is **defined but never set** (no assignments found)
- All other headers are set in `ConfigureApiHttpClient()`

---

## 7. Summary of Broken Pieces

| Component | Current State | Issue |
|-----------|---------------|-------|
| `NitroApiClientOptions` | Defined | Never instantiated or registered in DI |
| `ResolveBaseAddress` | Required property | Must be set but no current implementation |
| `ResolveAuthHeader` | Required property | Must be set but no current implementation |
| `ConfigureHttpClientBuilder` | Defined property | Assigned by callers but never invoked |
| `TryAddNitroApiClient()` | Returns unused `IHttpClientBuilder` | Never applies `configure` callback |
| `--cloud-url` option | Parsed globally | Never applied to HttpClient base address |
| `--api-key` option | Parsed globally | Never applied to auth header |
| `NitroApiClientId` | Generated in BuildSecrets | Never used |
| `GraphQLClientId` header | Defined but never set | Unused in ConfigureApiHttpClient() |

---

## 8. Design Implications for New Registration Pattern

### Current Flow (Broken)
```
Program.Main()
  → AddNitroClients()  // no arguments
      → TryAddNitroApiClient(services, configure: null)
          → AddHttpClient(name, configurator)
          → clientBuilder is discarded (unused)
          → NitroApiClientOptions never registered
          → Runtime: ConfigureApiHttpClient fails getting options from DI
```

### Redesigned Flow (Proposed)
```
Program.Main()
  → AddNitroClients(Action<IHttpClientBuilder> configure)
      → For each client:
          → TryAddNitroApiClient(services, configure)
              → var builder = AddHttpClient(name, configurator)
              → Register NitroApiClientOptions or builder config
              → Invoke configure(builder) to let caller add policies
              → All 12 clients share the same builder state
```

---

## 9. No "ApiKeyOption" vs "OptionalApiKeyOption" Split

Only one class: `OptionalApiKeyOption`

No separate required version exists. This suggests:
- All commands should support optional API key auth (alongside session auth)
- Or: commands are grouped differently (some require auth, some don't)

The plan structure suggests auth is optional per command, not per global option type.

---

## 10. Recommendations for Scout Conclusion

1. **Root cause**: `NitroApiClientOptions` registration is missing; `TryAddNitroApiClient` discards the builder.
2. **Design shift**: Introduce an `Action<IHttpClientBuilder>` parameter to `AddNitroClients()` and pass it to each per-client method, ensuring all 12 clients are configured consistently.
3. **CLI integration**: Post-registration, extract `--cloud-url` and `--api-key` from the root command's `parseResult` and apply them to the HttpClient before any command executes.
4. **Dead code**: `NitroApiClientId`, `ConfigureHttpClientBuilder`, and `GraphQLClientId` are currently unused — clarify if they should be removed or activated.
5. **Test implications**: Registering `NitroApiClientOptions` in DI requires a test harness that can instantiate the full chain: CLI parse → options → DI → HttpClient.

