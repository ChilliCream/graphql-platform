using HotChocolate.Features;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Introspection;

// ReSharper disable once InconsistentNaming
internal sealed class __OptInFeatureStability : ITypeResolverInterceptor
{
    public void OnApplyResolver(string fieldName, IFeatureCollection features)
    {
        switch (fieldName)
        {
            case "feature":
                features.Set(new ResolveFieldValue(Feature));
                break;

            case "stability":
                features.Set(new ResolveFieldValue(Stability));
                break;
        }
    }

    public static void Feature(FieldContext context)
    {
        var directive = context.Parent<FusionDirective>();

        if (directive.Arguments.TryGetValue(
                DirectiveNames.OptInFeatureStability.Arguments.Feature,
                out var argValue)
            && argValue is StringValueNode feature)
        {
            context.WriteValue(feature.Value);
        }
    }

    public static void Stability(FieldContext context)
    {
        var directive = context.Parent<FusionDirective>();

        if (directive.Arguments.TryGetValue(
                DirectiveNames.OptInFeatureStability.Arguments.Stability,
                out var argValue)
            && argValue is StringValueNode stability)
        {
            context.WriteValue(stability.Value);
        }
    }
}
