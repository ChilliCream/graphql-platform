using System.Buffers;

namespace CookieCrumble.Formatters;

public class ExceptionSnapshotValueFormatter : SnapshotValueFormatter<Exception>
{
    protected override void Format(IBufferWriter<byte> snapshot, Exception value)
    {
        snapshot.Append(value.GetType().FullName ?? value.GetType().Name);
        snapshot.AppendLine();
        snapshot.Append(value.Message);
        snapshot.AppendLine();
    }
}
