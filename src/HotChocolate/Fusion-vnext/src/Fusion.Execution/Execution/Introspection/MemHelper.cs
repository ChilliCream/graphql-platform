using System.Text;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution.Introspection;

internal static class MemHelper
{
    private static readonly Encoding s_utf8 = Encoding.UTF8;

    public static void WriteValue(this FieldContext context, Uri? uri)
        => WriteValue(context, uri?.ToString());

    public static void WriteValue(this FieldContext context, string? s)
    {
        if (s is null)
        {
            return;
        }

        var start = context.Memory.Length;
        var expectedSize = s_utf8.GetByteCount(s);
        var span = context.Memory.GetSpan(expectedSize + 1);
        span[0] = RawFieldValueType.String;
        var written = s_utf8.GetBytes(s, span[1..]);
        context.Memory.Advance(written + 1);
        var segment = context.Memory.GetWrittenMemorySegment(start, written + 1);
        context.FieldResult.SetNextValue(segment);
    }

    public static void WriteValue(this FieldContext context, bool b)
    {
        var start = context.Memory.Length;
        const int length = 2;
        var span = context.Memory.GetSpan(length);
        span[0] = RawFieldValueType.Boolean;
        span[1] = b ? (byte)1 : (byte)0;
        context.Memory.Advance(length);
        var segment = context.Memory.GetWrittenMemorySegment(start, length);
        context.FieldResult.SetNextValue(segment);
    }

    public static void WriteValue(this FieldContext context, ReadOnlySpan<byte> value)
    {
        if (value.Length == 0)
        {
            return;
        }

        var start = context.Memory.Length;
        var length = value.Length + 1;
        var span = context.Memory.GetSpan(length);
        span[0] = RawFieldValueType.String;
        value.CopyTo(span[1..]);
        context.Memory.Advance(length);
        var segment = context.Memory.GetWrittenMemorySegment(start, length);
        context.FieldResult.SetNextValue(segment);
    }
}
