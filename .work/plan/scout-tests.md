# Scout: Test Infrastructure for CommandLine.Client

## Summary

The CommandLine.Tests project uses a comprehensive test infrastructure centered around the `CommandBuilder` pattern. All tests mock typed client interfaces directly; there are NO tests specifically for the `NitroClientServiceCollectionExtensions`, `NitroApiClientOptions`, or `AddNitroClients` registration methods. This is an important gap — the current design assumes the registration works but never validates it in tests.

## Key Findings

### 1. No Tests Covering Service Registration

- **Search Results**: Grep for `AddNitroClients`, `NitroClientServiceCollection`, and `NitroApiClientOptions` found NO test files in `/test/CommandLine.Tests/`.
- **No CommandLine.Client.Tests Project**: There is no dedicated test project for the client library itself.
- **The Gap**: The registration infrastructure is implemented but untested. If we redesign it to return `IHttpClientBuilder`, we'll need to add tests to prevent regressions.

### 2. Test Pattern: CommandBuilder + Direct Client Mocking

All command tests follow this pattern:

```csharp
var client = new Mock<IApisClient>(MockBehavior.Strict);
client.Setup(x => x.CreateApiAsync(...))
    .ReturnsAsync(payload);

var result = await new CommandBuilder()
    .AddService(client.Object)    // <-- mock replaces real client
    .AddApiKey()
    .AddInteractionMode(mode)
    .AddArguments(...)
    .ExecuteAsync();
```

**How It Works**:
- `CommandBuilder` constructs a full `IServiceCollection` with all CLI services
- In the constructor, `AddMockedNitroClients()` registers all typed clients as `Mock.Of<T>()` (loose mocks)
- Tests call `.AddService(client.Object)` to replace specific clients with strict mocks
- Real `IHttpClientFactory`, `HttpClient`, and API client implementations are **never tested** in command tests

### 3. CommandBuilder: Full DI Setup, Not Just Client Registration

File: `/Users/tobiastengler/src/ai/platform-2/src/Nitro/CommandLine/test/CommandLine.Tests/CommandBuilder.cs`

Key aspects:
- Line 33-35: Calls `AddNitroCommands()` and `AddNitroServices()` in its constructor
- Line 37: **Does NOT** call `AddNitroClients()`
- Line 39: Instead, calls custom `AddMockedNitroClients()` directly (all typed clients are `Mock.Of<T>()`)
- Line 78-84: `.AddService<T>()` uses `ServiceDescriptor.Replace()` to swap mocks

**Implication**: The CommandBuilder bypasses the real registration entirely. Tests never verify that the registration works.

### 4. Current Registration Infrastructure

File: `/Users/tobiastengler/src/ai/platform-2/src/Nitro/CommandLine/src/CommandLine.Client/Extensions/NitroClientServiceCollectionExtensions.cs`

- `AddNitroClients()` calls `AddNitroApisClient()`, `AddNitroApiKeysClient()`, etc.
- Each `AddNitro*Client()` method calls `TryAddNitroApiClient(services, configure)` (line 188)
- `TryAddNitroApiClient()` calls `AddHttpClient(ApiClient.ClientName, ConfigureApiHttpClient)`
  - Line 192: `services.AddHttpClient(ApiClient.ClientName, ConfigureApiHttpClient)`
  - **Returns nothing** — the HttpClient is configured but the builder is discarded
- `ConfigureApiHttpClient()` expects `NitroApiClientOptions` to be pre-registered in the service provider

**Crucial Gap**: `NitroApiClientOptions` must be registered **before** `AddNitroClients()` is called, but the extension method has no mechanism to do this. This is configured elsewhere in the CLI.

### 5. Where NitroApiClientOptions Is Registered

File: `/Users/tobiastengler/src/ai/platform-2/src/Nitro/CommandLine/src/CommandLine/Program.cs`

```csharp
var services = new ServiceCollection();
services
    .AddNitroServices()
    .AddNitroClients()           // <-- expects NitroApiClientOptions already registered
    .AddSingleton<INitroConsole>(...)
    .AddNitroCommands();
```

**Problem**: `NitroApiClientOptions` registration is not visible in Program.cs. Either:
1. It's registered in `AddNitroServices()` or `AddNitroCommands()`, OR
2. It's missing and the code relies on runtime resolution (likely a bug)

### 6. NitroApiClientOptions Definition

File: `/Users/tobiastengler/src/ai/platform-2/src/Nitro/CommandLine/src/CommandLine.Client/NitroApiClientOptions.cs`

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
            throw new InvalidOperationException(...);
        if (ResolveAuthHeader is null)
            throw new InvalidOperationException(...);
    }
}
```

**Key Point**: The `ConfigureHttpClientBuilder` property is already defined but **never used** in `TryAddNitroApiClient()`. This suggests incomplete refactoring toward an `IHttpClientBuilder`-based API.

### 7. TestHelpers.cs: Minimal Test Utilities

File: `/Users/tobiastengler/src/ai/platform-2/src/Nitro/CommandLine/test/CommandLine.Tests/TestHelpers.cs`

Only provides `ToAsyncEnumerable<T>()` — a simple helper for converting sync collections to async streams. No mocking utilities or client registration helpers.

### 8. Test Files Sampled

Checked 3 representative test files to confirm the pattern:

- **CreateApiCommandTests.cs** (70 tests): All mock `IApisClient` directly via `.AddService()`, no client registration tests
- **UploadClientCommandTests.cs**: Same pattern, mocks `IClientsClient` and file system
- **CreateApiKeyCommandTests.cs**: Same pattern, mocks `IApisClient` and `IApiKeysClient`

## Implications for API Redesign

### Current Test Strategy
- Tests are **command-layer** focused, not **infrastructure** focused
- No tests validate that `AddNitroClients()` wires up the HTTP stack correctly
- Changing from options-based config to `IHttpClientBuilder` return value **requires new tests**

### What Tests Are Missing
1. **Integration tests for service registration**: Verify `AddNitroClients()` correctly wires `IHttpClientFactory`, `HttpClient`, and typed clients
2. **Options configuration tests**: Verify `NitroApiClientOptions` is correctly resolved at runtime
3. **Builder pattern tests** (if redesigning): Test that the returned `IHttpClientBuilder` can be further customized

### Test Infrastructure Pattern to Match
- **Use a real `ServiceCollection`** (not mocks) to test registration
- **Build the real service provider** and resolve typed clients
- **Verify the HTTP client is configured** with correct headers, base address, auth, etc.
- **Keep command tests as-is**: They remain command-focused, not infrastructure-focused

## Recommendations

1. **Add a new test file**: `CommandLine.Client.Tests.csproj` with tests for service registration
   - OR: Add tests to `CommandLine.Tests` in a new `Services/` folder
2. **Test patterns to add**:
   - "AddNitroClients_Should_RegisterAllClients"
   - "AddNitroClients_With_Configuration_Should_ApplySettings"
   - "ConfigureApiHttpClient_Should_SetHeaders_And_BaseAddress"
3. **Verify existing CLI works**: Run the CLI in isolation to confirm current registration is actually functional (not broken by incomplete refactoring)
4. **Adopt the new test pattern** once the API is redesigned to return `IHttpClientBuilder`
