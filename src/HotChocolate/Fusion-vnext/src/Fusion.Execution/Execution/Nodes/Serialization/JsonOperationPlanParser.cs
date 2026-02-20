using System.Collections.Immutable;
using System.Text.Json;
using HotChocolate.Fusion.Language;
using HotChocolate.Language;

namespace HotChocolate.Fusion.Execution.Nodes.Serialization;

public sealed class JsonOperationPlanParser : OperationPlanParser
{
    private readonly OperationCompiler _operationCompiler;

    public JsonOperationPlanParser(OperationCompiler operationCompiler)
    {
        ArgumentNullException.ThrowIfNull(operationCompiler);

        _operationCompiler = operationCompiler;
    }

    public override OperationPlan Parse(ReadOnlyMemory<byte> planSourceText)
    {
        using var document = JsonDocument.Parse(planSourceText);
        var rootElement = document.RootElement;
        var searchSpace = 0;
        var expandedNodes = 0;

        var id = rootElement.GetProperty("id").GetString()!;
        var operation = ParseOperation(rootElement.GetProperty("operation"));

        if (rootElement.TryGetProperty("searchSpace", out var searchSpaceElement))
        {
            searchSpace = searchSpaceElement.GetInt32();
        }

        if (rootElement.TryGetProperty("expandedNodes", out var expandedNodesElement))
        {
            expandedNodes = expandedNodesElement.GetInt32();
        }

        var nodes = ParseNodes(rootElement.GetProperty("nodes"), operation);

        return OperationPlan.Create(
            id,
            operation,
            [.. nodes.Where(n => n.Dependencies.Length == 0)],
            nodes,
            searchSpace,
            expandedNodes);
    }

    private Operation ParseOperation(JsonElement operationElement)
    {
        var sourceText = operationElement.GetProperty("document").GetString()!;
        var id = operationElement.GetProperty("id").GetString()!;
        var hash = operationElement.GetProperty("hash").GetString()!;

        var document = Utf8GraphQLParser.Parse(sourceText);
        var operationDefinition = document.Definitions.OfType<OperationDefinitionNode>().SingleOrDefault();

        if (operationDefinition is null)
        {
            throw new InvalidOperationException(
                "There must be exactly one operation definition in the "
                + "operation document of the operation plan.");
        }

        return _operationCompiler.Compile(id, hash, operationDefinition);
    }

    private ImmutableArray<ExecutionNode> ParseNodes(JsonElement nodesElement, Operation operation)
    {
        var nodes = new List<(ExecutionNode, int[]?, Dictionary<string, int>?, int?)>();

        foreach (var nodeElement in nodesElement.EnumerateArray())
        {
            var nodeType = nodeElement.GetProperty("type").GetString()!;
            var id = nodeElement.GetProperty("id").GetInt32();

            (ExecutionNode, int[]?, Dictionary<string, int>?, int?) node = nodeType switch
            {
                "Operation" => ParseOperationNode(nodeElement, id),
                "Introspection" => ParseIntrospectionNode(nodeElement, id, operation),
                "Node" => ParseNodeFieldNode(nodeElement, id, operation),
                _ => throw new NotSupportedException($"Unsupported node type: {nodeType}")
            };

            nodes.Add(node);
        }

        var nodeMap = nodes.ToDictionary(n => n.Item1.Id, n => n.Item1);

        foreach (var (node, dependencies, branches, fallback) in nodes)
        {
            if (dependencies is not null)
            {
                foreach (var dependencyId in dependencies)
                {
                    if (nodeMap.TryGetValue(dependencyId, out var dependencyNode))
                    {
                        node.AddDependency(dependencyNode);
                        dependencyNode.AddDependent(node);
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Dependency node with ID {dependencyId} not found for node {node.Id}.");
                    }
                }
            }

            if (node is NodeFieldExecutionNode nodeExecutionNode)
            {
                if (branches is not null)
                {
                    foreach (var (typeName, nodeId) in branches)
                    {
                        if (nodeMap.TryGetValue(nodeId, out var branchNode))
                        {
                            nodeExecutionNode.AddBranch(typeName, branchNode);
                        }
                        else
                        {
                            throw new InvalidOperationException(
                                $"Branch node with ID {nodeId} not found for node {node.Id}.");
                        }
                    }
                }

                if (fallback.HasValue)
                {
                    if (nodeMap.TryGetValue(fallback.Value, out var fallbackNode))
                    {
                        nodeExecutionNode.AddFallbackQuery(fallbackNode);
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"Fallback node with ID {fallback} not found for node {node.Id}.");
                    }
                }
            }
        }

        foreach (var (node, _, _, _) in nodes)
        {
            node.Seal();
        }

