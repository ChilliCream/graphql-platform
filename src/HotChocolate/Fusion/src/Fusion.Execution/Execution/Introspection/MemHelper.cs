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
        const string format = "R";
        var doubleValue = (double)value;

        if (!doubleValue.TryFormat(buffer, out var written, format, CultureInfo.InvariantCulture))
        {
            throw new InvalidOperationException($"Failed to format float value '{value}'.");
        }

        context.FieldResult.SetNumberValue(buffer[..written]);
    }
}
