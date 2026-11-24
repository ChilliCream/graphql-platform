using System.Collections.Immutable;

namespace HotChocolate.Types;

/// <summary>
/// Defines the selections below a field that run one after the other, each after its predecessor has completed
/// its own resolver task. Member paths cannot pass through list fields, abstract types, other group roots or
/// batch resolver fields, and every field on a member path must be parallel executable.
/// </summary>
public sealed class ExecutionGroup
{
    internal ExecutionGroup(ExecutionGroupConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        Entries = [.. configuration.Entries];
    }

    /// <summary>
    /// Gets the members of the group in definition order. Members with equal priority run in definition order,
    /// then in selection order.
    /// </summary>
    public ImmutableArray<ExecutionGroupEntry> Entries { get; }
}
