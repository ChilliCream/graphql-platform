using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace HotChocolate.Execution.Processing;

internal sealed class SelectionLookup
{
    private readonly Entry[] _table;
    private readonly int _seed;
    private readonly int _mask;

    private SelectionLookup(Entry[] table, int seed, int mask)
    {
        _table = table;
        _seed = seed;
        _mask = mask;
    }

    public static SelectionLookup Create(SelectionSet selectionSet)
    {
        var selections = selectionSet.Selections;
        var tableSize = NextPowerOfTwo(Math.Max(selections.Length * 2, 4));
        var mask = tableSize - 1;
        var table = new Entry[tableSize];

        // We try multiple seeds to find one with minimal clustering
        var bestSeed = 0;
        var bestMaxProbeLength = int.MaxValue;

        for (var seed = 0; seed < 100; seed++)
        {
            Array.Clear(table);
            var maxProbeLength = 0;

            foreach (var selection in selections)
            {
                var hashCode = ComputeHash(selection.Utf8ResponseName, seed);
                var index = hashCode & mask;
                var probeLength = 0;

                // Linear probe to find empty slot
                while (table[index].Selection != null)
                {
                    index = (index + 1) & mask;
                    probeLength++;
                }

                table[index] = new Entry(hashCode, selection);
                maxProbeLength = Math.Max(maxProbeLength, probeLength);
            }

            // Track best seed
            if (maxProbeLength < bestMaxProbeLength)
            {
                bestMaxProbeLength = maxProbeLength;
                bestSeed = seed;

                // If we found excellent distribution, stop early
                if (maxProbeLength <= 2)
                {
                    break;
                }
            }
        }

        // Rebuild table with best seed
        Array.Clear(table);
        foreach (var selection in selections)
        {
            var hashCode = ComputeHash(selection.Utf8ResponseName, bestSeed);
            var index = hashCode & mask;

            while (table[index].Selection != null)
            {
                index = (index + 1) & mask;
            }

            table[index] = new Entry(hashCode, selection);
        }

        return new SelectionLookup(table, bestSeed, mask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryGetSelection(ReadOnlySpan<byte> name, [NotNullWhen(true)] out Selection? selection)
    {
        var table = _table.AsSpan();

        if (table.Length == 0)
        {
            selection = null!;
            return false;
        }

        var hashCode = ComputeHash(name, _seed);
        var index = hashCode & _mask;

        while (true)
        {
            ref var entry = ref table[index];

            // if we hit an empty slot, then there is no selection with the specified name.
            if (entry.Selection is null)
            {
                selection = null;
                return false;
            }

            if (entry.HashCode == hashCode && name.SequenceEqual(entry.Selection.Utf8ResponseName))
            {
                selection = entry.Selection;
                return true;
            }

            // we had a hash collision need to find the next slot.
            index = (index + 1) & _mask;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int ComputeHash(ReadOnlySpan<byte> bytes, int seed)
    {
        var hash = (uint)seed;

        foreach (var b in bytes)
        {
            hash = hash * 31 + b;
        }

        return (int)(hash & 0x7FFFFFFF);
    }

    private static int NextPowerOfTwo(int n)
    {
        if (n <= 0)
        {
            return 1;
        }

        n--;
        n |= n >> 1;
        n |= n >> 2;
        n |= n >> 4;
        n |= n >> 8;
        n |= n >> 16;
        n++;

        return n;
    }

    private readonly struct Entry(int hashCode, Selection selection)
    {
        public readonly int HashCode = hashCode;
        public readonly Selection? Selection = selection;
    }
}
