using System.Collections.Immutable;

namespace HotChocolate.Adapters.Mcp.Storage;

public sealed record OpenAiComponentCsp(
    ImmutableArray<string>? ConnectDomains,
    ImmutableArray<string>? ResourceDomains);
