using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;

namespace HotChocolate.Fusion.Planning;

internal class RequirementExecutionStep : ExecutionStep
{
    public RequirementExecutionStep(
        string subgraphName,
        ObjectTypeInfo selectionSetTypeInfo,
        ISelection? parentSelection)
        : base(selectionSetTypeInfo, parentSelection)
    {

    }
}
