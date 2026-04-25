using System.Diagnostics.CodeAnalysis;

namespace Mocha.Resources;

/// <summary>
/// Default <see cref="IMochaResourceDefinitionCatalog"/> implementation backed by a fixed
/// list of definitions.
/// </summary>
public sealed class MochaResourceDefinitionCatalog : IMochaResourceDefinitionCatalog
{
    private readonly Dictionary<string, MochaResourceDefinition> _byKind;
    private readonly IReadOnlyList<MochaResourceDefinition> _definitions;

    /// <summary>
    /// Initializes a new <see cref="MochaResourceDefinitionCatalog"/> from the supplied
    /// <paramref name="definitions"/>.
    /// </summary>
    /// <param name="definitions">The definitions to register; duplicates by kind are ignored.</param>
    public MochaResourceDefinitionCatalog(IEnumerable<MochaResourceDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        _byKind = new Dictionary<string, MochaResourceDefinition>(StringComparer.Ordinal);
        var list = new List<MochaResourceDefinition>();
        foreach (var definition in definitions)
        {
            if (_byKind.TryAdd(definition.Kind, definition))
            {
                list.Add(definition);
            }
        }

        _definitions = list;
    }

    /// <inheritdoc />
    public IReadOnlyList<MochaResourceDefinition> Definitions => _definitions;

    /// <inheritdoc />
    public bool TryGet(string kind, [NotNullWhen(true)] out MochaResourceDefinition? definition)
    {
        ArgumentNullException.ThrowIfNull(kind);

        if (_byKind.TryGetValue(kind, out var found))
        {
            definition = found;
            return true;
        }

        definition = null;
        return false;
    }
}
