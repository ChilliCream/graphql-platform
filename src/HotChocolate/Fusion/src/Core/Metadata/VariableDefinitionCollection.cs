using System.Collections;

namespace HotChocolate.Fusion.Metadata;

internal sealed class VariableDefinitionCollection : IEnumerable<VariableDefinition>
{
    private readonly IReadOnlyList<VariableDefinition> _variables;

    public VariableDefinitionCollection(
        IReadOnlyList<VariableDefinition> variables)
    {
        _variables = variables;
    }

    public int Count => _variables.Count;

    public IEnumerator<VariableDefinition> GetEnumerator()
        => _variables.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public static VariableDefinitionCollection Empty { get; } =
        new(new List<VariableDefinition>());
}
