using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace HotChocolate.Fusion.Metadata;

internal sealed class ResolverDefinitionCollection : IEnumerable<ResolverDefinition>
{
    private readonly Dictionary<string, ResolverDefinition[]> _fetchDefinitions;

    public ResolverDefinitionCollection(IEnumerable<ResolverDefinition> fetchDefinitions)
    {
        _fetchDefinitions = fetchDefinitions
            .GroupBy(t => t.SubgraphName)
            .ToDictionary(t => t.Key, t => t.ToArray(), StringComparer.Ordinal);
    }

    public int Count => _fetchDefinitions.Count;

    public bool TryGetValue(
        string subgraphName,
        [NotNullWhen(true)] out IReadOnlyList<ResolverDefinition>? values)
    {
        if (_fetchDefinitions.TryGetValue(subgraphName, out var temp))
        {
            values = temp;
            return true;
        }

        values = null;
        return false;
    }

    public bool ContainsResolvers(string schemaName) => _fetchDefinitions.ContainsKey(schemaName);

    public IEnumerator<ResolverDefinition> GetEnumerator()
        => _fetchDefinitions.Values.SelectMany(t => t).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static ResolverDefinitionCollection Empty { get; } =
        new(new List<ResolverDefinition>());
}
