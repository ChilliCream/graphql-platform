using System.Collections;

namespace HotChocolate.Fusion.Metadata;

internal sealed class FieldVariableDefinitionCollection : IEnumerable<IVariableDefinition>
{
    private readonly IReadOnlyList<IVariableDefinition> _variables;

    public FieldVariableDefinitionCollection(
        IReadOnlyList<IVariableDefinition> variables)
    {
        _variables = variables;
    }

    public int Count => _variables.Count;

    public IEnumerator<IVariableDefinition> GetEnumerator()
        => _variables.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public static FieldVariableDefinitionCollection Empty { get; } =
        new(new List<IVariableDefinition>());
}
