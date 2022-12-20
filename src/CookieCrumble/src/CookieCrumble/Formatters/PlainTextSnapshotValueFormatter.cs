using System.Buffers;

namespace CookieCrumble.Formatters;

internal sealed class PlainTextSnapshotValueFormatter : ISnapshotValueFormatter
{
    public bool CanHandle(object? value)
        => value is string;

    public void Format(IBufferWriter<byte> snapshot, object? value)
    {
        if (value?.ToString() is { } s)
        {
            snapshot.Append(s);
        }
    }
}
