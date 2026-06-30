using System.Diagnostics;
using HotChocolate.Features;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Introspection;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Introspection;

// ReSharper disable once InconsistentNaming
internal sealed class __Field : ITypeResolverInterceptor
{
    private readonly bool _enableOptInFeatures;

    public __Field(bool enableOptInFeatures = false)
    {
        _enableOptInFeatures = enableOptInFeatures;
    }

    public void OnApplyResolver(string fieldName, IFeatureCollection features)
    {
        switch (fieldName)
        {
            case "name":
                features.Set(new ResolveFieldValue(Name));
                break;

            case "description":
                features.Set(new ResolveFieldValue(Description));
                break;

            case "args":
                if (_enableOptInFeatures)
                {
                    features.Set(new ResolveFieldValue(ArgumentsWithOptIn));
                }
                else
                {
                    features.Set(new ResolveFieldValue(Arguments));
                }
                break;

            case "type":
                features.Set(new ResolveFieldValue(Type));
                break;

            case "isDeprecated":
                features.Set(new ResolveFieldValue(IsDeprecated));
                break;

            case "deprecationReason":
                features.Set(new ResolveFieldValue(DeprecationReason));
                break;

            case "requiresOptIn" when _enableOptInFeatures:
                features.Set(new ResolveFieldValue(RequiresOptIn));
                break;
        }
    }

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
        var count = includeDeprecated
            ? field.Arguments.Count
            : field.Arguments.Count(t => !t.IsDeprecated);
        var list = context.FieldResult.CreateListValue(count);

        var index = 0;
        foreach (var element in list.EnumerateArray())
        {
            var argument = field.Arguments[index++];

            if (!includeDeprecated && argument.IsDeprecated)
            {
                continue;
            }

            context.AddRuntimeResult(argument);
            element.CreateObjectValue(context.Selection, context.IncludeFlags);
        }
    }

    public static void Type(FieldContext context)
    {
        var field = context.Parent<IOutputFieldDefinition>();
        context.AddRuntimeResult(field.Type);
        context.FieldResult.CreateObjectValue(context.Selection, context.IncludeFlags);
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

    public static void ArgumentsWithOptIn(FieldContext context)
    {
        var field = context.Parent<IOutputFieldDefinition>();
        var includeDeprecated = context.ArgumentValue<BooleanValueNode>("includeDeprecated").Value;
        var includeOptIn = __Schema.ReadIncludeOptIn(context);
        var count = field.Arguments.Count(
            a => (includeDeprecated || !a.IsDeprecated)
                && OptInIntrospectionHelper.IsIncluded(a.Directives, includeOptIn));
        using var list = context.FieldResult.CreateListValue(count).EnumerateArray().GetEnumerator();

        foreach (var argument in field.Arguments)
        {
            if (!includeDeprecated && argument.IsDeprecated)
            {
                continue;
            }

            if (!OptInIntrospectionHelper.IsIncluded(argument.Directives, includeOptIn))
            {
                continue;
            }

            if (!list.MoveNext())
            {
                Debug.Fail("Expected enumerator of list value to be able to advance");
                break;
            }

            context.AddRuntimeResult(argument);
            list.Current.CreateObjectValue(context.Selection, context.IncludeFlags);
        }
    }

    public static void RequiresOptIn(FieldContext context)
    {
        var field = context.Parent<IOutputFieldDefinition>();
        WriteRequiresOptIn(context, field.Directives);
    }

    internal static void WriteRequiresOptIn(
        FieldContext context,
        IReadOnlyDirectiveCollection directives)
    {
        var features = CollectRequiresOptInFeatures(directives);
        using var list = context.FieldResult.CreateListValue(features.Length).EnumerateArray().GetEnumerator();

        foreach (var feature in features)
        {
            if (!list.MoveNext())
            {
                break;
            }

            list.Current.SetStringValue(feature);
        }
    }

    internal static string[] CollectRequiresOptInFeatures(IReadOnlyDirectiveCollection directives)
    {
        List<string>? features = null;

        foreach (var directive in directives)
        {
            if (!directive.Name.Equals(
                    DirectiveNames.RequiresOptIn.Name,
                    StringComparison.Ordinal))
            {
                continue;
            }

            if (directive is FusionDirective fusionDirective
                && fusionDirective.Arguments.TryGetValue(
                    DirectiveNames.RequiresOptIn.Arguments.Feature,
                    out var argValue)
                && argValue is StringValueNode feature)
            {
                (features ??= []).Add(feature.Value);
            }
        }

        return features is null ? [] : features.ToArray();
    }
}
