using HotChocolate.Features;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types.Introspection;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Introspection;

// ReSharper disable once InconsistentNaming
internal sealed class __Schema : ITypeResolverInterceptor
{
    private readonly bool _enableOptInFeatures;

    public __Schema(bool enableOptInFeatures = false)
    {
        _enableOptInFeatures = enableOptInFeatures;
    }

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
                if (_enableOptInFeatures)
                {
                    features.Set(new ResolveFieldValue(DirectivesWithOptIn));
                }
                else
                {
                    features.Set(new ResolveFieldValue(Directives));
                }
                break;

            case "optInFeatureStability" when _enableOptInFeatures:
                features.Set(new ResolveFieldValue(OptInFeatureStability));
                break;

            case "optInFeatures" when _enableOptInFeatures:
                features.Set(new ResolveFieldValue(OptInFeatures));
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
        var includeDeprecated = context.ArgumentValue<BooleanValueNode>("includeDeprecated").Value;
        var directiveDefinitions = context.Schema.DirectiveDefinitions;
        var count = includeDeprecated
            ? directiveDefinitions.Count
            : directiveDefinitions.Count(d => !d.IsDeprecated);
        using var list = context.FieldResult.CreateListValue(count).EnumerateArray().GetEnumerator();

        foreach (var directiveDef in directiveDefinitions)
        {
            if (!includeDeprecated && directiveDef.IsDeprecated)
            {
                continue;
            }

            if (!list.MoveNext())
            {
                break;
            }

            context.AddRuntimeResult(directiveDef);
            list.Current.CreateObjectValue(context.Selection, context.IncludeFlags);
        }
    }

    public static void DirectivesWithOptIn(FieldContext context)
    {
        var includeDeprecated = context.ArgumentValue<BooleanValueNode>("includeDeprecated").Value;
        var includeOptIn = ReadIncludeOptIn(context);
        var schema = context.Schema;
        var count = schema.DirectiveDefinitions.Count(
            d => (includeDeprecated || !d.IsDeprecated)
                && OptInIntrospectionHelper.IsIncluded(d.Directives, includeOptIn));
        using var list = context.FieldResult.CreateListValue(count).EnumerateArray().GetEnumerator();

        foreach (var directiveDef in schema.DirectiveDefinitions)
        {
            if (!includeDeprecated && directiveDef.IsDeprecated)
            {
                continue;
            }

            if (!OptInIntrospectionHelper.IsIncluded(directiveDef.Directives, includeOptIn))
            {
                continue;
            }

            if (!list.MoveNext())
            {
                break;
            }

            context.AddRuntimeResult(directiveDef);
            list.Current.CreateObjectValue(context.Selection, context.IncludeFlags);
        }
    }

    public static void OptInFeatures(FieldContext context)
    {
        var optInFeatures = context.Schema.Features.Get<FusionOptInFeatures>();
        var count = optInFeatures?.Count ?? 0;

        if (optInFeatures is null || count == 0)
        {
            context.FieldResult.CreateListValue(0);
            return;
        }

        var features = optInFeatures.ToArray();
        using var list = context.FieldResult.CreateListValue(count).EnumerateArray().GetEnumerator();

        for (var i = 0; i < features.Length; i++)
        {
            if (!list.MoveNext())
            {
                break;
            }

            list.Current.SetStringValue(features[i]);
        }
    }

    public static void OptInFeatureStability(FieldContext context)
    {
        var schema = context.Schema;
        var stabilityDirectives = schema.Directives
            .Where(d => d.Name.Equals(
                DirectiveNames.OptInFeatureStability.Name,
                StringComparison.Ordinal))
            .ToArray();
        var list = context.FieldResult.CreateListValue(stabilityDirectives.Length);

        var i = 0;
        foreach (var element in list.EnumerateArray())
        {
            context.AddRuntimeResult(stabilityDirectives[i++]);
            element.CreateObjectValue(context.Selection, context.IncludeFlags);
        }
    }

    internal static string[] ReadIncludeOptIn(FieldContext context)
    {
        var node = context.ArgumentValue<IValueNode>("includeOptIn");

        if (node is NullValueNode or not ListValueNode)
        {
            return [];
        }

        var list = (ListValueNode)node;

        if (list.Items.Count == 0)
        {
            return [];
        }

        var result = new string[list.Items.Count];

        for (var i = 0; i < list.Items.Count; i++)
        {
            if (list.Items[i] is StringValueNode sv)
            {
                result[i] = sv.Value;
            }
        }

        return result;
    }
}
