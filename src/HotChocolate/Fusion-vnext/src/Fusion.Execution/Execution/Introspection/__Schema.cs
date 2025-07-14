#pragma warning disable IDE1006 // Naming Styles
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution.Introspection;

// ReSharper disable once InconsistentNaming
internal static class __Schema
{
    public static void Description(FieldContext context)
        => context.WriteValue(context.Schema.Description);

    public static void Types(FieldContext context)
    {
        var list = context.ResultPool.RentObjectListResult();
        context.FieldResult.SetNextValue(list);

        foreach (var type in context.Schema.Types)
        {
            context.AddRuntimeResult(type);
            list.SetNextValue(context.ResultPool.RentObjectResult());
        }
    }

    public static void QueryType(FieldContext context)
    {
        context.AddRuntimeResult(context.Schema.QueryType);
        context.FieldResult.SetNextValue(context.ResultPool.RentObjectResult());
    }

    public static void MutationType(FieldContext context)
    {
        if (context.Schema.MutationType is not null)
        {
            context.AddRuntimeResult(context.Schema.MutationType);
            context.FieldResult.SetNextValue(context.ResultPool.RentObjectResult());
        }
    }

    public static void SubscriptionType(FieldContext context)
    {
        if (context.Schema.MutationType is not null)
        {
            context.AddRuntimeResult(context.Schema.MutationType);
            context.FieldResult.SetNextValue(context.ResultPool.RentObjectResult());
        }
    }

    public static void Directives(FieldContext context)
    {
        var list = context.ResultPool.RentObjectListResult();
        context.FieldResult.SetNextValue(list);

        foreach (var directiveDefinition in context.Schema.DirectiveDefinitions)
        {
            context.AddRuntimeResult(directiveDefinition);
            list.SetNextValue(context.ResultPool.RentObjectResult());
        }
    }
}
#pragma warning restore IDE1006 // Naming Styles
