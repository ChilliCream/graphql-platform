#pragma warning disable IDE1006 // Naming Styles
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language.Utilities;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Introspection;

// ReSharper disable once InconsistentNaming
internal sealed class __InputValue
{
    public static void Name(FieldContext context)
        => context.WriteValue(context.Parent<IInputValueDefinition>().Name);

    public static void Description(FieldContext context)
    {
        if (context.Parent<IInputValueDefinition>() is { Description: not null } fieldDef)
        {
            context.WriteValue(fieldDef.Description);
        }
    }

    public static void Type(FieldContext context)
    {
        var field = context.Parent<IInputValueDefinition>();
        context.AddRuntimeResult(field.Type);
        context.FieldResult.SetNextValue(context.ResultPool.RentObjectResult());
    }

    public static void IsDeprecated(FieldContext context)
    {
        var field = context.Parent<IInputValueDefinition>();
        context.WriteValue(field.IsDeprecated);
    }

    public static void DeprecationReason(FieldContext context)
    {
        var field = context.Parent<IInputValueDefinition>();
        if (field.DeprecationReason is not null)
        {
            context.WriteValue(field.DeprecationReason);
        }
    }

    public static void DefaultValue(FieldContext context)
    {
        var field = context.Parent<IInputValueDefinition>();
        if (field.DefaultValue is not null)
        {
            context.WriteValue(field.DefaultValue.Print());
        }
    }
}
#pragma warning restore IDE1006 // Naming Styles
