using System.Text;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Execution.Results;

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

        context.FieldResult.SetStringValue(s);
    }

    public static void WriteValue(this FieldContext context, bool b)
        => context.FieldResult.SetBooleanValue(b);

    public static void WriteValue(this FieldContext context, ReadOnlySpan<byte> value)
        => context.FieldResult.SetStringValue(value);
}
