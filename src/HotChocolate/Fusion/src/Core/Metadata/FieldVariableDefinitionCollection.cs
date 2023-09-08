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

    public IEnumerable<IVariableDefinition> GetByName(string name)
        => _variables.Where(t => t.Name.Equals(name));

    public bool IsOfType<T>(string name)
        where T : IVariableDefinition
        => _variables.FirstOrDefault(t => t.Name.Equals(name)) is T;

    public int Count => _variables.Count;

    public IEnumerator<IVariableDefinition> GetEnumerator()
        => _variables.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public static FieldVariableDefinitionCollection Empty { get; } =
        new(new List<IVariableDefinition>());
}
