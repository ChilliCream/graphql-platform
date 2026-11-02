using System.Text.Json.Serialization;

namespace HotChocolate.Fusion.Aspire.Nitro;

/// <summary>
/// The serialization contract for the files that the Nitro integration reads and writes. The
/// naming policy matches the Nitro CLI so the session file can be read as the CLI writes it.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    WriteIndented = true)]
[JsonSerializable(typeof(NitroSession))]
[JsonSerializable(typeof(NitroSeedMetadata))]
internal sealed partial class NitroJsonContext : JsonSerializerContext;
