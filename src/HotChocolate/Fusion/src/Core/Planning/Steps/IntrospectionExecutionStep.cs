using HotChocolate.Fusion.Metadata;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

internal sealed class IntrospectionExecutionStep : ExecutionStep
{
    public IntrospectionExecutionStep(IObjectType queryType, ObjectTypeInfo queryTypeInfo)
        : base(null, queryType, queryTypeInfo)
    {
    }
}
