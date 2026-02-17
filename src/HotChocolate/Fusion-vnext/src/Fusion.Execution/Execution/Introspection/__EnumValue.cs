using HotChocolate.Features;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Introspection;

// ReSharper disable once InconsistentNaming
internal sealed class __EnumValue : ITypeResolverInterceptor
{
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

            case "isDeprecated":
                features.Set(new ResolveFieldValue(IsDeprecated));
                break;

            case "deprecationReason":
                features.Set(new ResolveFieldValue(DeprecationReason));
                break;
        }
    }

    public static void Name(FieldContext context)
        => context.WriteValue(context.Parent<IEnumValue>().Name);

    public static void Description(FieldContext context)
        => context.WriteValue(context.Parent<IEnumValue>().Description);

    public static void IsDeprecated(FieldContext context)
        => context.WriteValue(context.Parent<IEnumValue>().IsDeprecated);

    public static void DeprecationReason(FieldContext context)
        => context.WriteValue(context.Parent<IEnumValue>().DeprecationReason);
}
