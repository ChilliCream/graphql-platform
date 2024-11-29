using System.Text.Json;
using HotChocolate.Fusion.Composition;

namespace HotChocolate.Fusion.CommandLine.Helpers;

internal sealed record SubgraphConfigurationDto
{
    public SubgraphConfigurationDto(
        string name,
        IReadOnlyList<IClientConfiguration>? clients = null,
        JsonElement? extensions = null)
    {
        Name = name;
        Clients = clients ?? Array.Empty<IClientConfiguration>();
        Extensions = extensions;
    }

    public string Name { get; init; }

    public IReadOnlyList<IClientConfiguration> Clients { get; init; }

    public JsonElement? Extensions { get; init; }

    public void Deconstruct(
        out string name,
        out IReadOnlyList<IClientConfiguration> clients,
        out JsonElement? extensions)
    {
        name = Name;
        clients = Clients;
        extensions = Extensions;
    }
}
