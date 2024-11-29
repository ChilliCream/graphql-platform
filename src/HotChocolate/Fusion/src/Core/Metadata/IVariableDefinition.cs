namespace HotChocolate.Fusion.Metadata;

/// <summary>
/// Represents a variable definition.
/// </summary>
internal interface IVariableDefinition
{
    /// <summary>
    /// Gets the name of the variable.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the name of the subgraph the variable is defined for.
    /// </summary>
    string SubgraphName { get; }
}
