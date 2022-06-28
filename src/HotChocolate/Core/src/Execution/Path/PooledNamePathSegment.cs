using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HotChocolate.Execution;

#if NET6_0_OR_GREATER
internal sealed class PooledNamePathSegment : NamePathSegment
{
    private readonly int _index;
    private readonly NameString[] _names;
    private readonly Path?[] _parents;

    public PooledNamePathSegment(int index, NameString[] names, Path?[] parents)
    {
        _index = index;
        _names = names;
        _parents = parents;
        if (names.Length <= _index || parents.Length <= index)
        {
            throw new InvalidOperationException("Name Path pool is in illegal state");
        }
    }

    public override NameString Name
    {
        get
        {
            ref NameString searchSpace = ref MemoryMarshal.GetArrayDataReference(_names);
            return Unsafe.Add(ref searchSpace, _index);
        }
        internal set
        {
            ref NameString searchSpace = ref MemoryMarshal.GetArrayDataReference(_names);
            Unsafe.Add(ref searchSpace, _index) = value;
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
