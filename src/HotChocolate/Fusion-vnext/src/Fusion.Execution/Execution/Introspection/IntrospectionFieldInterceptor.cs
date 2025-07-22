using HotChocolate.Features;
using HotChocolate.Fusion.Types.Completion;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Introspection;

internal sealed class IntrospectionFieldInterceptor : CompositeTypeInterceptor
{
    public override void OnCompleteOutputField(
        ICompositeSchemaBuilderContext context,
        IComplexTypeDefinition type,
        IOutputFieldDefinition field,
        OperationType? operationType,
        ref IFeatureCollection features)
    {
        var typeLookup = context.Features.GetRequired<Dictionary<string, ITypeResolverInterceptor>>();
        var typeName = type.Name;

        if (operationType.HasValue)
        {
            typeName = operationType.Value.ToString();
        }

        if (typeLookup.TryGetValue(typeName, out var resolverInterceptor))
        {
            if (features.IsReadOnly)
            {
                features = features.IsEmpty
                    ? new FeatureCollection()
                    : new FeatureCollection(features);
            }

            resolverInterceptor.OnApplyResolver(field.Name, features);
        }
    }

    public override void OnCompleteSchema(
        ICompositeSchemaBuilderContext context,
        ref IFeatureCollection features)
    {
        if (!features.IsEmpty)
        {
            features = features.IsReadOnly
                ? new FeatureCollection(features)
                : features;

            features.Set<Dictionary<string, ITypeResolverInterceptor>>(null);
        }
    }
}
