using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HotChocolate.Execution;

#if NET6_0_OR_GREATER
internal sealed class PooledIndexerPathSegment : IndexerPathSegment
{
    private readonly int _index;
    private readonly Path?[] _parents;

    public PooledIndexerPathSegment(int index, Path?[] parents)
    {
        _index = index;
        _parents = parents;
        if (parents.Length <= index)
        {
            throw new InvalidOperationException("Name Path pool is in illegal state");
        }
    }

    public override Path Parent
    {
        get
        {
            ref Path? searchSpace = ref MemoryMarshal.GetArrayDataReference(_parents);
            return Unsafe.Add(ref searchSpace, _index) ?? Root;
        }
        internal set
        {
            ref Path? searchSpace = ref MemoryMarshal.GetArrayDataReference(_parents);
            Unsafe.Add(ref searchSpace, _index) = value;
        }
    }
}
#endif
