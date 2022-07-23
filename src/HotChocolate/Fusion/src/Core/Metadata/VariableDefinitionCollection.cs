using System.Collections;

namespace HotChocolate.Fusion.Metadata;

public class VariableDefinitionCollection : IEnumerable<FieldVariableDefinition>
{


    public int Count { get; }

    public IReadOnlyList<IVariableDefinition> this[string variableName]
        => throw new NotImplementedException();

    public bool TryGetValue(string variableName, out IReadOnlyList<IVariableDefinition> value)
    {
        throw new NotImplementedException();
    }

    public bool ContainsVariable(string variableName) => throw new NotImplementedException();

    public IEnumerator<FieldVariableDefinition> GetEnumerator()
        => Enumerable.Empty<FieldVariableDefinition>().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
