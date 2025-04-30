using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Fusion.Types;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

public sealed class OperationPlanContext
{
    private readonly ISourceSchemaClientScope _clientScope;

    public OperationPlanContext(
        OperationPlan operationPlan,
        GraphQLRequestContext requestContext)
    {
        OperationPlan = operationPlan;
        RequestContext = requestContext;
        _clientScope = requestContext.RequestServices.GetRequiredService<ISourceSchemaClientScope>();
    }

    public OperationPlan OperationPlan { get; }

    public GraphQLRequestContext RequestContext { get; }

    public FetchResultStore ResultStore { get; } = new();

    public ImmutableArray<VariableValues> CreateVariables(
        ImmutableHashSet<string> variables,
        ImmutableArray<OperationRequirement> requirements)
    {
        throw new NotImplementedException("Use CreateVariables2 instead.");
    }

    public ISourceSchemaClient GetClient(string schemaName)
        => _clientScope.GetClient(schemaName);
}
