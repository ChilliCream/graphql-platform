using System.Buffers;
using System.Text;

namespace CookieCrumble;

public static class WriterExtensions
{
    private static readonly Encoding s_utf8 = Encoding.UTF8;

    public static void Append(this IBufferWriter<byte> snapshot, string value)
        => Append(snapshot, value.AsSpan());

    public static void Append(this IBufferWriter<byte> snapshot, ReadOnlySpan<char> value)
    {
        s_utf8.GetBytes(value, snapshot);
    }

    public static void AppendLine(this IBufferWriter<byte> snapshot)
    {
        snapshot.GetSpan(1)[0] = (byte)'\n';
        snapshot.Advance(1);
    }

    public static void AppendLine(this IBufferWriter<byte> snapshot, bool appendWhenTrue)
    {
        if (!appendWhenTrue)
        {
            return;
        }

        snapshot.GetSpan(1)[0] = (byte)'\n';
        snapshot.Advance(1);
    }

    public static void AppendSeparator(this IBufferWriter<byte> snapshot)
    {
        const byte hyphen = (byte)'-';
        snapshot.GetSpan(15).Fill(hyphen);
        snapshot.Advance(15);
    }
}
