using HotChocolate.Features;
using HotChocolate.Fusion.Execution.Nodes;

namespace HotChocolate.Fusion.Execution.Introspection;

// ReSharper disable once InconsistentNaming
internal sealed class __Schema : ITypeResolverInterceptor
{
    public void OnApplyResolver(string fieldName, IFeatureCollection features)
    {
        switch (fieldName)
        {
            case "description":
                features.Set(new ResolveFieldValue(Description));
                break;

            case "types":
                features.Set(new ResolveFieldValue(Types));
                break;

            case "queryType":
                features.Set(new ResolveFieldValue(QueryType));
                break;

            case "mutationType":
                features.Set(new ResolveFieldValue(MutationType));
                break;

            case "subscriptionType":
                features.Set(new ResolveFieldValue(SubscriptionType));
                break;

            case "directives":
                features.Set(new ResolveFieldValue(Directives));
                break;
        }
    }

    public static void Description(FieldContext context)
        => context.WriteValue(context.Schema.Description);

    public static void Types(FieldContext context)
    {
        var list = context.FieldResult.CreateListValue(context.Schema.Types.Count);

        var i = 0;
        foreach (var element in list.EnumerateArray())
        {
            var type = context.Schema.Types[i++];
            context.AddRuntimeResult(type);
            element.CreateObjectValue(context.Selection, context.IncludeFlags);
        }
    }

    public static void QueryType(FieldContext context)
    {
        context.AddRuntimeResult(context.Schema.QueryType);
        context.FieldResult.CreateObjectValue(context.Selection, context.IncludeFlags);
    }

    public static void MutationType(FieldContext context)
    {
        if (context.Schema.MutationType is not null)
        {
            context.AddRuntimeResult(context.Schema.MutationType);
            context.FieldResult.CreateObjectValue(context.Selection, context.IncludeFlags);
        }
    }

    public static void SubscriptionType(FieldContext context)
    {
        if (context.Schema.SubscriptionType is not null)
        {
            context.AddRuntimeResult(context.Schema.SubscriptionType);
            context.FieldResult.CreateObjectValue(context.Selection, context.IncludeFlags);
        }
    }

    public static void Directives(FieldContext context)
    {
        var list = context.FieldResult.CreateListValue(context.Schema.DirectiveDefinitions.Count);

        var i = 0;
        foreach (var element in list.EnumerateArray())
        {
            var type = context.Schema.DirectiveDefinitions[i++];
            context.AddRuntimeResult(type);
            element.CreateObjectValue(context.Selection, context.IncludeFlags);
        }
    }
}
