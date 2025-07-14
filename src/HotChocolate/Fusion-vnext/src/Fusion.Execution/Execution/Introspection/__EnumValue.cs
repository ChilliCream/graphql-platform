using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Types;

#pragma warning disable IDE1006 // Naming Styles
namespace HotChocolate.Fusion.Execution.Introspection;

// ReSharper disable once InconsistentNaming
internal sealed class __EnumValue
{
    public static void Name(FieldContext context)
        => context.WriteValue(context.Parent<IEnumValue>().Name);

    public static void Description(FieldContext context)
        => context.WriteValue(context.Parent<IEnumValue>().Description);

    public static void IsDeprecated(FieldContext context)
        => context.WriteValue(context.Parent<IEnumValue>().IsDeprecated);

    public static void DeprecationReason(FieldContext context)
        => context.WriteValue(context.Parent<IEnumValue>().DeprecationReason);
}
#pragma warning restore IDE1006 // Naming Styles
