using System.Collections.Immutable;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Language;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Execution.Nodes.Serialization;

/// <summary>
/// Turns a JSON-encoded operation plan back into a living <see cref="OperationPlan"/>
/// object, including the original GraphQL operation, every execution node, and the
/// dependency graph that connects them.
/// </summary>
public sealed class JsonOperationPlanParser : OperationPlanParser
{
    private readonly OperationCompiler _operationCompiler;

    /// <summary>
    /// Initializes a new instance of <see cref="JsonOperationPlanParser"/>.
    /// </summary>
    /// <param name="operationCompiler">
    /// The compiler used to compile parsed operation definitions.
    /// </param>
    public JsonOperationPlanParser(OperationCompiler operationCompiler)
    {
        ArgumentNullException.ThrowIfNull(operationCompiler);

        _operationCompiler = operationCompiler;
    }

    /// <inheritdoc />
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

        var deferredGroups = ImmutableArray<DeferredExecutionGroup>.Empty;

        if (rootElement.TryGetProperty("deferredGroups", out var deferredGroupsElement))
        {
            var groupBuilder = ImmutableArray.CreateBuilder<DeferredExecutionGroup>();
            var groupMap = new Dictionary<int, DeferredExecutionGroup>();

            foreach (var groupElement in deferredGroupsElement.EnumerateArray())
            {
                var deferId = groupElement.GetProperty("id").GetInt32();

                string? label = null;
                if (groupElement.TryGetProperty("label", out var labelElement))
                {
                    label = labelElement.GetString();
                }

                var path = SelectionPath.Parse(groupElement.GetProperty("path").GetString()!);

                string? ifVariable = null;
                if (groupElement.TryGetProperty("ifVariable", out var ifVarElement))
                {
                    ifVariable = ifVarElement.GetString()!.TrimStart('$');
                }

                DeferredExecutionGroup? parent = null;
                if (groupElement.TryGetProperty("parentId", out var parentIdElement))
                {
                    groupMap.TryGetValue(parentIdElement.GetInt32(), out parent);
                }

                var groupOperation = ParseOperation(groupElement.GetProperty("operation"));

                var groupNodes = groupElement.TryGetProperty("nodes", out var groupNodesElement)
                    ? ParseNodes(groupNodesElement, groupOperation)
                    : [];

                var rootGroupNodes = groupNodes
                    .Where(n => n.Dependencies.Length == 0 && n.OptionalDependencies.Length == 0)
                    .ToImmutableArray();

                var group = new DeferredExecutionGroup(
                    deferId,
                    label,
                    path,
                    ifVariable,
                    parent,
                    groupOperation,
                    rootGroupNodes,
                    groupNodes);

                groupBuilder.Add(group);
                groupMap[deferId] = group;
            }

            deferredGroups = groupBuilder.ToImmutable();
        }

        // Root nodes are the entry points of the execution plan. A node is a
        // root when it has no dependencies at all, meaning the executor can
        // start it immediately without waiting for other nodes to finish.
        return OperationPlan.Create(
            id,
            operation,
            [.. nodes.Where(n => n.Dependencies.Length == 0 && n.OptionalDependencies.Length == 0)],
            nodes,
            deferredGroups,
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
            throw ThrowHelper.SingleOperationRequired();
        }

