using System.Collections;

namespace HotChocolate.Fusion.Metadata;

internal sealed class VariableDefinitionCollection : IEnumerable<FieldVariableDefinition>
{
    private readonly IReadOnlyList<FieldVariableDefinition> _variables;

    public VariableDefinitionCollection(
        IReadOnlyList<FieldVariableDefinition> variables)
    {
        _variables = variables;
    }

    public int Count => _variables.Count;

    public IEnumerator<FieldVariableDefinition> GetEnumerator()
        => _variables.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public static VariableDefinitionCollection Empty { get; } =
        new(new List<FieldVariableDefinition>());
}
