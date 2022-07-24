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
    {
        foreach (var variable in _variables)
        {
            yield return variable;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
