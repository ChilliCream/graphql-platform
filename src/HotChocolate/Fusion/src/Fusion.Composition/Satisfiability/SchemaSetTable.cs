namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// Assigns dense indexes to source schema names and interns schema sets, so that equal sets
/// share one <see cref="SchemaSet"/>.
/// </summary>
internal sealed class SchemaSetTable
{
    private readonly Dictionary<string, int> _indexes = new(StringComparer.Ordinal);
    private readonly List<string> _names = [];
    private readonly List<ulong[]> _members = [[]];
    private readonly Dictionary<ulong[], SchemaSet> _sets = new(MembersComparer.Instance);
    private ulong[] _buffer = [];

    public SchemaSetTable()
    {
        _sets.Add(_members[0], SchemaSet.Empty);
    }

    /// <summary>
    /// Gets the index of <paramref name="schemaName"/>, assigning the next free index on first use.
    /// </summary>
    public int GetIndex(string schemaName)
    {
        if (!_indexes.TryGetValue(schemaName, out var index))
        {
            index = _names.Count;
            _indexes.Add(schemaName, index);
            _names.Add(schemaName);
        }

        return index;
    }

    /// <summary>
    /// Gets the schema name with the given index.
    /// </summary>
    public string GetSchemaName(int index) => _names[index];

    /// <summary>
    /// Creates the set that contains the schemas with the given <paramref name="indexes"/>.
    /// </summary>
    public SchemaSet Create(List<int> indexes)
    {
        var buffer = GetBuffer(SchemaSet.Empty);

        foreach (var index in indexes)
        {
            Add(buffer, index);
        }

        return Intern(buffer);
    }

    /// <summary>
    /// Gets the members of <paramref name="set"/> as bits indexed by schema index.
    /// </summary>
    public ReadOnlySpan<ulong> GetMembers(SchemaSet set) => _members[set.Id];

    /// <summary>
    /// Determines whether <paramref name="set"/> contains <paramref name="schemaName"/>.
    /// </summary>
    public bool Contains(SchemaSet set, string schemaName)
        => _indexes.TryGetValue(schemaName, out var index) && Contains(_members[set.Id], index);

    /// <summary>
    /// Determines whether <paramref name="set"/> contains the schema with the given index.
    /// </summary>
    public bool Contains(SchemaSet set, int index) => Contains(_members[set.Id], index);

    /// <summary>
    /// Gets the shared scratch buffer, sized for every known schema and holding the members of
    /// <paramref name="set"/>. The next call to <see cref="GetBuffer"/> or <see cref="Create"/>
    /// overwrites the buffer.
    /// </summary>
    public ulong[] GetBuffer(SchemaSet set)
    {
        var length = (_names.Count + 63) >> 6;

        if (_buffer.Length < length)
        {
            _buffer = new ulong[length];
        }

        var members = _members[set.Id];
        members.CopyTo(_buffer, 0);
        Array.Clear(_buffer, members.Length, _buffer.Length - members.Length);
        return _buffer;
    }

    /// <summary>
    /// Removes every schema from <paramref name="buffer"/> that <paramref name="set"/> does not contain.
    /// </summary>
    public void IntersectWith(ulong[] buffer, SchemaSet set)
    {
        var members = _members[set.Id];

        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] &= i < members.Length ? members[i] : 0UL;
        }
    }

    /// <summary>
    /// Adds every schema of <paramref name="set"/> to <paramref name="buffer"/>.
    /// </summary>
    public void UnionWith(ulong[] buffer, SchemaSet set)
    {
        var members = _members[set.Id];

        for (var i = 0; i < members.Length; i++)
        {
            buffer[i] |= members[i];
        }
    }

    /// <summary>
    /// Gets the set with the members of <paramref name="buffer"/>.
    /// </summary>
    public SchemaSet Intern(ulong[] buffer)
    {
        if (!_sets.TryGetValue(buffer, out var set))
        {
            var members = MembersComparer.Trim(buffer).ToArray();
            set = new SchemaSet(_members.Count);
            _members.Add(members);
            _sets.Add(members, set);
        }

        return set;
    }

    /// <summary>
    /// Determines whether <paramref name="buffer"/> contains the schema with the given index.
    /// </summary>
    public static bool Contains(ulong[] buffer, int index)
    {
        var word = index >> 6;
        return word < buffer.Length && (buffer[word] & (1UL << index)) != 0;
    }

    /// <summary>
    /// Adds the schema with the given index to <paramref name="buffer"/>.
    /// </summary>
    public static void Add(ulong[] buffer, int index)
        => buffer[index >> 6] |= 1UL << index;

    /// <summary>
    /// Removes the schema with the given index from <paramref name="buffer"/>.
    /// </summary>
    public static void Remove(ulong[] buffer, int index)
        => buffer[index >> 6] &= ~(1UL << index);

    private sealed class MembersComparer : IEqualityComparer<ulong[]>
    {
        public static readonly MembersComparer Instance = new();

        public bool Equals(ulong[]? x, ulong[]? y) => Trim(x).SequenceEqual(Trim(y));

        public int GetHashCode(ulong[] obj)
        {
            var hash = new HashCode();

            foreach (var word in Trim(obj))
            {
                hash.Add(word);
            }

            return hash.ToHashCode();
        }

        public static ReadOnlySpan<ulong> Trim(ulong[]? words)
        {
            ReadOnlySpan<ulong> span = words;

            while (!span.IsEmpty && span[^1] == 0)
            {
                span = span[..^1];
            }

            return span;
        }
    }
}
