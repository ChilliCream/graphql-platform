using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Fusion.Language;
using HotChocolate.Fusion.Types;
using HotChocolate.Fusion.Types.Directives;
using HotChocolate.Language;
using HotChocolate.Types;
using StringValueNode = HotChocolate.Language.StringValueNode;
using IValueNode = HotChocolate.Language.IValueNode;

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
        if (!rootElement.TryGetProperty("includeConditions", out var includeConditionsElement))
        {
            throw ThrowHelper.InvalidOperationPlan(
                "The operation-wide include-condition table is required.");
        }

        var includeConditions = ParseIncludeConditions(includeConditionsElement);
        var compiledIncludeConditions = IncludeConditionCollection.Create(includeConditions);
        var operation = ParseOperation(
            rootElement.GetProperty("operation"),
            compiledIncludeConditions);

        if (rootElement.TryGetProperty("searchSpace", out var searchSpaceElement))
        {
            searchSpace = searchSpaceElement.GetInt32();
        }

        if (rootElement.TryGetProperty("expandedNodes", out var expandedNodesElement))
        {
            expandedNodes = expandedNodesElement.GetInt32();
        }

        var nodes = ParseNodes(rootElement.GetProperty("nodes"), operation);

        var deliveryGroups = ImmutableArray<DeliveryGroup>.Empty;
        var incrementalPlans = ImmutableArray<IncrementalPlan>.Empty;
        var deliveryGroupMap = new Dictionary<int, DeliveryGroup>();

        if (rootElement.TryGetProperty("deliveryGroups", out var deliveryGroupsElement))
        {
            deliveryGroups = ParseDeliveryGroups(deliveryGroupsElement, deliveryGroupMap);
        }

        if (rootElement.TryGetProperty("incrementalPlans", out var incrementalPlansElement))
        {
            incrementalPlans = ParseIncrementalPlans(
                incrementalPlansElement,
                deliveryGroupMap,
                compiledIncludeConditions);
        }

        var policyExpressions = ImmutableArray<PolicyConditionExpression>.Empty;
        if (rootElement.TryGetProperty("policyExpressions", out var policyExpressionsElement))
        {
            policyExpressions = ParsePolicyExpressions(policyExpressionsElement);
        }

        var policySlots = ImmutableArray<PolicyConditionSlot>.Empty;

        if (rootElement.TryGetProperty("policySlots", out var policySlotsElement))
        {
            policySlots = ParsePolicySlots(policySlotsElement);
        }

        var policies = ImmutableArray<PolicyPlanEntry>.Empty;
        if (rootElement.TryGetProperty("policies", out var policiesElement))
        {
            policies = ParsePolicies(policiesElement);
        }

        // Root nodes are the entry points of the execution plan. A node is a
        // root when it has no dependencies at all, meaning the executor can
        // start it immediately without waiting for other nodes to finish.
        return OperationPlan.CreateParsed(
            id,
            operation,
            [.. nodes.Where(n => n.Dependencies.Length == 0 && n.OptionalDependencies.Length == 0)],
            nodes,
            deliveryGroups,
            incrementalPlans,
            includeConditions,
            policyExpressions,
            policySlots,
            policies,
            searchSpace,
            expandedNodes);
    }

    private static ImmutableArray<OperationIncludeCondition> ParseIncludeConditions(
        JsonElement includeConditionsElement)
    {
        RequireArray(includeConditionsElement, "includeConditions");
        var builder = ImmutableArray.CreateBuilder<OperationIncludeCondition>();

        foreach (var conditionElement in includeConditionsElement.EnumerateArray())
        {
            ValidateProperties(
                conditionElement,
                ["skipVariable", "includeVariable"],
                [],
                "include condition");
            var skipVariable = conditionElement.TryGetProperty("skipVariable", out var skipElement)
                ? skipElement.GetString()
                : null;
            var includeVariable = conditionElement.TryGetProperty("includeVariable", out var includeElement)
                ? includeElement.GetString()
                : null;

            if ((skipVariable is null && includeVariable is null)
                || string.IsNullOrWhiteSpace(skipVariable) && skipVariable is not null
                || string.IsNullOrWhiteSpace(includeVariable) && includeVariable is not null)
            {
                throw ThrowHelper.InvalidOperationPlan("A serialized include condition is malformed.");
            }

            builder.Add(new OperationIncludeCondition
            {
                SkipVariable = skipVariable,
                IncludeVariable = includeVariable
            });
        }

        var conditions = builder.ToImmutable();
        if (conditions.Length > 64
            || conditions.Distinct().Count() != conditions.Length)
        {
            throw ThrowHelper.InvalidOperationPlan(
                "The operation-wide include-condition table must be unique and contain at most 64 entries.");
        }

        return conditions;
    }

    private static ImmutableArray<PolicyConditionExpression> ParsePolicyExpressions(
        JsonElement policyExpressionsElement)
    {
        RequireArray(policyExpressionsElement, "policyExpressions");
        var builder = ImmutableArray.CreateBuilder<PolicyConditionExpression>();

        foreach (var expressionElement in policyExpressionsElement.EnumerateArray())
        {
            ValidateProperties(
                expressionElement,
                ["ordinal", "names", "expression"],
                ["ordinal", "names", "expression"],
                "policy expression");

            var groups = ParsePolicyNameGroups(expressionElement.GetProperty("names"));
            var expression = new PolicyConditionExpression
            {
                Ordinal = expressionElement.GetProperty("ordinal").GetInt32(),
                Groups = groups,
                Text = PolicyNameGroups.Format(groups)
            };

            if (!string.Equals(
                expressionElement.GetProperty("expression").GetString(),
                expression.Format(),
                StringComparison.Ordinal))
            {
                throw ThrowHelper.InvalidOperationPlan("A serialized policy expression does not match its policy groups.");
            }

            builder.Add(expression);
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<PolicyConditionSlot> ParsePolicySlots(JsonElement policySlotsElement)
    {
        RequireArray(policySlotsElement, "policySlots");
        var builder = ImmutableArray.CreateBuilder<PolicyConditionSlot>();

        foreach (var slotElement in policySlotsElement.EnumerateArray())
        {
            ValidateProperties(
                slotElement,
                ["ordinal", "variable", "applications", "rmax", "guardMasks", "coordinates"],
                ["ordinal", "variable", "applications", "rmax", "guardMasks", "coordinates"],
                "policy gate");
            var ordinal = slotElement.GetProperty("ordinal").GetInt32();
            if (!string.Equals(
                slotElement.GetProperty("variable").GetString(),
                $"$__fusion_policy_{ordinal}",
                StringComparison.Ordinal))
            {
                throw ThrowHelper.InvalidOperationPlan("A serialized policy gate variable does not match its ordinal.");
            }
            var applications = ImmutableArray.CreateBuilder<PolicyConditionApplication>();
            var applicationsElement = slotElement.GetProperty("applications");
            RequireArray(applicationsElement, "policy gate applications");

            foreach (var applicationElement in applicationsElement.EnumerateArray())
            {
                ValidateProperties(
                    applicationElement,
                    ["expressionOrdinal", "onDenied"],
                    ["expressionOrdinal", "onDenied"],
                    "policy gate application");

                applications.Add(new PolicyConditionApplication
                {
                    ExpressionOrdinal = applicationElement.GetProperty("expressionOrdinal").GetInt32(),
                    OnDenied = ParseDefinedEnum<PolicyDenialBehavior>(
                        applicationElement.GetProperty("onDenied"),
                        "policy gate application denial behavior")
                });
            }

            var masksElement = slotElement.GetProperty("guardMasks");
            RequireArray(masksElement, "policy gate guardMasks");
            var masks = masksElement.EnumerateArray().Select(element => element.GetUInt64()).ToImmutableArray();
            var rmax = ParseDefinedEnum<PolicyDenialBehavior>(
                slotElement.GetProperty("rmax"),
                "policy gate residual denial behavior");
            var coordinatesElement = slotElement.GetProperty("coordinates");
            RequireArray(coordinatesElement, "policy gate coordinates");
            var coordinateBuilder = ImmutableArray.CreateBuilder<PolicyConditionCoordinate>();

            foreach (var coordinateElement in coordinatesElement.EnumerateArray())
            {
                ValidateProperties(
                    coordinateElement,
                    ["occurrences", "typeName", "fieldName", "responseNames", "applications", "isRoot", "liveGuardMasks", "gateGuardMasks"],
                    ["occurrences", "typeName", "responseNames", "applications", "isRoot", "liveGuardMasks", "gateGuardMasks"],
                    "policy gate coordinate");
                var occurrencesElement = coordinateElement.GetProperty("occurrences");
                RequireArray(occurrencesElement, "policy gate coordinate occurrences");
                var liveMasksElement = coordinateElement.GetProperty("liveGuardMasks");
                RequireArray(liveMasksElement, "policy gate coordinate liveGuardMasks");
                var gateMasksElement = coordinateElement.GetProperty("gateGuardMasks");
                RequireArray(gateMasksElement, "policy gate coordinate gateGuardMasks");
                var responseNamesElement = coordinateElement.GetProperty("responseNames");
                RequireArray(responseNamesElement, "policy gate coordinate responseNames");
                var coordinateApplicationsElement = coordinateElement.GetProperty("applications");
                RequireArray(coordinateApplicationsElement, "policy gate coordinate applications");
                var coordinateApplications = ImmutableArray.CreateBuilder<PolicyConditionApplication>();

                foreach (var applicationElement in coordinateApplicationsElement.EnumerateArray())
                {
                    ValidateProperties(
                        applicationElement,
                        ["expressionOrdinal", "onDenied"],
                        ["expressionOrdinal", "onDenied"],
                        "policy gate coordinate application");
                    coordinateApplications.Add(new PolicyConditionApplication
                    {
                        ExpressionOrdinal = applicationElement.GetProperty("expressionOrdinal").GetInt32(),
                        OnDenied = ParseDefinedEnum<PolicyDenialBehavior>(
                            applicationElement.GetProperty("onDenied"),
                            "policy gate coordinate application denial behavior")
                    });
                }

                coordinateBuilder.Add(new PolicyConditionCoordinate
                {
                    Occurrences = occurrencesElement
                        .EnumerateArray()
                        .Select(ParsePolicyOccurrence)
                        .ToImmutableArray(),
                    TypeName = coordinateElement.GetProperty("typeName").GetString()!,
                    FieldName = coordinateElement.TryGetProperty("fieldName", out var fieldNameElement)
                        ? fieldNameElement.GetString()
                        : null,
                    ResponseNames = responseNamesElement
                        .EnumerateArray()
                        .Select(element => element.GetString()!)
                        .ToImmutableArray(),
                    Applications = coordinateApplications.ToImmutable(),
                    IsRoot = coordinateElement.GetProperty("isRoot").GetBoolean(),
                    LiveGuardMasks = liveMasksElement
                        .EnumerateArray()
                        .Select(element => element.GetUInt64())
                        .ToImmutableArray(),
                    GateGuardMasks = gateMasksElement
                        .EnumerateArray()
                        .Select(element => element.GetUInt64())
                        .ToImmutableArray()
                });
            }

            var coordinates = coordinateBuilder.ToImmutable();

            builder.Add(new PolicyConditionSlot
            {
                Ordinal = ordinal,
                Applications = applications.ToImmutable(),
                Rmax = rmax,
                GuardMasks = masks,
                Coordinates = coordinates
            });
        }

        return builder.ToImmutable();
    }

    private static ImmutableArray<PolicyPlanEntry> ParsePolicies(JsonElement policiesElement)
    {
        RequireArray(policiesElement, "policies");
        var builder = ImmutableArray.CreateBuilder<PolicyPlanEntry>();

        foreach (var policyElement in policiesElement.EnumerateArray())
        {
            ValidateProperties(
                policyElement,
                ["name", "requirementHash"],
                ["name", "requirementHash"],
                "policy inventory entry");
            builder.Add(new PolicyPlanEntry
            {
                PolicyName = policyElement.GetProperty("name").GetString()!,
                RequirementHash = policyElement.GetProperty("requirementHash").GetUInt64()
            });
        }

        return builder.ToImmutable();
    }

    private static T ParseDefinedEnum<T>(JsonElement element, string description)
        where T : struct, Enum
    {
        if (element.ValueKind is not JsonValueKind.String
            || !Enum.TryParse<T>(element.GetString(), ignoreCase: true, out var value)
            || !Enum.IsDefined(value))
        {
            throw ThrowHelper.InvalidOperationPlan($"The {description} is invalid.");
        }

        return value;
    }

    private static void RequireArray(JsonElement element, string description)
    {
        if (element.ValueKind is not JsonValueKind.Array)
        {
            throw ThrowHelper.InvalidOperationPlan($"The {description} must be an array.");
        }
    }

    private static void ValidateProperties(
        JsonElement element,
        string[] allowed,
        string[] required,
        string description)
    {
        if (element.ValueKind is not JsonValueKind.Object)
        {
            throw ThrowHelper.InvalidOperationPlan($"The {description} must be an object.");
        }

        foreach (var property in element.EnumerateObject())
        {
            if (!allowed.Contains(property.Name, StringComparer.Ordinal))
            {
                throw ThrowHelper.InvalidOperationPlan(
                    $"The {description} contains the unexpected property '{property.Name}'.");
            }
        }

        foreach (var propertyName in required)
        {
            if (!element.TryGetProperty(propertyName, out _))
            {
                throw ThrowHelper.InvalidOperationPlan(
                    $"The {description} is missing the required property '{propertyName}'.");
            }
        }
    }

    private static ImmutableArray<DeliveryGroup> ParseDeliveryGroups(
        JsonElement deliveryGroupsElement,
        Dictionary<int, DeliveryGroup> deliveryGroupMap)
    {
        RequireArray(deliveryGroupsElement, "deliveryGroups");

        // Phase 1: Construct every DeliveryGroup without resolving parent references.
        // Parents are captured as numeric ids for the second pass because a parent
        // may appear after its child in the serialized array.
        var ordered = new List<(DeliveryGroup Usage, int? ParentId)>();

        foreach (var groupElement in deliveryGroupsElement.EnumerateArray())
        {
            var deferId = groupElement.GetProperty("id").GetInt32();
            var path = SelectionPath.Parse(groupElement.GetProperty("path").GetString()!);

            string? label = null;
            if (groupElement.TryGetProperty("label", out var labelElement))
            {
                label = labelElement.GetString();
            }

            string? ifVariable = null;
            if (groupElement.TryGetProperty("ifVariable", out var ifVarElement)
                && ifVarElement.ValueKind == JsonValueKind.String)
            {
                ifVariable = ifVarElement.GetString()!.TrimStart('$');
            }

            int? parentId = null;
            if (groupElement.TryGetProperty("parentId", out var parentIdElement)
                && parentIdElement.ValueKind == JsonValueKind.Number)
            {
                parentId = parentIdElement.GetInt32();
            }

            var deliveryGroup = new DeliveryGroup(label, Parent: null, DeferConditionIndex: 0)
            {
                Id = deferId,
                Path = path,
                IfVariable = ifVariable
            };

            if (!deliveryGroupMap.TryAdd(deferId, deliveryGroup))
            {
                throw ThrowHelper.InvalidOperationPlan(
                    "An operation plan cannot contain duplicate delivery group identifiers.");
            }

            ordered.Add((deliveryGroup, parentId));
        }

        // Phase 2: Resolve every parent id against the raw group table and rebuild
        // the records so their Parent references point at canonical instances. The
        // recursive resolution keeps a child canonical even when its parent appears
        // later in the serialized array.
        var entriesById = ordered.ToDictionary(entry => entry.Usage.Id);
        var resolvedById = new Dictionary<int, DeliveryGroup>();
        var resolvingIds = new HashSet<int>();

        DeliveryGroup Resolve(int id)
        {
            if (resolvedById.TryGetValue(id, out var resolved))
            {
                return resolved;
            }

            if (!entriesById.TryGetValue(id, out var entry)
                || !resolvingIds.Add(id))
            {
                throw ThrowHelper.InvalidOperationPlan(
                    "Delivery group parent references must form an acyclic immediate-parent topology.");
            }

            try
            {
                if (entry.ParentId is null)
                {
                    resolved = entry.Usage;
                }
                else
                {
                    if (entry.ParentId.Value == entry.Usage.Id
                        || !entriesById.ContainsKey(entry.ParentId.Value))
                    {
                        throw ThrowHelper.InvalidOperationPlan(
                            "A non-root delivery group must reference an existing immediate parent delivery group.");
                    }

                    resolved = entry.Usage with { Parent = Resolve(entry.ParentId.Value) };
                }

                resolvedById.Add(id, resolved);
                return resolved;
            }
            finally
            {
                resolvingIds.Remove(id);
            }
        }

        // Update the map so incremental plans and the returned collection share
        // the same DeliveryGroup instances.
        var builder = ImmutableArray.CreateBuilder<DeliveryGroup>(ordered.Count);

        foreach (var (usage, _) in ordered)
        {
            var resolved = Resolve(usage.Id);
            deliveryGroupMap[usage.Id] = resolved;
            builder.Add(resolved);
        }

        return builder.MoveToImmutable();
    }

    private ImmutableArray<IncrementalPlan> ParseIncrementalPlans(
        JsonElement incrementalPlansElement,
        Dictionary<int, DeliveryGroup> deliveryGroupMap,
        IncludeConditionCollection includeConditions)
    {
        var builder = ImmutableArray.CreateBuilder<IncrementalPlan>();

        foreach (var incrementalPlanElement in incrementalPlansElement.EnumerateArray())
        {
            var deliveryGroupIdsElement = incrementalPlanElement.GetProperty("deliveryGroupIds");
            var incrementalPlanDeliveryGroupsBuilder = ImmutableArray.CreateBuilder<DeliveryGroup>();

            foreach (var idElement in deliveryGroupIdsElement.EnumerateArray())
            {
                incrementalPlanDeliveryGroupsBuilder.Add(deliveryGroupMap[idElement.GetInt32()]);
            }

            var incrementalPlanOperation = ParseOperation(
                incrementalPlanElement.GetProperty("operation"),
                includeConditions);

            var incrementalPlanNodes = incrementalPlanElement.TryGetProperty("nodes", out var incrementalPlanNodesElement)
                ? ParseNodes(incrementalPlanNodesElement, incrementalPlanOperation)
                : [];

            var rootIncrementalPlanNodes = incrementalPlanNodes
                .Where(n => n.Dependencies.Length == 0 && n.OptionalDependencies.Length == 0)
                .ToImmutableArray();

            var incrementalPlanRequirements = ImmutableArray<OperationRequirement>.Empty;

            if (incrementalPlanElement.TryGetProperty("requirements", out var requirementsElement))
            {
                var requirementsBuilder = ImmutableArray.CreateBuilder<OperationRequirement>();

                foreach (var requirementElement in requirementsElement.EnumerateArray())
                {
                    var requirementName = requirementElement.GetProperty("name").GetString()!;
                    var requirementType = requirementElement.GetProperty("type").GetString()!;
                    var requirementPath = requirementElement.GetProperty("path").GetString()!;
                    var internalAlias =
                        requirementElement.TryGetProperty("internalAlias", out var internalAliasElement)
                            ? internalAliasElement.GetString()
                            : null;
                    var selectionMap = requirementElement.GetProperty("selectionMap").GetString()!;
                    var requirementTypeNode = Utf8GraphQLParser.Syntax.ParseTypeReference(requirementType);

                    requirementsBuilder.Add(new OperationRequirement(
                        requirementName,
                        requirementTypeNode,
                        SelectionPath.Parse(requirementPath),
                        FieldSelectionMapParser.Parse(selectionMap),
                        internalAlias));
                }

                incrementalPlanRequirements = requirementsBuilder.ToImmutable();
            }

            var incrementalPlan = new IncrementalPlan(
                incrementalPlanOperation,
                rootIncrementalPlanNodes,
                incrementalPlanNodes,
                incrementalPlanDeliveryGroupsBuilder.ToImmutable(),
                incrementalPlanRequirements)
            {
                ParentNodeId = incrementalPlanElement.GetProperty("parentNodeId").GetInt32()
            };

            builder.Add(incrementalPlan);
        }

        return builder.ToImmutable();
    }

    private Operation ParseOperation(
        JsonElement operationElement,
        IncludeConditionCollection includeConditions)
    {
        var sourceText = operationElement.GetProperty("document").GetString()!;
        var id = operationElement.GetProperty("id").GetString()!;
        var hash = operationElement.GetProperty("hash").GetString()!;

        if (!operationElement.TryGetProperty("shortHash", out var shortHashElement))
        {
            throw ThrowHelper.InvalidOperationPlan(
                "The shortHash is required on the operation of a valid operation plan.");
        }

        var shortHash = shortHashElement.GetString()!;

        var document = Utf8GraphQLParser.Parse(sourceText, ParserOptions.Trusted);
        var operationDefinition = document.Definitions.OfType<OperationDefinitionNode>().SingleOrDefault();

        if (operationDefinition is null)
        {
            throw ThrowHelper.SingleOperationRequired();
        }

        return _operationCompiler.Compile(
            id,
            hash,
            shortHash,
            operationDefinition,
            includeConditions);
    }

    private ImmutableArray<ExecutionNode> ParseNodes(JsonElement nodesElement, Operation operation)
    {
        // Phase 1: Read every JSON node element into a lightweight intermediate
        // object. We do not create real execution nodes yet because we first need
        // to know which operations belong to the same batch group.
        var parsedNodes = new List<ParsedNodeInfo>();
        var rawNodeIndexes = new Dictionary<int, int>();

        foreach (var nodeElement in nodesElement.EnumerateArray())
        {
            var nodeType = nodeElement.GetProperty("type").GetString();
            var id = nodeElement.GetProperty("id").GetInt32();
            if (!rawNodeIndexes.TryAdd(id, parsedNodes.Count))
            {
                throw ThrowHelper.InvalidOperationPlan(
                    "An operation plan cannot contain duplicate node identifiers.");
            }

            ValidateRawDependencies(nodeElement, id);

            if (nodeType is "Policy")
            {
                ValidateRawPolicyDependencyOrder(nodeElement, id);
            }

            var schema = _operationCompiler.Schema;

            switch (nodeType)
            {
                case "Operation":
                    parsedNodes.Add(ParseOperationNodeInfo(nodeElement, id, schema));
                    break;

                case "OperationBatch":
                    parsedNodes.Add(ParseOperationBatchNodeInfo(nodeElement, id, schema));
                    break;

                case "ApolloOperation":
                case "ApolloOperationBatch":
                    parsedNodes.Add(ParseApolloOperationNodeInfo(nodeElement, id, schema));
                    break;

                case "EventStream":
                    parsedNodes.Add(ParseEventStreamNodeInfo(nodeElement, id, schema));
                    break;

                case "Introspection":
                    parsedNodes.Add(ParseIntrospectionNodeInfo(nodeElement, id, operation));
                    break;

                case "Node":
                    parsedNodes.Add(ParseNodeFieldNodeInfo(nodeElement, id, operation));
                    break;

                case "Policy":
                    parsedNodes.Add(ParsePolicyNodeInfo(nodeElement, id));
                    break;

                default:
                    throw new NotSupportedException($"Unsupported node type: {nodeType}");
            }
        }

        ValidateRawPolicyTopology(parsedNodes, rawNodeIndexes);

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

        // Merge each batch group into a single batch execution node.
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

            // Apollo entity lookups group into their own batch node type because
            // each member operation is sent as its own _entities request.
            ExecutionNode batchNode;

            if (groupMembers[0] is ParsedApolloOperationNodeInfo)
            {
                var operationDefinitions = operations.Cast<SingleOperationDefinition>().ToArray();
                var lookups = new ApolloEntityLookup[groupMembers.Count];

                for (var i = 0; i < groupMembers.Count; i++)
                {
                    lookups[i] = ((ParsedApolloOperationNodeInfo)groupMembers[i]).CreateLookup();
                }

                batchNode = ApolloOperationBatchExecutionNode.CreateFromParser(
                    groupId,
                    operationDefinitions,
                    lookups,
                    _operationCompiler.Schema);
            }
            else
            {
                batchNode = new OperationBatchExecutionNode(groupId, operations.ToArray());
            }

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
                // Multiple member identifiers can redirect to the same batch
                // node, so dependencies are deduplicated after redirection.
                var seenDependencyIds = new HashSet<int>();

                foreach (var rawDepId in dependencies)
                {
                    var dependencyId = idRedirects.TryGetValue(rawDepId, out var redirectId)
                        ? redirectId
                        : rawDepId;

                    if (!seenDependencyIds.Add(dependencyId))
                    {
                        continue;
                    }

                    if (nodeMap.TryGetValue(dependencyId, out var dependencyNode))
                    {
                        // Operations inside a batch track their own dependencies,
                        // so batch nodes with multiple operations or a single
                        // merged multi-target operation take optional dependencies.
                        // Single-target operation nodes require strict dependencies.
                        if (node is OperationBatchExecutionNode batchNode
                            && (batchNode.Operations.Length > 1
                                || batchNode.Operations[0] is BatchOperationDefinition))
                        {
                            node.AddOptionalDependency(dependencyNode);
                        }
                        else if (node is ApolloOperationBatchExecutionNode { Operations.Length: > 1 })
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

            if (node is ApolloOperationBatchExecutionNode abn)
            {
                foreach (var op in abn.Operations)
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
            if (!nodeMap.TryGetValue(groupId, out var groupNode))
            {
                continue;
            }

            var memberIndex = 0;

            foreach (var member in groupMembers)
            {
                if (member.Dependencies is { Length: > 0 })
                {
                    var opDef = groupNode switch
                    {
                        OperationBatchExecutionNode batch => batch.Operations[memberIndex],
                        ApolloOperationBatchExecutionNode apolloBatch => apolloBatch.Operations[memberIndex],
                        _ => null
                    };

                    if (opDef is not null)
                    {
                        foreach (var depId in member.Dependencies)
                        {
                            if (planNodeMap.TryGetValue(depId, out var depNode))
                            {
                                opDef.AddDependency(depNode);
                            }
                        }
                    }
                }

                memberIndex++;
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

    private static void ValidateRawDependencies(
        JsonElement nodeElement,
        int nodeId)
    {
        if (!nodeElement.TryGetProperty("dependencies", out var dependenciesElement))
        {
            return;
        }

        RequireArray(dependenciesElement, "dependencies");
        var dependencies = new HashSet<int>();
        var parentDependencies = new HashSet<int>();
        var previousDependencyId = -1;
        foreach (var dependency in dependenciesElement.EnumerateArray())
        {
            switch (dependency.ValueKind)
            {
                case JsonValueKind.Number:
                    var dependencyId = dependency.GetInt32();
                    if (!dependencies.Add(dependencyId))
                    {
                        throw ThrowHelper.InvalidOperationPlan(
                            $"Node {nodeId} contains a duplicate dependency identifier.");
                    }

                    if (dependencyId <= previousDependencyId)
                    {
                        throw ThrowHelper.InvalidOperationPlan(
                            $"Node {nodeId} dependencies must be in canonical order.");
                    }

                    previousDependencyId = dependencyId;
                    break;

                case JsonValueKind.Object:
                    var parentNodeId = dependency.GetProperty("parentNodeId").GetInt32();
                    if (!parentDependencies.Add(parentNodeId))
                    {
                        throw ThrowHelper.InvalidOperationPlan(
                            $"Node {nodeId} contains a duplicate parent dependency identifier.");
                    }
                    break;
            }
        }
    }

    private static void ValidateRawPolicyDependencyOrder(JsonElement nodeElement, int nodeId)
    {
        if (!nodeElement.TryGetProperty("dependencies", out var dependenciesElement))
        {
            return;
        }

        var previousDependencyId = -1;
        var previousParentDependencyId = -1;
        var hasParentDependencies = false;
        foreach (var dependency in dependenciesElement.EnumerateArray())
        {
            switch (dependency.ValueKind)
            {
                case JsonValueKind.Number:
                    if (hasParentDependencies)
                    {
                        throw ThrowHelper.InvalidOperationPlan(
                            $"Policy execution node {nodeId} numeric dependencies must precede parent dependencies.");
                    }

                    var dependencyId = dependency.GetInt32();
                    if (dependencyId <= previousDependencyId)
                    {
                        throw ThrowHelper.InvalidOperationPlan(
                            $"Policy execution node {nodeId} dependencies must be in canonical order.");
                    }

                    previousDependencyId = dependencyId;
                    break;

                case JsonValueKind.Object:
                    hasParentDependencies = true;
                    var parentDependencyId = dependency.GetProperty("parentNodeId").GetInt32();
                    if (parentDependencyId <= previousParentDependencyId)
                    {
                        throw ThrowHelper.InvalidOperationPlan(
                            $"Policy execution node {nodeId} parent dependencies must be in canonical order.");
                    }

                    previousParentDependencyId = parentDependencyId;
                    break;
            }
        }
    }

    private static void ValidateRawPolicyTopology(
        List<ParsedNodeInfo> parsedNodes,
        IReadOnlyDictionary<int, int> rawNodeIndexes)
    {
        PolicyOccurrenceReference? previousOccurrence = null;

        foreach (var policy in parsedNodes.OfType<ParsedPolicyNodeInfo>())
        {
            if (policy.Dependencies is not null)
            {
                foreach (var dependencyId in policy.Dependencies)
                {
                    if (rawNodeIndexes.TryGetValue(dependencyId, out var dependencyIndex)
                        && dependencyIndex >= rawNodeIndexes[policy.Id])
                    {
                        throw ThrowHelper.InvalidOperationPlan(
                            "A policy execution node must follow its guarded producer and requirement providers.");
                    }
                }
            }

            var firstOccurrence = policy.Targets
                .SelectMany(target => target.Occurrences)
                .OrderBy(occurrence => occurrence.PlanPart)
                .ThenBy(occurrence => occurrence.SelectionSetId)
                .ThenBy(occurrence => occurrence.SelectionId)
                .ThenBy(occurrence => occurrence.OccurrenceOrdinal)
                .FirstOrDefault();
            if (firstOccurrence == default)
            {
                continue;
            }

            if (previousOccurrence is { } previous
                && CompareOccurrencePosition(previous, firstOccurrence) >= 0)
            {
                throw ThrowHelper.InvalidOperationPlan(
                    "Policy execution nodes must follow compiled occurrence order.");
            }

            previousOccurrence = firstOccurrence;

            if (!rawNodeIndexes.ContainsKey(policy.Id - 1))
            {
                throw ThrowHelper.InvalidOperationPlan(
                    "A policy execution node must immediately follow its canonical guarded producer.");
            }
        }
    }

    private static int CompareOccurrencePosition(
        PolicyOccurrenceReference left,
        PolicyOccurrenceReference right)
    {
        var comparison = left.PlanPart.CompareTo(right.PlanPart);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.SelectionSetId.CompareTo(right.SelectionSetId);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.SelectionId.CompareTo(right.SelectionId);
        if (comparison != 0)
        {
            return comparison;
        }

        return left.OccurrenceOrdinal.CompareTo(right.OccurrenceOrdinal);
    }

    private static ParsedOperationNodeInfo ParseOperationNodeInfo(
        JsonElement nodeElement, int id, FusionSchemaDefinition schema)
    {
        var (schemaName, opSource, lookupTypeName, source, requirements, forwardedVariables,
            resultSelectionSet, dependencies, parentDependencies, batchingGroupId,
            conditions, requiresFileUpload) = ParseCommonOperationFields(nodeElement);

        SelectionPath? target = null;

        if (nodeElement.TryGetProperty("target", out var targetElement))
        {
            target = SelectionPath.Parse(targetElement.GetString()!);
        }

        var parentType = ResolveResultSelectionSetType(schema, opSource.Type, source ?? SelectionPath.Root);

        return new ParsedSingleOperationNodeInfo
        {
            Id = id,
            SchemaName = schemaName,
            OperationSource = opSource,
            LookupTypeName = lookupTypeName,
            Source = source ?? SelectionPath.Root,
            Target = target ?? SelectionPath.Root,
            Requirements = requirements?.ToArray() ?? [],
            ForwardedVariables = forwardedVariables ?? [],
            ResultSelectionSet =
                ResultSelectionSet.CreateFromPlan(
                    resultSelectionSet!,
                    schema,
                    parentType,
                    schemaName),
            Dependencies = dependencies,
            ParentDependencies = parentDependencies,
            BatchingGroupId = batchingGroupId,
            Conditions = conditions,
            RequiresFileUpload = requiresFileUpload,
            Schema = schema
        };
    }

    private static ParsedApolloOperationNodeInfo ParseApolloOperationNodeInfo(
        JsonElement nodeElement, int id, FusionSchemaDefinition schema)
    {
        var (schemaName, opSource, _, source, requirements, forwardedVariables,
            resultSelectionSet, dependencies, parentDependencies, batchingGroupId,
            conditions, requiresFileUpload) = ParseCommonOperationFields(nodeElement);

        if (string.IsNullOrEmpty(schemaName))
        {
            throw new InvalidOperationException(
                "The schema is required on an Apollo operation of a valid operation plan.");
        }

        SelectionPath? target = null;

        if (nodeElement.TryGetProperty("target", out var targetElement))
        {
            target = SelectionPath.Parse(targetElement.GetString()!);
        }

        var entityTypeName = ParseApolloEntityType(nodeElement);
        var parentType = ResolveResultSelectionSetType(schema, opSource.Type, source ?? SelectionPath.Root);

        return new ParsedApolloOperationNodeInfo
        {
            Id = id,
            SchemaName = schemaName,
            OperationSource = opSource,
            Source = source ?? SelectionPath.Root,
            Target = target ?? SelectionPath.Root,
            Requirements = requirements?.ToArray() ?? [],
            ForwardedVariables = forwardedVariables ?? [],
            ResultSelectionSet =
                ResultSelectionSet.CreateFromPlan(
                    resultSelectionSet!,
                    schema,
                    parentType,
                    schemaName),
            Dependencies = dependencies,
            ParentDependencies = parentDependencies,
            BatchingGroupId = batchingGroupId,
            Conditions = conditions,
            RequiresFileUpload = requiresFileUpload,
            Schema = schema,
            FusionSchema = schema,
            EntityTypeName = entityTypeName
        };
    }

    private static string ParseApolloEntityType(JsonElement nodeElement)
    {
        if (!nodeElement.TryGetProperty("entityType", out var entityTypeElement)
            || entityTypeElement.GetString() is not { Length: > 0 } entityTypeName)
        {
            throw new InvalidOperationException(
                "The entityType is required on an Apollo operation of a valid operation plan.");
        }

        return entityTypeName;
    }

    private static ParsedEventStreamNodeInfo ParseEventStreamNodeInfo(
        JsonElement nodeElement, int id, ISchemaDefinition schema)
    {
        var resultSelectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet(
            nodeElement.GetProperty("resultSelectionSet").GetString()!);
        var source = nodeElement.TryGetProperty("source", out var sourceElement)
            ? SelectionPath.Parse(sourceElement.GetString()!)
            : SelectionPath.Root;
        var target = nodeElement.TryGetProperty("target", out var targetElement)
            ? SelectionPath.Parse(targetElement.GetString()!)
            : SelectionPath.Root;
        var dependencies = TryParseDependencies(nodeElement, out var parentDependencies);
        var conditions = TryParseConditions(nodeElement);
        var fieldName = nodeElement.GetProperty("fieldName").GetString()!;
        var message = nodeElement.GetProperty("eventStream").GetProperty("message").GetString()!;
        var eventStreamSource = ParseEventStreamSource(nodeElement, fieldName, message);

        return new ParsedEventStreamNodeInfo
        {
            Id = id,
            FieldName = fieldName,
            Source = source,
            Target = target,
            ResultSelectionSet = ResultSelectionSet.CreateFromPlan(resultSelectionSet, schema),
            EventStreamSource = eventStreamSource,
            Message = message,
            Dependencies = dependencies,
            ParentDependencies = parentDependencies,
            Conditions = conditions
        };
    }

    private static EventStreamSource ParseEventStreamSource(
        JsonElement nodeElement,
        string fieldName,
        string message)
    {
        var eventStreamElement = nodeElement.GetProperty("eventStream");
        var topics = ParseTopics(eventStreamElement, fieldName);
        var broker = eventStreamElement.TryGetProperty("broker", out var brokerElement)
            ? brokerElement.GetString()
            : null;
        var cursorField = eventStreamElement.TryGetProperty("cursorField", out var cursorFieldElement)
            ? cursorFieldElement.GetString()
            : null;
        var cursorArgument = eventStreamElement.TryGetProperty("cursorArgument", out var cursorArgumentElement)
            ? cursorArgumentElement.GetString()
            : null;

        return new EventStreamSource
        {
            SchemaName = eventStreamElement.GetProperty("schema").GetString()!,
            FieldName = fieldName,
            Topics = topics,
            Broker = broker,
            Message = FieldDirectiveParser.ParseSelectionSet(message),
            CursorField = cursorField,
            CursorArgument = cursorArgument
        };
    }

    private static ImmutableArray<string> ParseTopics(JsonElement eventStreamElement, string fieldName)
    {
        if (!eventStreamElement.TryGetProperty("topics", out var topicsElement))
        {
            return [fieldName];
        }

        var builder = ImmutableArray.CreateBuilder<string>();

        foreach (var topicElement in topicsElement.EnumerateArray())
        {
            if (topicElement.GetString() is { } topic)
            {
                builder.Add(topic);
            }
        }

        return builder.Count == 0
            ? [fieldName]
            : builder.ToImmutable();
    }

    private static ParsedOperationNodeInfo ParseOperationBatchNodeInfo(
        JsonElement nodeElement, int id, FusionSchemaDefinition schema)
    {
        var (schemaName, opSource, lookupTypeName, source, requirements, forwardedVariables,
            resultSelectionSet, dependencies, parentDependencies, batchingGroupId,
            conditions, requiresFileUpload) = ParseCommonOperationFields(nodeElement);

        var targets = nodeElement.TryGetProperty("targets", out var targetsElement)
            ? targetsElement.EnumerateArray().Select(e => SelectionPath.Parse(e.GetString()!)).ToArray()
            : [];

        var parentType = ResolveResultSelectionSetType(schema, opSource.Type, source ?? SelectionPath.Root);

        return new ParsedBatchOperationNodeInfo
        {
            Id = id,
            SchemaName = schemaName,
            OperationSource = opSource,
            LookupTypeName = lookupTypeName,
            Source = source ?? SelectionPath.Root,
            Targets = targets,
            Requirements = requirements?.ToArray() ?? [],
            ForwardedVariables = forwardedVariables ?? [],
            ResultSelectionSet =
                ResultSelectionSet.CreateFromPlan(
                    resultSelectionSet!,
                    schema,
                    parentType,
                    schemaName),
            Dependencies = dependencies,
            ParentDependencies = parentDependencies,
            BatchingGroupId = batchingGroupId,
            Conditions = conditions,
            RequiresFileUpload = requiresFileUpload,
            Schema = schema
        };
    }

    // Reconstructs the type that declares a fetch node's result selection set by walking the
    // source path from the operation root type, mirroring how BuildExecutionTree passes
    // operationStep.Type into ResultSelectionSet.Create. This is what lets a rehydrated plan
    // re-derive @interfaceObject opacity, so a cached plan behaves like a freshly built one.
    private static ITypeDefinition? ResolveResultSelectionSetType(
        FusionSchemaDefinition schema,
        OperationType operationType,
        SelectionPath source)
    {
        ITypeDefinition current = schema.GetOperationType(operationType);

        for (var i = 0; i < source.Length; i++)
        {
            var segment = source[i];

            switch (segment.Kind)
            {
                case SelectionPathSegmentKind.Field:
                    if (current is not IComplexTypeDefinition complexType
                        || !complexType.Fields.TryGetField(segment.Name, out var field))
                    {
                        return null;
                    }

                    current = field.Type.NamedType();
                    break;

                case SelectionPathSegmentKind.InlineFragment:
                    if (!schema.Types.TryGetType(segment.Name, out var fragmentType))
                    {
                        return null;
                    }

                    current = fragmentType;
                    break;
            }
        }

        return current;
    }

    private static (string? schemaName, OperationSourceText opSource, string? lookupTypeName,
        SelectionPath? source, List<OperationRequirement>? requirements, string[]? forwardedVariables,
        SelectionSetNode? resultSelectionSet, int[]? dependencies, int[]? parentDependencies,
        int? batchingGroupId, ExecutionNodeCondition[] conditions, bool requiresFileUpload)
        ParseCommonOperationFields(JsonElement nodeElement)
    {
        string? schemaName = null;

        if (nodeElement.TryGetProperty("schema", out var schemaElement))
        {
            schemaName = schemaElement.GetString()!;
        }

        var operationElement = nodeElement.GetProperty("operation");
        var operationName = operationElement.GetProperty("name").GetString()!;
        var operationType = ParseDefinedEnum<OperationType>(
            operationElement.GetProperty("kind"),
            "operation kind");
        // The parsed document string is transient: encode it to UTF-8 once and discard it.
        var document = operationElement.GetProperty("document").GetString()!;
        var documentBytes = Encoding.UTF8.GetBytes(document);
        var sha256 = operationElement.GetProperty("hash").GetString()!;
        var hash = OperationSourceTextHash.From(
            sha256,
            operationElement.GetProperty("xxHash").GetUInt64());

        var lookupTypeName = nodeElement.TryGetProperty("lookupTypeName", out var lookupTypeNameElement)
            ? lookupTypeNameElement.GetString()
            : null;

        SelectionPath? source = null;
        List<OperationRequirement>? requirements = null;
        string[]? forwardedVariables = null;
        SelectionSetNode? resultSelectionSet = null;
        int[]? dependencies = null;
        int[]? parentDependencies = null;
        int? batchingGroupId = null;

        if (nodeElement.TryGetProperty("source", out var sourceElement))
        {
            source = SelectionPath.Parse(sourceElement.GetString()!);
        }

        var opSource = new OperationSourceText(
            operationName,
            operationType,
            documentBytes,
            hash);

        if (nodeElement.TryGetProperty("requirements", out var requirementsElement))
        {
            requirements = [];

            foreach (var requirementElement in requirementsElement.EnumerateArray())
            {
                var requirementName = requirementElement.GetProperty("name").GetString()!;
                var requirementType = requirementElement.GetProperty("type").GetString()!;
                var requirementPath = requirementElement.GetProperty("path").GetString()!;
                var internalAlias =
                    requirementElement.TryGetProperty("internalAlias", out var internalAliasElement)
                        ? internalAliasElement.GetString()
                        : null;
                var selectionMap = requirementElement.GetProperty("selectionMap").GetString()!;
                var requirementTypeNode = Utf8GraphQLParser.Syntax.ParseTypeReference(requirementType);

                requirements.Add(new OperationRequirement(
                    requirementName,
                    requirementTypeNode,
                    SelectionPath.Parse(requirementPath),
                    FieldSelectionMapParser.Parse(selectionMap),
                    internalAlias));
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

        dependencies = TryParseDependencies(nodeElement, out parentDependencies);

        if (nodeElement.TryGetProperty("batchingGroupId", out var batchingGroupIdElement))
        {
            batchingGroupId = batchingGroupIdElement.GetInt32();
        }

        var conditions = TryParseConditions(nodeElement);

        var requiresFileUpload = nodeElement.TryGetProperty("requiresFileUpload", out var requiresFileUploadElement)
            && requiresFileUploadElement.ValueKind == JsonValueKind.True;

        return (schemaName, opSource, lookupTypeName, source, requirements, forwardedVariables,
            resultSelectionSet, dependencies, parentDependencies, batchingGroupId, conditions, requiresFileUpload);
    }

    private static int[]? TryParseDependencies(
        JsonElement nodeElement,
        out int[]? parentDependencies)
    {
        parentDependencies = null;

        if (nodeElement.TryGetProperty("dependencies", out var dependenciesElement))
        {
            List<int>? intDeps = null;
            List<int>? parentDeps = null;

            foreach (var dependencyElement in dependenciesElement.EnumerateArray())
            {
                switch (dependencyElement.ValueKind)
                {
                    case JsonValueKind.Number:
                        intDeps ??= [];
                        intDeps.Add(dependencyElement.GetInt32());
                        break;

                    case JsonValueKind.Object:
                        parentDeps ??= [];
                        parentDeps.Add(dependencyElement.GetProperty("parentNodeId").GetInt32());
                        break;

                    default:
                        throw new InvalidOperationException(
                            $"Unsupported dependency element kind: {dependencyElement.ValueKind}.");
                }
            }

            parentDependencies = parentDeps?.ToArray();
            return intDeps?.ToArray();
        }

        return null;
    }

    private static ParsedIntrospectionNodeInfo ParseIntrospectionNodeInfo(
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

    private static ParsedNodeFieldNodeInfo ParseNodeFieldNodeInfo(
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
                throw ThrowHelper.InvalidOperationPlan(
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

    private static ImmutableArray<ImmutableArray<string>> ParsePolicyNameGroups(JsonElement namesElement)
    {
        if (namesElement.ValueKind is not JsonValueKind.Array)
        {
            throw ThrowHelper.InvalidOperationPlan(
                "The `names` property of a policy in the operation plan "
                + "must be a list of policy name groups.");
        }

        var groups = ImmutableArray.CreateBuilder<ImmutableArray<string>>();

        foreach (var groupElement in namesElement.EnumerateArray())
        {
            if (groupElement.ValueKind is not JsonValueKind.Array)
            {
                throw ThrowHelper.InvalidOperationPlan(
                    "A policy name group in the operation plan must be "
                    + "a list of policy names.");
            }

            var names = ImmutableArray.CreateBuilder<string>();

            foreach (var nameElement in groupElement.EnumerateArray())
            {
                if (nameElement.ValueKind is not JsonValueKind.String)
                {
                    throw ThrowHelper.InvalidOperationPlan(
                        "A policy name in the operation plan must be a string.");
                }

                names.Add(nameElement.GetString()!);
            }

            if (names.Count == 0)
            {
                throw ThrowHelper.InvalidOperationPlan(
                    "A policy name group in the operation plan must contain "
                    + "at least one policy name.");
            }

            groups.Add(names.ToImmutable());
        }

        if (groups.Count == 0)
        {
            throw ThrowHelper.InvalidOperationPlan(
                "A policy in the operation plan must contain at least "
                + "one policy name group.");
        }

        return groups.ToImmutable();
    }

    private static ParsedNodeInfo ParsePolicyNodeInfo(JsonElement nodeElement, int id)
    {
        var targetsElement = nodeElement.GetProperty("targets");
        RequireArray(targetsElement, "policy targets");
        var targets = new List<PolicyExecutionTarget>();

        foreach (var targetElement in targetsElement.EnumerateArray())
        {
            ValidateProperties(
                targetElement,
                ["occurrences", "kind", "path", "typeName", "policies", "requirements", "conditions"],
                ["occurrences", "kind", "path", "typeName", "policies"],
                "policy target");
            var occurrencesElement = targetElement.GetProperty("occurrences");
            RequireArray(occurrencesElement, "policy target occurrences");
            var policiesElement = targetElement.GetProperty("policies");
            RequireArray(policiesElement, "policy target policies");
            var policies = new List<PolicyApplication>();
            var requirements = new List<PolicyRequirement>();

            foreach (var policyElement in policiesElement.EnumerateArray())
            {
                ValidateProperties(
                    policyElement,
                    ["names", "onDenied"],
                    ["names", "onDenied"],
                    "policy target application");
                policies.Add(new PolicyApplication
                {
                    Groups = ParsePolicyNameGroups(policyElement.GetProperty("names")),
                    OnDenied = ParseDefinedEnum<PolicyDenialBehavior>(
                        policyElement.GetProperty("onDenied"),
                        "policy target denial behavior")
                });
            }

            if (targetElement.TryGetProperty("requirements", out var requirementsElement))
            {
                RequireArray(requirementsElement, "policy target requirements");
                foreach (var requirementElement in requirementsElement.EnumerateArray())
                {
                    ValidateProperties(
                        requirementElement,
                        ["name", "selectionSet"],
                        ["name", "selectionSet"],
                        "policy target requirement");
                    requirements.Add(new PolicyRequirement
                    {
                        PolicyName = requirementElement.GetProperty("name").GetString()!,
                        SelectionSet = Utf8GraphQLParser.Syntax.ParseSelectionSet(
                            requirementElement.GetProperty("selectionSet").GetString()!)
                    });
                }
            }

            targets.Add(new PolicyExecutionTarget
            {
                Occurrences = occurrencesElement
                    .EnumerateArray()
                    .Select(ParsePolicyOccurrence)
                    .ToImmutableArray(),
                Kind = ParseDefinedEnum<PolicyTargetKind>(
                    targetElement.GetProperty("kind"),
                    "policy target kind"),
                Path = SelectionPath.Parse(targetElement.GetProperty("path").GetString()!),
                TypeName = targetElement.GetProperty("typeName").GetString()!,
                Policies = policies.ToArray(),
                Requirements = requirements.ToArray(),
                Conditions = TryParseConditions(targetElement)
            });
        }

        var dependencies = TryParseDependencies(nodeElement, out var parentDependencies);
        var conditions = TryParseConditions(nodeElement);

        return new ParsedPolicyNodeInfo
        {
            Id = id,
            Targets = targets.ToArray(),
            Conditions = conditions,
            Dependencies = dependencies,
            ParentDependencies = parentDependencies
        };
    }

    private static PolicyOccurrenceReference ParsePolicyOccurrence(JsonElement element)
    {
        ValidateProperties(
            element,
            ["planPart", "selectionSetId", "selectionId", "occurrenceOrdinal", "applicationOrdinal", "facet"],
            ["planPart", "selectionSetId", "selectionId", "occurrenceOrdinal", "applicationOrdinal", "facet"],
            "policy occurrence");
        var facet = element.GetProperty("facet").GetString() switch
        {
            "slot-gate" => PolicyOccurrenceFacet.SlotGate,
            "residual-eval" => PolicyOccurrenceFacet.ResidualEvaluation,
            _ => throw ThrowHelper.InvalidOperationPlan("The policy occurrence facet is invalid.")
        };
        return new PolicyOccurrenceReference
        {
            PlanPart = element.GetProperty("planPart").GetInt32(),
            SelectionSetId = element.GetProperty("selectionSetId").GetInt32(),
            SelectionId = element.GetProperty("selectionId").GetInt32(),
            OccurrenceOrdinal = element.GetProperty("occurrenceOrdinal").GetInt32(),
            ApplicationOrdinal = element.GetProperty("applicationOrdinal").GetInt32(),
            Facet = facet
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
        public string? LookupTypeName { get; init; }
        public required SelectionPath Source { get; init; }
        public OperationRequirement[] Requirements { get; init; } = [];
        public string[] ForwardedVariables { get; init; } = [];
        public required ResultSelectionSet ResultSelectionSet { get; init; }
        public int? BatchingGroupId { get; init; }
        public int[]? ParentDependencies { get; init; }
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
            var definition = new SingleOperationDefinition(
                Id,
                OperationSource,
                LookupTypeName,
                SchemaName,
                Target,
                Source,
                Requirements,
                ForwardedVariables,
                ResultSelectionSet,
                Conditions,
                RequiresFileUpload);

            if (ParentDependencies is not null)
            {
                foreach (var parentId in ParentDependencies)
                {
                    definition.AddParentDependency(parentId);
                }
            }

            return definition;
        }

        public override (ExecutionNode, int[]?, Dictionary<string, int>?, int?) ToExecutionNodeTuple()
        {
            var node = new OperationExecutionNode(
                Id,
                OperationSource,
                LookupTypeName,
                SchemaName,
                Target,
                Source,
                Requirements,
                ForwardedVariables,
                ResultSelectionSet,
                Conditions,
                RequiresFileUpload);

            if (ParentDependencies is not null)
            {
                foreach (var parentId in ParentDependencies)
                {
                    node.AddParentDependency(parentId);
                }
            }

            return (node, Dependencies, null, null);
        }
    }

    private sealed class ParsedApolloOperationNodeInfo : ParsedOperationNodeInfo
    {
        public required SelectionPath Target { get; init; }

        public required FusionSchemaDefinition FusionSchema { get; init; }

        public required string EntityTypeName { get; init; }

        public ApolloEntityLookup CreateLookup()
            => new(
                OperationSource,
                Utf8GraphQLOperationParser.Parse(OperationSource.Value, ParserOptions.Trusted),
                EntityTypeName,
                RepresentationShape: default);

        public override OperationDefinition ToOperationDefinition()
        {
            var definition = new SingleOperationDefinition(
                Id,
                OperationSource,
                lookupTypeName: null,
                SchemaName,
                Target,
                Source,
                Requirements,
                ForwardedVariables,
                ResultSelectionSet,
                Conditions,
                RequiresFileUpload);

            if (ParentDependencies is not null)
            {
                foreach (var parentId in ParentDependencies)
                {
                    definition.AddParentDependency(parentId);
                }
            }

            return definition;
        }

        public override (ExecutionNode, int[]?, Dictionary<string, int>?, int?) ToExecutionNodeTuple()
        {
            var node = ApolloOperationExecutionNode.CreateFromParser(
                Id,
                OperationSource,
                EntityTypeName,
                SchemaName!,
                Target,
                Requirements,
                ForwardedVariables,
                ResultSelectionSet,
                Conditions,
                RequiresFileUpload,
                FusionSchema);

            if (ParentDependencies is not null)
            {
                foreach (var parentId in ParentDependencies)
                {
                    node.AddParentDependency(parentId);
                }
            }

            return (node, Dependencies, null, null);
        }
    }

    private sealed class ParsedEventStreamNodeInfo : ParsedNodeInfo
    {
        public required string FieldName { get; init; }

        public required SelectionPath Source { get; init; }

        public required SelectionPath Target { get; init; }

        public required ResultSelectionSet ResultSelectionSet { get; init; }

        public required EventStreamSource EventStreamSource { get; init; }

        public required string Message { get; init; }

        public int[]? ParentDependencies { get; init; }

        public ExecutionNodeCondition[] Conditions { get; init; } = [];

        public override (ExecutionNode, int[]?, Dictionary<string, int>?, int?) ToExecutionNodeTuple()
        {
            var node = new EventStreamExecutionNode(
                Id,
                FieldName,
                Target,
                Source,
                ResultSelectionSet,
                EventStreamSource,
                Message,
                Conditions);

            if (ParentDependencies is not null)
            {
                foreach (var parentId in ParentDependencies)
                {
                    node.AddParentDependency(parentId);
                }
            }

            return (node, Dependencies, null, null);
        }
    }

    private sealed class ParsedBatchOperationNodeInfo : ParsedOperationNodeInfo
    {
        public SelectionPath[] Targets { get; init; } = [];

        public override OperationDefinition ToOperationDefinition()
        {
            var definition = new BatchOperationDefinition(
                Id,
                OperationSource,
                LookupTypeName,
                SchemaName,
                Targets,
                Source,
                Requirements,
                ForwardedVariables,
                ResultSelectionSet,
                Conditions,
                RequiresFileUpload);

            if (ParentDependencies is not null)
            {
                foreach (var parentId in ParentDependencies)
                {
                    definition.AddParentDependency(parentId);
                }
            }

            return definition;
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

    private sealed class ParsedPolicyNodeInfo : ParsedNodeInfo
    {
        public PolicyExecutionTarget[] Targets { get; init; } = [];
        public int[]? ParentDependencies { get; init; }
        public ExecutionNodeCondition[] Conditions { get; init; } = [];

        public override (ExecutionNode, int[]?, Dictionary<string, int>?, int?) ToExecutionNodeTuple()
        {
            var node = new PolicyExecutionNode(Id, Targets, Conditions);

            if (ParentDependencies is not null)
            {
                foreach (var parentId in ParentDependencies)
                {
                    node.AddParentDependency(parentId);
                }
            }

            return (node, Dependencies, null, null);
        }
    }
}
