using System.Diagnostics.CodeAnalysis;
using HotChocolate.Types;
using HotChocolate.Types.Mutable;

namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// Interns context scopes, the maps from a source schema and a context name to the nearest value
/// above a position of a query path that sets the context, so that equal scopes share one
/// <see cref="ContextScopeRef"/>.
/// </summary>
internal sealed class ContextScopes
{
    private readonly MutableSchemaDefinition _schema;
    private readonly SchemaSetInterner _schemaSets;
    private readonly List<Entry[]> _scopes = [[]];
    private readonly Dictionary<Entry[], ContextScopeRef> _refs = new(EntriesComparer.Instance);
    private readonly Dictionary<MutableObjectTypeDefinition, (int SchemaIndex, string Name)[]> _settings = [];
    private readonly Dictionary<(ContextScopeRef, ContextValue), ContextScopeRef> _extensions = [];
    private Dictionary<MutableObjectTypeDefinition, List<MutableUnionTypeDefinition>>? _unionsByMember;

    /// <summary>
    /// Creates the scopes of the types of <paramref name="schema"/>, whose context directives name
    /// schemas of <paramref name="schemaSets"/>.
    /// </summary>
    public ContextScopes(MutableSchemaDefinition schema, SchemaSetInterner schemaSets)
    {
        _schema = schema;
        _schemaSets = schemaSets;
        _refs.Add(_scopes[0], ContextScopeRef.Empty);
    }

    /// <summary>
    /// Gets the scope of the values below a value of <paramref name="type"/> held in
    /// <paramref name="schemaName"/>, or <c>null</c> at an operation root, given the
    /// <paramref name="scope"/> of that value. Every context that the type sets replaces the entry of
    /// the same schema and name, and the value is never part of its own scope.
    /// </summary>
    public ContextScopeRef Extend(
        ContextScopeRef scope,
        MutableObjectTypeDefinition type,
        string? schemaName)
    {
        var settings = GetSettings(type);

        if (settings.Length == 0)
        {
            return scope;
        }

        var value = new ContextValue(type, schemaName);

        if (_extensions.TryGetValue((scope, value), out var extended))
        {
            return extended;
        }

        var entries = new List<Entry>(_scopes[scope.Id]);

        foreach (var (schemaIndex, name) in settings)
        {
            var index = entries.FindIndex(e => e.SchemaIndex == schemaIndex && e.Name == name);

            if (index >= 0)
            {
                entries[index] = new Entry(schemaIndex, name, value);
            }
            else
            {
                entries.Add(new Entry(schemaIndex, name, value));
            }
        }

        entries.Sort(EntryComparer.Instance);
        extended = Intern([.. entries]);
        _extensions.Add((scope, value), extended);

        return extended;
    }

    /// <summary>
    /// Determines whether <paramref name="scope"/> has an entry for the context
    /// <paramref name="name"/> of the schema <paramref name="schemaName"/>.
    /// </summary>
    public bool Contains(ContextScopeRef scope, string schemaName, string name)
        => TryGetValue(scope, schemaName, name, out _);

    /// <summary>
    /// Gets the value that <paramref name="scope"/> holds for the context <paramref name="name"/> of the
    /// schema <paramref name="schemaName"/>.
    /// </summary>
    public bool TryGetValue(
        ContextScopeRef scope,
        string schemaName,
        string name,
        [NotNullWhen(true)] out ContextValue? value)
    {
        var schemaIndex = _schemaSets.GetIndex(schemaName);

        foreach (var entry in _scopes[scope.Id])
        {
            if (entry.SchemaIndex == schemaIndex && entry.Name == name)
            {
                value = entry.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private ContextScopeRef Intern(Entry[] entries)
    {
        if (!_refs.TryGetValue(entries, out var scope))
        {
            scope = new ContextScopeRef(_scopes.Count);
            _scopes.Add(entries);
            _refs.Add(entries, scope);
        }

        return scope;
    }

    /// <summary>
    /// Gets the contexts that values of <paramref name="type"/> set: those of the type itself, of the
    /// interfaces it implements and of the unions it is a member of.
    /// </summary>
    private (int SchemaIndex, string Name)[] GetSettings(MutableObjectTypeDefinition type)
    {
        if (_settings.TryGetValue(type, out var settings))
        {
            return settings;
        }

        var contexts = new List<(int SchemaIndex, string Name)>();

        AddContexts(type, contexts);
        AddInterfaceContexts(type.Implements, contexts, []);

        foreach (var union in GetUnions(type))
        {
            AddContexts(union, contexts);
        }

        settings = [.. contexts.Distinct()];
        _settings.Add(type, settings);

        return settings;
    }

    private void AddInterfaceContexts(
        IEnumerable<MutableInterfaceTypeDefinition> interfaces,
        List<(int SchemaIndex, string Name)> contexts,
        HashSet<MutableInterfaceTypeDefinition> visited)
    {
        foreach (var interfaceType in interfaces)
        {
            if (visited.Add(interfaceType))
            {
                AddContexts(interfaceType, contexts);
                AddInterfaceContexts(interfaceType.Implements, contexts, visited);
            }
        }
    }

    private void AddContexts(IDirectivesProvider type, List<(int SchemaIndex, string Name)> contexts)
    {
        foreach (var directive in type.Directives.AsEnumerable())
        {
            if (directive.Name == WellKnownDirectiveNames.FusionContext)
            {
                contexts.Add(
                    (_schemaSets.GetIndex((string)directive.Arguments[WellKnownArgumentNames.Schema].Value!),
                    (string)directive.Arguments[WellKnownArgumentNames.Name].Value!));
            }
        }
    }

    private List<MutableUnionTypeDefinition> GetUnions(MutableObjectTypeDefinition type)
    {
        if (_unionsByMember is null)
        {
            _unionsByMember = [];

            foreach (var union in _schema.Types.OfType<MutableUnionTypeDefinition>())
            {
                if (!union.Directives.AsEnumerable().Any(d => d.Name == WellKnownDirectiveNames.FusionContext))
                {
                    continue;
                }

                foreach (var member in union.Types)
                {
                    if (!_unionsByMember.TryGetValue(member, out var unions))
                    {
                        unions = [];
                        _unionsByMember.Add(member, unions);
                    }

                    unions.Add(union);
                }
            }
        }

        return _unionsByMember.TryGetValue(type, out var result) ? result : [];
    }

    private readonly record struct Entry(int SchemaIndex, string Name, ContextValue Value);

    private sealed class EntryComparer : IComparer<Entry>
    {
        public static readonly EntryComparer Instance = new();

        public int Compare(Entry x, Entry y)
        {
            var result = x.SchemaIndex.CompareTo(y.SchemaIndex);

            return result != 0 ? result : string.CompareOrdinal(x.Name, y.Name);
        }
    }

    private sealed class EntriesComparer : IEqualityComparer<Entry[]>
    {
        public static readonly EntriesComparer Instance = new();

        public bool Equals(Entry[]? x, Entry[]? y) => x.AsSpan().SequenceEqual(y.AsSpan());

        public int GetHashCode(Entry[] obj)
        {
            var hash = new HashCode();

            foreach (var entry in obj)
            {
                hash.Add(entry);
            }

            return hash.ToHashCode();
        }
    }
}
