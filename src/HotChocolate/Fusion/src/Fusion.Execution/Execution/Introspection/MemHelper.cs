using System.Buffers;
using System.Globalization;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution.Introspection;

internal static class MemHelper
{
    public static void WriteValue(this FieldContext context, Uri? uri)
        => WriteValue(context, uri?.ToString());

    public static void WriteValue(this FieldContext context, string? s)
    {
        if (s is null)
        {
            return;
        }

        context.FieldResult.SetStringValue(s);
    }

    public static void WriteValue(this FieldContext context, bool b)
        => context.FieldResult.SetBooleanValue(b);

    public static void WriteValue(this FieldContext context, ReadOnlySpan<byte> value)
        => context.FieldResult.SetStringValue(value);

    public static void WriteFloatValue(this FieldContext context, float value)
    {
        Span<byte> buffer = stackalloc byte[32];
        if (value.TryFormat(buffer, out var written, default, CultureInfo.InvariantCulture))
        {
            context.FieldResult.SetNumberValue(buffer[..written]);
        }
    }
}
