using System.Numerics;
using HotChocolate.Fusion.Options;

namespace HotChocolate.Fusion.Satisfiability;

/// <summary>
/// Assigns dense indexes to source schema names and interns schema sets, so that equal sets
/// share one <see cref="SchemaSet"/>.
/// </summary>
internal sealed class SchemaSetTable
{
    private readonly Dictionary<string, int> _indexes = new(StringComparer.Ordinal);
    private readonly string[] _names;
    private readonly List<ulong[]> _members = [[]];
    private readonly Dictionary<ulong[], SchemaSet> _sets = new(MembersComparer.Instance);
    private readonly ulong[] _buffer;
    private readonly Stack<ulong[]> _bufferPool = new();

    /// <summary>
    /// Creates a table that assigns each of the distinct <paramref name="schemaNames"/> its
    /// position as index.
    /// </summary>
    public SchemaSetTable(IEnumerable<string> schemaNames)
    {
        _names = [.. schemaNames];

        for (var i = 0; i < _names.Length; i++)
        {
            _indexes.Add(_names[i], i);
        }

        _buffer = new ulong[(_names.Length + 63) >> 6];
        _sets.Add(_members[0], SchemaSet.Empty);
    }

    /// <summary>
    /// Gets the index of <paramref name="schemaName"/>, which must be one of the schemas of the table.
    /// </summary>
    public int GetIndex(string schemaName)
    {
        if (!_indexes.TryGetValue(schemaName, out var index))
        {
            throw ThrowHelper.UnknownSourceSchema(schemaName);
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
    /// Gets the shared scratch buffer, sized for every schema of the table and holding the members
    /// of <paramref name="set"/>. The next call to <see cref="GetBuffer"/> or <see cref="Create"/>
    /// overwrites the buffer.
    /// </summary>
    public ulong[] GetBuffer(SchemaSet set)
    {
        var members = _members[set.Id];
        members.CopyTo(_buffer, 0);
        Array.Clear(_buffer, members.Length, _buffer.Length - members.Length);
        return _buffer;
    }

    /// <summary>
    /// Rents a buffer that is not shared until it is returned with <see cref="ReturnBuffer"/>, sized for
    /// every schema of the table and holding the members of <paramref name="set"/>.
    /// </summary>
    public ulong[] RentBuffer(SchemaSet set)
    {
        if (!_bufferPool.TryPop(out var buffer))
        {
            buffer = new ulong[_buffer.Length];
        }

        var members = _members[set.Id];
        members.CopyTo(buffer, 0);
        Array.Clear(buffer, members.Length, buffer.Length - members.Length);
        return buffer;
    }

    /// <summary>
    /// Returns a buffer of <see cref="RentBuffer"/>, which the caller must not use afterwards.
    /// </summary>
    public void ReturnBuffer(ulong[] buffer) => _bufferPool.Push(buffer);

    /// <summary>
    /// Gets the number of schemas in <paramref name="set"/>.
    /// </summary>
    public int Count(SchemaSet set)
    {
        var count = 0;

        foreach (var word in _members[set.Id])
        {
            count += BitOperations.PopCount(word);
        }

        return count;
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
