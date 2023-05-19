using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Planning;

internal sealed class IntrospectionExecutionStep : ExecutionStep
{
    public IntrospectionExecutionStep(ObjectTypeInfo queryTypeInfo)
        : base(queryTypeInfo, null)
    {
    }
}
