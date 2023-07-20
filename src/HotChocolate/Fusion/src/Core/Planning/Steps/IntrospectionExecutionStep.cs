using HotChocolate.Fusion.Metadata;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

internal sealed class IntrospectionExecutionStep : ExecutionStep
{
    public IntrospectionExecutionStep(IObjectType queryType, ObjectTypeMetadata queryTypeMetadata)
        : base(null, queryType, queryTypeMetadata)
    {
    }
}
