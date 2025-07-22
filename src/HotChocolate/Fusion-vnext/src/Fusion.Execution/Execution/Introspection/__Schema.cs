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
        var list = context.ResultPool.RentObjectListResult();
        context.FieldResult.SetNextValue(list);

        foreach (var type in context.Schema.Types)
        {
            context.AddRuntimeResult(type);
            list.SetNextValue(context.RentInitializedObjectResult());
        }
    }

    public static void QueryType(FieldContext context)
    {
        context.AddRuntimeResult(context.Schema.QueryType);
        context.FieldResult.SetNextValue(context.RentInitializedObjectResult());
    }

    public static void MutationType(FieldContext context)
    {
        if (context.Schema.MutationType is not null)
        {
            context.AddRuntimeResult(context.Schema.MutationType);
            context.FieldResult.SetNextValue(context.RentInitializedObjectResult());
        }
    }

    public static void SubscriptionType(FieldContext context)
    {
        if (context.Schema.MutationType is not null)
        {
            context.AddRuntimeResult(context.Schema.MutationType);
            context.FieldResult.SetNextValue(context.RentInitializedObjectResult());
        }
    }

    public static void Directives(FieldContext context)
    {
        var list = context.ResultPool.RentObjectListResult();
        context.FieldResult.SetNextValue(list);

        foreach (var directiveDefinition in context.Schema.DirectiveDefinitions)
        {
            context.AddRuntimeResult(directiveDefinition);
            list.SetNextValue(context.RentInitializedObjectResult());
        }
    }
}
