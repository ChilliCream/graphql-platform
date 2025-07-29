using HotChocolate.Features;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Types.Completion;

public abstract class CompositeTypeInterceptor
{
    public virtual void OnCompleteOutputField(
        ICompositeSchemaBuilderContext context,
        IComplexTypeDefinition type,
        IOutputFieldDefinition field,
        OperationType? operationType,
        ref IFeatureCollection features)
    {
    }

    public virtual void OnBeforeCompleteSchema(
        ICompositeSchemaBuilderContext context,
        ref IFeatureCollection features)
    {
    }
}
