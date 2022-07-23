using System.Collections;

namespace HotChocolate.Fusion.Metadata;

internal sealed class VariableDefinitionCollection : IEnumerable<FieldVariableDefinition>
{
    private readonly FieldVariableDefinition[] _variables;

    public VariableDefinitionCollection(IEnumerable<FieldVariableDefinition> variables)
    {
        _variables = variables.ToArray();
    }

    public int Count => _variables.Length;

    public IEnumerator<FieldVariableDefinition> GetEnumerator()
        => Enumerable.Empty<FieldVariableDefinition>().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
