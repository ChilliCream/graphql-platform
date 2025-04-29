using System.Collections.Immutable;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Planning;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution;

public class OperationPlanContext
{
    public required OperationPlan OperationPlan { get; init; }

    public required GraphQLRequestContext RequestContext { get; init; }

    public IValueNode? CreateVariables(ImmutableArray<OperationRequirement> requirements)
    {
        throw new NotImplementedException();
    }

    public IGraphQLClient GetClient(string schemaName)
    {
        throw new NotImplementedException();
    }
}
