using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Fusion.Metadata;

internal sealed class FetchDefinitionCollection : IEnumerable<FetchDefinition>
{
    private readonly Dictionary<string, FetchDefinition[]> _fetchDefinitions;

    public FetchDefinitionCollection(IEnumerable<FetchDefinition> fetchDefinitions)
    {
        _fetchDefinitions = fetchDefinitions
            .GroupBy(t => t.SchemaName)
            .ToDictionary(t => t.Key, t => t.ToArray(), StringComparer.Ordinal);
    }

    public int Count => _fetchDefinitions.Count;

    // public IReadOnlyList<FetchDefinition> this[string schemaName] => throw new NotImplementedException();

    public bool TryGetValue(
        string schemaName,
        [NotNullWhen(true)] out IReadOnlyList<FetchDefinition>? values)
    {
        if (_fetchDefinitions.TryGetValue(schemaName, out var temp))
        {
            values = temp;
            return true;
        }

        values = null;
        return false;
    }

    public bool ContainsResolvers(string schemaName) => _fetchDefinitions.ContainsKey(schemaName);

    public IEnumerator<FetchDefinition> GetEnumerator()
        => _fetchDefinitions.Values.SelectMany(t => t).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static FetchDefinitionCollection Empty { get; } =
        new(new List<FetchDefinition>());
}
