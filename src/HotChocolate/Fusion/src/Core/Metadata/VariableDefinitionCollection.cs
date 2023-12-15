using System.Collections;

namespace HotChocolate.Fusion.Metadata;

/// <summary>
/// Represents a collection of variable definitions for the purpose of query planning.
/// </summary>
/// <param name="variables">
/// The collection of variable definitions for the object type.
/// </param>
internal sealed class VariableDefinitionCollection(
    IReadOnlyList<FieldVariableDefinition> variables)
    : IEnumerable<FieldVariableDefinition>
{
    private readonly IReadOnlyList<FieldVariableDefinition> _variables = variables
        ?? throw new ArgumentNullException(nameof(variables));

    /// <summary>
    /// Gets the number of variable definitions in the collection.
    /// </summary>
    public int Count => _variables.Count;

    public IEnumerator<FieldVariableDefinition> GetEnumerator()
        => _variables.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public static VariableDefinitionCollection Empty { get; } =
        new(new List<FieldVariableDefinition>());
}
