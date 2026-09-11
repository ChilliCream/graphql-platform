using System.Numerics;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// An order-independent set of object types, represented as a bitset over the
/// snapshot's dense object-type index, with a fingerprint computed from the
/// final bit content rather than from insertion order.
/// </summary>
internal readonly struct PossibleTypeSet : IEquatable<PossibleTypeSet>
{
    private readonly ulong[] _words;

    private PossibleTypeSet(ulong[] words, int count, ulong fingerprint)
    {
        _words = words;
        Count = count;
        Fingerprint = fingerprint;
    }

    /// <summary>
    /// Gets the number of object types in this set.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Gets a fingerprint over this set's content. Two sets with the same
    /// members over the same dense index have the same fingerprint,
    /// regardless of the order their members were supplied in.
    /// </summary>
    public ulong Fingerprint { get; }

    /// <summary>
    /// Determines whether the object type at <paramref name="objectTypeIndex"/>
    /// (the snapshot's own dense index) belongs to this set.
    /// </summary>
    public bool Contains(int objectTypeIndex)
    {
        var wordIndex = objectTypeIndex >> 6;
        return wordIndex < _words.Length
            && (_words[wordIndex] & (1UL << (objectTypeIndex & 63))) != 0;
    }

    /// <summary>
    /// Returns the set of object types that belong to both this set and
    /// <paramref name="other"/>.
    /// </summary>
    internal PossibleTypeSet Intersect(PossibleTypeSet other)
    {
        var wordCount = Math.Min(_words.Length, other._words.Length);
        var words = wordCount == 0 ? [] : new ulong[wordCount];
        var count = 0;

        for (var i = 0; i < wordCount; i++)
        {
            words[i] = _words[i] & other._words[i];
            count += BitOperations.PopCount(words[i]);
        }

        return new PossibleTypeSet(words, count, ComputeFingerprint(words));
    }

    /// <summary>
    /// Returns an enumerator over the object-type indices this set contains,
    /// in ascending order.
    /// </summary>
    public Enumerator GetEnumerator() => new(_words);

    /// <inheritdoc />
    public bool Equals(PossibleTypeSet other)
        => Fingerprint == other.Fingerprint && _words.AsSpan().SequenceEqual(other._words);

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is PossibleTypeSet other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode()
        => Fingerprint.GetHashCode();

    /// <summary>
    /// Builds a <see cref="PossibleTypeSet"/> over an object-type dense index
    /// of size <paramref name="objectTypeCount"/> from the given member
    /// indices. The result does not depend on the order of
    /// <paramref name="memberIndices"/>.
    /// </summary>
    internal static PossibleTypeSet Create(int objectTypeCount, ReadOnlySpan<int> memberIndices)
    {
        var wordCount = (objectTypeCount + 63) >> 6;
        var words = wordCount == 0 ? [] : new ulong[wordCount];

        foreach (var index in memberIndices)
        {
            words[index >> 6] |= 1UL << (index & 63);
        }

        var count = 0;

        foreach (var word in words)
        {
            count += BitOperations.PopCount(word);
        }

        return new PossibleTypeSet(words, count, ComputeFingerprint(words));
    }

    private static ulong ComputeFingerprint(ReadOnlySpan<ulong> words)
    {
        // FNV-1a over the final word content: a pure function of set
        // membership, independent of the order bits were set in.
        const ulong offsetBasis = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        var hash = offsetBasis;

        foreach (var word in words)
        {
            hash = (hash ^ word) * prime;
        }

        return hash;
    }

    /// <summary>
    /// A non-allocating, forward-only enumerator over the object-type
    /// indices a <see cref="PossibleTypeSet"/> contains.
    /// </summary>
    public struct Enumerator
    {
        private readonly ulong[] _words;
        private int _wordIndex;
        private ulong _remainingBits;

        internal Enumerator(ulong[] words)
        {
            _words = words;
            _wordIndex = -1;
            _remainingBits = 0;
        }

        /// <summary>
        /// Gets the object-type index the enumerator currently points at.
        /// </summary>
        public int Current { get; private set; }

        /// <summary>
        /// Advances to the next set object-type index.
        /// </summary>
        public bool MoveNext()
        {
            while (_remainingBits == 0)
            {
                _wordIndex++;

                if (_wordIndex >= _words.Length)
                {
                    return false;
                }

                _remainingBits = _words[_wordIndex];
            }

            Current = (_wordIndex << 6) + BitOperations.TrailingZeroCount(_remainingBits);
            _remainingBits &= _remainingBits - 1;
            return true;
        }
    }
}
