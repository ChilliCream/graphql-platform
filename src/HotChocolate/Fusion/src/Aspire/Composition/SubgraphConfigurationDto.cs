using System.Text.Json;

namespace HotChocolate.Fusion.Composition;

internal sealed class SubgraphConfigurationDto(
    string name,
    IReadOnlyList<IClientConfiguration>? clients = null,
    JsonElement? extensions = null)
{
    public string Name { get; } = name;

    public IReadOnlyList<IClientConfiguration> Clients { get; } = clients ?? Array.Empty<IClientConfiguration>();

    public JsonElement? Extensions { get; } = extensions;
}