        return [.. nodeMap.Values.OrderBy(t => t.Id)];
    }

    private static (OperationExecutionNode, int[]?, Dictionary<string, int>?, int?) ParseOperationNode(
        JsonElement nodeElement, int id)
    {
        string? schemaName = null;
        if (nodeElement.TryGetProperty("schema", out var schemaElement))
        {
            schemaName = schemaElement.GetString()!;
        }

        var operationElement = nodeElement.GetProperty("operation");
        var operationName = operationElement.GetProperty("name").GetString()!;
        var operationType = Enum.Parse<OperationType>(operationElement.GetProperty("kind").GetString()!);
        var document = operationElement.GetProperty("document").GetString()!;
        var hash = operationElement.GetProperty("hash").GetString()!;

        SelectionPath? source = null;
        SelectionPath? target = null;
        List<OperationRequirement>? requirements = null;
        string[]? forwardedVariables = null;
        string[]? responseNames = null;
        int[]? dependencies = null;

        if (nodeElement.TryGetProperty("source", out var sourceElement))
        {
            source = SelectionPath.Parse(sourceElement.GetString()!);
        }

        if (nodeElement.TryGetProperty("target", out var targetElement))
        {
            target = SelectionPath.Parse(targetElement.GetString()!);
        }

        if (nodeElement.TryGetProperty("requirements", out var requirementsElement))
        {
            requirements = [];

            foreach (var requirementElement in requirementsElement.EnumerateArray())
            {
                var requirementName = requirementElement.GetProperty("name").GetString()!;
                var requirementType = requirementElement.GetProperty("type").GetString()!;
                var requirementPath = requirementElement.GetProperty("path").GetString()!;
                var selectionMap = requirementElement.GetProperty("selectionMap").GetString()!;

                requirements.Add(new OperationRequirement(
                    requirementName,
                    Utf8GraphQLParser.Syntax.ParseTypeReference(requirementType),
                    SelectionPath.Parse(requirementPath),
                    FieldSelectionMapParser.Parse(selectionMap)));
            }
        }

        if (nodeElement.TryGetProperty("forwardedVariables", out var forwardedVariablesElement))
        {
            forwardedVariables = forwardedVariablesElement
                .EnumerateArray()
                .Select(e => e.GetString()!)
                .ToArray();
        }

        if (nodeElement.TryGetProperty("responseNames", out var responseNamesElement))
        {
            responseNames = responseNamesElement
                .EnumerateArray()
                .Select(e => e.GetString()!)
                .ToArray();
        }

        if (nodeElement.TryGetProperty("dependencies", out var dependenciesElement))
        {
            dependencies = dependenciesElement
                .EnumerateArray()
                .Select(e => e.GetInt32())
                .ToArray();
        }

        var conditions = TryParseConditions(nodeElement);

        var requiresFileUpload = nodeElement.TryGetProperty("requiresFileUpload", out var requiresFileUploadElement)
            && requiresFileUploadElement.ValueKind == JsonValueKind.True;

        var node = new OperationExecutionNode(
            id,
            new OperationSourceText(
                operationName,
                operationType,
                document,
                hash),
            schemaName,
            target ?? SelectionPath.Root,
            source ?? SelectionPath.Root,
            requirements?.ToArray() ?? [],
            forwardedVariables ?? [],
            responseNames ?? [],
            conditions,
            requiresFileUpload);

        return (node, dependencies, null, null);
    }

    private static (IntrospectionExecutionNode, int[]?, Dictionary<string, int>?, int?) ParseIntrospectionNode(
        JsonElement nodeElement,
        int id,
        Operation operation)
    {
        var selectionsElement = nodeElement.GetProperty("selections");
        var selections = new List<Selection>();

        foreach (var selectionElement in selectionsElement.EnumerateArray())
        {
            var responseName = selectionElement.GetProperty("responseName").GetString()!;
            var selection = GetRootSelection(responseName);
            selections.Add(selection);
        }

        var conditions = TryParseConditions(nodeElement);

        var node = new IntrospectionExecutionNode(
            id,
            selections.ToArray(),
            conditions);

        return (node, null, null, null);

        Selection GetRootSelection(string responseName)
        {
            foreach (var selection in operation.RootSelectionSet.Selections)
            {
                if (selection.ResponseName.Equals(responseName, StringComparison.Ordinal))
                {
                    return selection;
                }
            }

            throw new InvalidOperationException(
                $"Root selection with response name '{responseName}' not found in operation '{operation.Id}'.");
        }
    }

    private static (NodeFieldExecutionNode, int[]?, Dictionary<string, int>?, int?) ParseNodeFieldNode(
        JsonElement nodeElement, int id, Operation operation)
    {
        var responseName = nodeElement.GetProperty("responseName").GetString()!;

        var idValueProperty = nodeElement.GetProperty("idValue").GetString()!;
        var idValue = Utf8GraphQLParser.Syntax.ParseValueLiteral(idValueProperty, false);

        if (idValue is VariableNode variableNode)
        {
            if (!operation.Definition.VariableDefinitions
                .Any(v => v.Variable.Equals(variableNode, SyntaxComparison.Syntax)))
            {
                throw new InvalidOperationException(
                    $"'idValue' references non-existent '{variableNode.Name}' variable.");
            }
        }
        else if (idValue is not StringValueNode)
        {
            throw new InvalidOperationException(
                $"Expected 'idValue' to be a {nameof(VariableNode)} or {nameof(StringValueNode)}.");
        }

        var branchesElement = nodeElement.GetProperty("branches");
        var branches = new Dictionary<string, int>();

        foreach (var branch in branchesElement.EnumerateObject())
        {
            var nodeId = branch.Value.GetInt32();

            branches.Add(branch.Name, nodeId);
        }

        var fallbackNodeId = nodeElement.GetProperty("fallback").GetInt32();

        var conditions = TryParseConditions(nodeElement);

        var node = new NodeFieldExecutionNode(
            id,
            responseName,
            idValue,
            conditions);

        return (node, null, branches, fallbackNodeId);
    }

    private static ExecutionNodeCondition[] TryParseConditions(JsonElement nodeElement)
    {
        if (!nodeElement.TryGetProperty("conditions", out var conditionsElement))
        {
            return [];
        }

        var conditions = new List<ExecutionNodeCondition>();

        foreach (var conditionElement in conditionsElement.EnumerateArray())
        {
            conditions.Add(new ExecutionNodeCondition
            {
                VariableName = conditionElement.GetProperty("variable").GetString()!.TrimStart('$'),
                PassingValue = conditionElement.GetProperty("passingValue").GetBoolean()
            });
        }

        return conditions.ToArray();
    }
}
