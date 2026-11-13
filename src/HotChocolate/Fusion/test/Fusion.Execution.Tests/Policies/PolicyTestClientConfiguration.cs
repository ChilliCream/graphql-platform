using HotChocolate.Fusion.Execution.Clients;

namespace HotChocolate.Fusion.Policies;

internal sealed class PolicyTestClientConfiguration(string name) : ISourceSchemaClientConfiguration
{
    public string Name { get; } = name;

    public SupportedOperationType SupportedOperations { get; } = SupportedOperationType.All;
}
