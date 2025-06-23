using System.Collections.Immutable;
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
        OperationExecutionPlan operationPlan,
        IReadOnlyDictionary<string, IValueNode>? variables,
        RequestContext requestContext)
    {
        OperationPlan = operationPlan;
        RequestContext = requestContext;
        Variables = variables;
        _clientScope = requestContext.RequestServices.GetRequiredService<ISourceSchemaClientScope>();
    }

    public OperationExecutionPlan OperationPlan { get; }

    public IReadOnlyDictionary<string, IValueNode>? Variables { get; }

    public ISchemaDefinition Schema => RequestContext.Schema;

    public RequestContext RequestContext { get; }

    public FetchResultStore ResultStore { get; } = new();

    // NOTE: this version is too simple, we will rewrite it once we have implemented the SelectionSetMap.
    public ImmutableArray<VariableValues>? TryCreateVariables(
        SelectionPath currentPath,
        ImmutableArray<string> variables,
        ImmutableArray<OperationRequirement> requirements)
    {
        if(variables.Length == 0 && requirements.Length == 0)
        {
            return ImmutableArray<VariableValues>.Empty;
        }

        var pathThroughVariables = GetPathThroughVariables(variables);

        if (requirements.Length > 0)
        {
            ImmutableArray<VariableValues>.Builder? builder = null;

            foreach (var (path, variableValues) in ResultStore.GetValues(currentPath, [..requirements.Select(t => (t.Key, t.Map))]))
            {
                variableValues.AddRange(pathThroughVariables);
                builder ??= ImmutableArray.CreateBuilder<VariableValues>(requirements.Length);
                builder.Add(new VariableValues(path, new ObjectValueNode(variableValues)));
            }

            return builder?.ToImmutable() ?? null;
        }

        return ImmutableArray<VariableValues>.Empty.Add(
            new VariableValues(Path.Root, new ObjectValueNode(pathThroughVariables)));
    }

    private IReadOnlyList<ObjectFieldNode> GetPathThroughVariables(
        ImmutableArray<string> requiredVariables)
    {
        if (Variables is null || requiredVariables.Length == 0)
        {
            return [];
        }

        var variables = new List<ObjectFieldNode>(Variables.Count);

        foreach (var variableName in requiredVariables)
        {
            if (Variables.TryGetValue(variableName, out var variableValue))
            {
                variables.Add(new ObjectFieldNode(variableName, variableValue));
            }
            else
            {
                throw new InvalidOperationException(
                    $"The variable '{variableName}' is not defined.");
            }
        }

        return variables;
    }

    public ISourceSchemaClient GetClient(string schemaName)
        => _clientScope.GetClient(schemaName);
}
