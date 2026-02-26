using System.Collections.Immutable;
using HotChocolate.Buffers;

namespace HotChocolate.Fusion.Execution;

public sealed class VariableValueSets : IDisposable
{
    public static readonly VariableValueSets Empty = new([], null);

    private readonly PooledArrayWriter? _buffer;

    internal VariableValueSets(
        ImmutableArray<VariableValues> values,
        PooledArrayWriter? buffer)
    {
        Values = values;
        _buffer = buffer;
    }

    public ImmutableArray<VariableValues> Values { get; }

    public void Dispose()
    {
        _buffer?.Dispose();
    }
}
