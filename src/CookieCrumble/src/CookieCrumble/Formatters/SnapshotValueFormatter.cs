using System.Buffers;

namespace CookieCrumble.Formatters;

/// <summary>
/// Formats a snapshot segment value for the snapshot file.
/// </summary>
public abstract class SnapshotValueFormatter<TValue> : ISnapshotValueFormatter
{
    public bool CanHandle(object? value)
        => value is TValue casted && CanHandle(casted);

    protected virtual bool CanHandle(TValue? value)
        => true;

    public void Format(IBufferWriter<byte> snapshot, object? value)
        => Format(snapshot, (TValue)value!);

    protected abstract void Format(IBufferWriter<byte> snapshot, TValue value);
}
