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
}
