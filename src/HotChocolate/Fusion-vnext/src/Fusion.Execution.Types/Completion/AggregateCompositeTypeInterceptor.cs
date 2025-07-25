using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Completion;

internal sealed class AggregateCompositeTypeInterceptor : CompositeTypeInterceptor
{
    private readonly CompositeTypeInterceptor[]  _interceptors;

    public AggregateCompositeTypeInterceptor(CompositeTypeInterceptor[] interceptors)
    {
        ArgumentNullException.ThrowIfNull(interceptors);
        _interceptors = interceptors;
    }

    public override void OnBeforeCompleteSchema(
        ICompositeSchemaBuilderContext context,
        ref IFeatureCollection features)
    {
        foreach (var interceptor in _interceptors)
        {
            interceptor.OnBeforeCompleteSchema(context, ref features);
        }
    }

    public override void OnCompleteOutputField(
        ICompositeSchemaBuilderContext context,
        IComplexTypeDefinition type,
        IOutputFieldDefinition field,
        OperationType? operationType,
        ref IFeatureCollection features)
    {
        foreach (var interceptor in _interceptors)
        {
            interceptor.OnCompleteOutputField(context, type, field, operationType, ref features);
        }
    }
}
