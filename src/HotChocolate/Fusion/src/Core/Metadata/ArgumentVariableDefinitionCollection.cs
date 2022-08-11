using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Fusion.Metadata;

internal sealed class ArgumentVariableDefinitionCollection : IEnumerable<ArgumentVariableDefinition>
{
    private readonly Dictionary<string, ArgumentVariableDefinition> _variableDefinitions;

    public ArgumentVariableDefinitionCollection(
        IEnumerable<ArgumentVariableDefinition> variableDefinitions)
    {
        _variableDefinitions = variableDefinitions.ToDictionary(t => t.Name);
    }

    public int Count => _variableDefinitions.Count;

    public ArgumentVariableDefinition this[string variableName]
        => _variableDefinitions[variableName];

    public bool TryGetValue(
        string variableName,
        [NotNullWhen(true)] out ArgumentVariableDefinition? value)
        => _variableDefinitions.TryGetValue(variableName, out value);

    public bool ContainsVariable(string variableName)
        => _variableDefinitions.ContainsKey(variableName);

    public IEnumerator<ArgumentVariableDefinition> GetEnumerator()
        => _variableDefinitions.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