        return _operationCompiler.Compile(id, hash, operationDefinition);
    }

    private ImmutableArray<ExecutionNode> ParseNodes(JsonElement nodesElement, Operation operation)
    {
        // Phase 1: Read every JSON node element into a lightweight intermediate
        // object. We do not create real execution nodes yet because we first need
        // to know which operations belong to the same batch group.
        var parsedNodes = new List<ParsedNodeInfo>();

        foreach (var nodeElement in nodesElement.EnumerateArray())
        {
            var nodeType = nodeElement.GetProperty("type").GetString()!;
            var id = nodeElement.GetProperty("id").GetInt32();

            var schema = _operationCompiler.Schema;

            switch (nodeType)
            {
                case "Operation":
                    parsedNodes.Add(ParseOperationNodeInfo(nodeElement, id, schema));
                    break;

                case "OperationBatch":
                    parsedNodes.Add(ParseOperationBatchNodeInfo(nodeElement, id, schema));
                    break;

                case "Introspection":
                    parsedNodes.Add(ParseIntrospectionNodeInfo(nodeElement, id, operation));
                    break;

                case "Node":
                    parsedNodes.Add(ParseNodeFieldNodeInfo(nodeElement, id, operation));
                    break;

                default:
                    throw new NotSupportedException($"Unsupported node type: {nodeType}");
            }
        }

        // Phase 2: Separate operations that share a batching group identifier
        // from those that stand alone. Operations in the same group will be
        // merged into a single OperationBatchExecutionNode later, so the
        // gateway can send them to the downstream service in one network call.
        var batchGroups = new Dictionary<int, List<ParsedOperationNodeInfo>>();
        var standaloneNodes = new List<ParsedNodeInfo>();

        foreach (var parsed in parsedNodes)
        {
            if (parsed is ParsedOperationNodeInfo opInfo && opInfo.BatchingGroupId.HasValue)
            {
                if (!batchGroups.TryGetValue(opInfo.BatchingGroupId.Value, out var group))
                {
                    group = [];
                    batchGroups[opInfo.BatchingGroupId.Value] = group;
                }

                group.Add(opInfo);
            }
            else
            {
                standaloneNodes.Add(parsed);
            }
        }

        // Phase 3: Turn the intermediate objects into real execution nodes.
        // We also build a lookup from node identifier to execution node so that
        // Phase 4 can wire up dependencies efficiently.
        var allNodes = new List<(ExecutionNode Node, int[]? Dependencies, Dictionary<string, int>? Branches, int? Fallback)>();
        var nodeMap = new Dictionary<int, ExecutionNode>();

        // Merge each batch group into a single OperationBatchExecutionNode.
        // The group identifier becomes the node identifier, and every member
        // operation becomes an entry in the batch node's operation list.
        foreach (var (groupId, groupMembers) in batchGroups)
        {
            var operations = new List<OperationDefinition>();
            var allDeps = new HashSet<int>();

            foreach (var member in groupMembers)
            {
                operations.Add(member.ToOperationDefinition());

                if (member.Dependencies is not null)
                {
                    foreach (var dep in member.Dependencies)
                    {
                        allDeps.Add(dep);
                    }
                }
            }

            var batchNode = new OperationBatchExecutionNode(groupId, operations.ToArray());
            allNodes.Add((batchNode, allDeps.Count > 0 ? allDeps.ToArray() : null, null, null));
            nodeMap[groupId] = batchNode;
        }

        // Convert every node that does not belong to a batch group into its
        // own execution node (for example, a single-operation node or an
        // introspection node).
        foreach (var parsed in standaloneNodes)
        {
            var (node, deps, branches, fallback) = parsed.ToExecutionNodeTuple();
            allNodes.Add((node, deps, branches, fallback));
            nodeMap[node.Id] = node;
        }

        // When multiple operations are merged into one batch node, only the
        // group identifier survives as a real node identifier. Other code may
        // still reference the original member identifiers in dependency lists,
        // so we build a redirect map that translates each absorbed member
        // identifier to the batch node's group identifier.
        var idRedirects = new Dictionary<int, int>();

        foreach (var (groupId, groupMembers) in batchGroups)
        {
            foreach (var member in groupMembers)
            {
                if (member.Id != groupId)
                {
                    idRedirects[member.Id] = groupId;
                }
            }
        }

        // Phase 4: Connect every node to the nodes it depends on. We use the
        // redirect map from above so that a dependency on a merged member
        // identifier correctly resolves to the batch node that now contains it.
        foreach (var (node, dependencies, branches, fallback) in allNodes)
        {
            if (dependencies is not null)
            {
                foreach (var rawDepId in dependencies)
                {
                    var dependencyId = idRedirects.TryGetValue(rawDepId, out var redirectId)
                        ? redirectId
                        : rawDepId;

                    if (nodeMap.TryGetValue(dependencyId, out var dependencyNode))
                    {
                        // A batch node that holds more than one operation can still
                        // run even if some of its dependencies are skipped, because
                        // each operation inside the batch tracks its own fine-grained
                        // dependencies. We mark these as optional so the executor
                        // does not block the entire batch when only one member's
                        // dependency is missing. Single-operation nodes (and
                        // non-batch nodes) need a strict dependency instead.
                        if (node is OperationBatchExecutionNode { Operations.Length: > 1 })
                        {
                            node.AddOptionalDependency(dependencyNode);
                        }
                        else
                        {
                            node.AddDependency(dependencyNode);
                        }

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
                    foreach (var (typeName, rawNodeId) in branches)
                    {
                        var nodeId = idRedirects.TryGetValue(rawNodeId, out var rId) ? rId : rawNodeId;

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

        // Build a unified lookup that maps every plan-level identifier to its
        // node. This includes execution nodes *and* the individual operation
        // definitions inside batch nodes. We need both because a member
        // operation's dependency list uses the original identifiers, which may
        // point to another operation definition rather than a top-level node.
        var planNodeMap = new Dictionary<int, IOperationPlanNode>(nodeMap.Count);

        foreach (var (id, node) in nodeMap)
        {
            planNodeMap[id] = node;

            if (node is OperationBatchExecutionNode bn)
            {
                foreach (var op in bn.Operations)
                {
                    planNodeMap[op.Id] = op;
                }
            }
        }

        // Each operation definition inside a batch node tracks its own
        // dependencies so the executor can skip individual operations whose
        // prerequisites were not met. Here we resolve those per-operation
        // dependencies using the original identifiers from the JSON.
        foreach (var (groupId, groupMembers) in batchGroups)
        {
            if (nodeMap.TryGetValue(groupId, out var batchNode) && batchNode is OperationBatchExecutionNode batch)
            {
                var memberIndex = 0;

                foreach (var member in groupMembers)
                {
                    if (member.Dependencies is { Length: > 0 })
                    {
                        var opDef = batch.Operations[memberIndex];

                        foreach (var depId in member.Dependencies)
                        {
                            if (planNodeMap.TryGetValue(depId, out var depNode))
                            {
                                opDef.AddDependency(depNode);
                            }
                        }
                    }

                    memberIndex++;
                }
            }
        }

        // Seal every node so its dependency and dependent lists become
        // immutable. After this point no further wiring changes are allowed.
        foreach (var (node, _, _, _) in allNodes)
        {
            node.Seal();
        }

        return [.. nodeMap.Values.OrderBy(t => t.Id)];
    }

    private static ParsedOperationNodeInfo ParseOperationNodeInfo(
        JsonElement nodeElement, int id, ISchemaDefinition schema)
    {
        var (schemaName, opSource, source, requirements, forwardedVariables,
            resultSelectionSet, dependencies, batchingGroupId, conditions,
            requiresFileUpload) = ParseCommonOperationFields(nodeElement, schema);

        SelectionPath? target = null;

        if (nodeElement.TryGetProperty("target", out var targetElement))
        {
            target = SelectionPath.Parse(targetElement.GetString()!);
        }

        return new ParsedSingleOperationNodeInfo
        {
            Id = id,
            SchemaName = schemaName,
            OperationSource = opSource,
            Source = source ?? SelectionPath.Root,
            Target = target ?? SelectionPath.Root,
            Requirements = requirements?.ToArray() ?? [],
            ForwardedVariables = forwardedVariables ?? [],
            ResultSelectionSet = ResultSelectionSet.Create(resultSelectionSet!, schema),
            Dependencies = dependencies,
            BatchingGroupId = batchingGroupId,
            Conditions = conditions,
            RequiresFileUpload = requiresFileUpload,
            Schema = schema
        };
    }

    private static ParsedOperationNodeInfo ParseOperationBatchNodeInfo(
        JsonElement nodeElement, int id, ISchemaDefinition schema)
    {
        var (schemaName, opSource, source, requirements, forwardedVariables,
            resultSelectionSet, dependencies, batchingGroupId, conditions,
            requiresFileUpload) = ParseCommonOperationFields(nodeElement, schema);

        var targets = nodeElement.TryGetProperty("targets", out var targetsElement)
            ? targetsElement.EnumerateArray().Select(e => SelectionPath.Parse(e.GetString()!)).ToArray()
            : [];

        return new ParsedBatchOperationNodeInfo
        {
            Id = id,
            SchemaName = schemaName,
            OperationSource = opSource,
            Source = source ?? SelectionPath.Root,
            Targets = targets,
            Requirements = requirements?.ToArray() ?? [],
            ForwardedVariables = forwardedVariables ?? [],
            ResultSelectionSet = ResultSelectionSet.Create(resultSelectionSet!, schema),
            Dependencies = dependencies,
            BatchingGroupId = batchingGroupId,
            Conditions = conditions,
            RequiresFileUpload = requiresFileUpload,
            Schema = schema
        };
    }

    private static (string? schemaName, OperationSourceText opSource, SelectionPath? source,
        List<OperationRequirement>? requirements, string[]? forwardedVariables,
        SelectionSetNode? resultSelectionSet, int[]? dependencies, int? batchingGroupId,
        ExecutionNodeCondition[] conditions, bool requiresFileUpload)
        ParseCommonOperationFields(JsonElement nodeElement, ISchemaDefinition _)
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
        var opSource = new OperationSourceText(operationName, operationType, document, hash);

        SelectionPath? source = null;
        List<OperationRequirement>? requirements = null;
        string[]? forwardedVariables = null;
        SelectionSetNode? resultSelectionSet = null;
        int[]? dependencies = null;
        int? batchingGroupId = null;

        if (nodeElement.TryGetProperty("source", out var sourceElement))
        {
            source = SelectionPath.Parse(sourceElement.GetString()!);
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

        if (nodeElement.TryGetProperty("resultSelectionSet", out var resultSelectionSetElement)
            && resultSelectionSetElement.GetString() is { Length: > 0 } resultSelectionSetSyntax)
        {
            resultSelectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet(resultSelectionSetSyntax);
        }

        if (resultSelectionSet is null)
        {
            throw new InvalidOperationException("The resultSelectionSet is required in a valid operation plan.");
        }

        if (nodeElement.TryGetProperty("dependencies", out var dependenciesElement))
        {
            dependencies = dependenciesElement
                .EnumerateArray()
                .Select(e => e.GetInt32())
                .ToArray();
        }

        if (nodeElement.TryGetProperty("batchingGroupId", out var batchingGroupIdElement))
        {
            batchingGroupId = batchingGroupIdElement.GetInt32();
        }

        var conditions = TryParseConditions(nodeElement);

        var requiresFileUpload = nodeElement.TryGetProperty("requiresFileUpload", out var requiresFileUploadElement)
            && requiresFileUploadElement.ValueKind == JsonValueKind.True;

        return (schemaName, opSource, source, requirements, forwardedVariables,
            resultSelectionSet, dependencies, batchingGroupId, conditions, requiresFileUpload);
    }

    private static ParsedNodeInfo ParseIntrospectionNodeInfo(
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

        return new ParsedIntrospectionNodeInfo
        {
            Id = id,
            Selections = selections.ToArray(),
            Conditions = conditions
        };

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

    private static ParsedNodeInfo ParseNodeFieldNodeInfo(
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

        return new ParsedNodeFieldNodeInfo
        {
            Id = id,
            ResponseName = responseName,
            IdValue = idValue,
            Conditions = conditions,
            Branches = branches,
            FallbackNodeId = fallbackNodeId
        };
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

    // The classes below are lightweight intermediate representations used only
    // during parsing. They hold the raw values extracted from JSON so we can
    // first group and redirect identifiers before creating the final execution
    // nodes and wiring their dependencies.

    private abstract class ParsedNodeInfo
    {
        public int Id { get; init; }
        public int[]? Dependencies { get; init; }

        public abstract (ExecutionNode Node, int[]? Dependencies, Dictionary<string, int>? Branches, int? Fallback)
            ToExecutionNodeTuple();
    }

    private abstract class ParsedOperationNodeInfo : ParsedNodeInfo
    {
        public string? SchemaName { get; init; }
        public required OperationSourceText OperationSource { get; init; }
        public required SelectionPath Source { get; init; }
        public OperationRequirement[] Requirements { get; init; } = [];
        public string[] ForwardedVariables { get; init; } = [];
        public required ResultSelectionSet ResultSelectionSet { get; init; }
        public int? BatchingGroupId { get; init; }
        public ExecutionNodeCondition[] Conditions { get; init; } = [];
        public bool RequiresFileUpload { get; init; }
        public required ISchemaDefinition Schema { get; init; }

        public abstract OperationDefinition ToOperationDefinition();
    }

    private sealed class ParsedSingleOperationNodeInfo : ParsedOperationNodeInfo
    {
        public required SelectionPath Target { get; init; }

        public override OperationDefinition ToOperationDefinition()
        {
            return new SingleOperationDefinition(
                Id,
                OperationSource,
                SchemaName,
                Target,
                Source,
                Requirements,
                ForwardedVariables,
                ResultSelectionSet,
                Conditions,
                RequiresFileUpload);
        }

        public override (ExecutionNode, int[]?, Dictionary<string, int>?, int?) ToExecutionNodeTuple()
        {
            var node = new OperationExecutionNode(
                Id,
                OperationSource,
                SchemaName,
                Target,
                Source,
                Requirements,
                ForwardedVariables,
                ResultSelectionSet,
                Conditions,
                RequiresFileUpload);

            return (node, Dependencies, null, null);
        }
    }

    private sealed class ParsedBatchOperationNodeInfo : ParsedOperationNodeInfo
    {
        public SelectionPath[] Targets { get; init; } = [];

        public override OperationDefinition ToOperationDefinition()
        {
            return new BatchOperationDefinition(
                Id,
                OperationSource,
                SchemaName,
                Targets,
                Source,
                Requirements,
                ForwardedVariables,
                ResultSelectionSet,
                Conditions,
                RequiresFileUpload);
        }

        public override (ExecutionNode, int[]?, Dictionary<string, int>?, int?) ToExecutionNodeTuple()
        {
            // This batch operation does not share a batching group with any other
            // operation, so it stands alone. We still wrap it in an
            // OperationBatchExecutionNode because the executor expects batch
            // operations to run through the batch execution path.
            var opDef = ToOperationDefinition();
            var batchNode = new OperationBatchExecutionNode(Id, [opDef]);

            return (batchNode, Dependencies, null, null);
        }
    }

    private sealed class ParsedIntrospectionNodeInfo : ParsedNodeInfo
    {
        public Selection[] Selections { get; init; } = [];
        public ExecutionNodeCondition[] Conditions { get; init; } = [];

        public override (ExecutionNode, int[]?, Dictionary<string, int>?, int?) ToExecutionNodeTuple()
        {
            var node = new IntrospectionExecutionNode(Id, Selections, Conditions);

            return (node, Dependencies, null, null);
        }
    }

    private sealed class ParsedNodeFieldNodeInfo : ParsedNodeInfo
    {
        public string ResponseName { get; init; } = "";
        public IValueNode IdValue { get; init; } = null!;
        public ExecutionNodeCondition[] Conditions { get; init; } = [];
        public Dictionary<string, int>? Branches { get; init; }
        public int FallbackNodeId { get; init; }

        public override (ExecutionNode, int[]?, Dictionary<string, int>?, int?) ToExecutionNodeTuple()
        {
            var node = new NodeFieldExecutionNode(Id, ResponseName, IdValue, Conditions);

            return (node, Dependencies, Branches, FallbackNodeId);
        }
    }
}
