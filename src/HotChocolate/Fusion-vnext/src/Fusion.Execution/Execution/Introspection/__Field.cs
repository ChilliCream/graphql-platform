#pragma warning disable IDE1006 // Naming Styles
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Introspection;

// ReSharper disable once InconsistentNaming
internal static class __Field
{
    public static void Name(FieldContext context)
        => context.WriteValue(context.Parent<IOutputFieldDefinition>().Name);

    public static void Description(FieldContext context)
    {
        if (context.Parent<IOutputFieldDefinition>() is { Description: not null } fieldDef)
        {
            context.WriteValue(fieldDef.Description);
        }
    }

    public static void Arguments(FieldContext context)
    {
        var field = context.Parent<IOutputFieldDefinition>();
        var includeDeprecated = context.ArgumentValue<BooleanValueNode>("includeDeprecated").Value;
        var list = context.ResultPool.RentObjectListResult();
        context.FieldResult.SetNextValue(list);

        foreach (var value in field.Arguments)
        {
            if (!includeDeprecated && value.IsDeprecated)
            {
                continue;
            }

            context.AddRuntimeResult(value);
            list.SetNextValue(context.ResultPool.RentObjectResult());
        }
    }

    public static void Type(FieldContext context)
    {
        var field = context.Parent<IOutputFieldDefinition>();
        context.AddRuntimeResult(field.Type);
        context.FieldResult.SetNextValue(context.ResultPool.RentObjectResult());
    }

    public static void IsDeprecated(FieldContext context)
    {
        var field = context.Parent<IOutputFieldDefinition>();
        context.WriteValue(field.IsDeprecated);
    }

    public static void DeprecationReason(FieldContext context)
    {
        var field = context.Parent<IOutputFieldDefinition>();
        if (field.DeprecationReason is not null)
        {
            context.WriteValue(field.DeprecationReason);
        }
    }
}
#pragma warning restore IDE1006 // Naming Styles
