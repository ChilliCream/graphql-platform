using HotChocolate.Fusion.Composition;

namespace HotChocolate.Fusion.CommandLine.Helpers;

internal sealed record SubgraphConfigurationDto
{
    public SubgraphConfigurationDto(string name,
        IReadOnlyList<IClientConfiguration>? clients = null)
    {
        Name = name;
        Clients = clients ?? Array.Empty<IClientConfiguration>();
    }

    public string Name { get; init; }

    public IReadOnlyList<IClientConfiguration> Clients { get; init; }

    public void Deconstruct(out string name, out IReadOnlyList<IClientConfiguration> clients)
    {
        name = Name;
        clients = Clients;
    }
}

