using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Planning;

public sealed partial class OperationPlanner
{
    private const string UploadScalarName = "Upload";

    /// <summary>
    /// Builds the actual execution plan from the provided <paramref name="planSteps"/>.
    /// </summary>
    private OperationPlan BuildExecutionPlan(
        Operation operation,
        OperationDefinitionNode operationDefinition,
        ImmutableList<PlanStep> planSteps,
        uint searchSpace,
        int expandedNodes)
    {
        if (operation.IsIntrospectionOnly())
        {
            var introspectionNode = new IntrospectionExecutionNode(
                1,
                [.. operation.RootSelectionSet.Selections],
                []);
            introspectionNode.Seal();

            var nodes = ImmutableArray.Create<ExecutionNode>(introspectionNode);

            return OperationPlan.Create(operation, nodes, nodes, searchSpace, expandedNodes);
        }

        var completedSteps = new HashSet<int>();
        var completedNodes = new Dictionary<int, ExecutionNode>();
        var dependencyLookup = new Dictionary<int, HashSet<int>>();
        var branchesLookup = new Dictionary<int, Dictionary<string, int>>();
        var fallbackLookup = new Dictionary<int, int>();
        var hasVariables = operationDefinition.VariableDefinitions.Count > 0;

        planSteps = PrepareSteps(planSteps, operationDefinition, dependencyLookup, branchesLookup, fallbackLookup);
        BuildExecutionNodes(
            planSteps,
            completedSteps,
            completedNodes,
            dependencyLookup,
            _schema,
            hasVariables);
        BuildDependencyStructure(completedNodes, dependencyLookup, branchesLookup, fallbackLookup);

        var rootNodes = planSteps
            .Where(t => !dependencyLookup.ContainsKey(t.Id))
            .Select(t => completedNodes[t.Id])
            .ToImmutableArray();

        var allNodes = completedNodes
            .OrderBy(t => t.Key)
            .Select(t => t.Value)
            .ToImmutableArray();

        if (operation.HasIntrospectionFields())
        {
            var introspectionNode = new IntrospectionExecutionNode(
                allNodes.Max(t => t.Id) + 1,
                operation.GetIntrospectionSelections(),
                []);
            rootNodes = rootNodes.Add(introspectionNode);
            allNodes = allNodes.Add(introspectionNode);
        }

        foreach (var node in allNodes)
        {
            node.Seal();
        }

        return OperationPlan.Create(operation, rootNodes, allNodes, searchSpace, expandedNodes);
    }

    private static ImmutableList<PlanStep> PrepareSteps(
        ImmutableList<PlanStep> planSteps,
        OperationDefinitionNode originalOperation,
        Dictionary<int, HashSet<int>> dependencyLookup,
        Dictionary<int, Dictionary<string, int>> branchesLookup,
        Dictionary<int, int> fallbackLookup)
    {
        var updatedPlanSteps = planSteps;
        var forwardVariableContext = new ForwardVariableRewriter.Context();

        foreach (var variableDef in originalOperation.VariableDefinitions)
        {
            forwardVariableContext.Variables[variableDef.Variable.Name.Value] = variableDef;
        }

        foreach (var step in planSteps)
        {
            if (step is OperationPlanStep operationPlanStep)
            {
                // Planning may leave temporary `{}` child selections after requirement rewrites.
                // We normalize those first, then only remove the step if the root selection set
                // itself became empty.
                operationPlanStep = RemoveEmptySelectionSets(operationPlanStep);

                if (!ReferenceEquals(step, operationPlanStep))
                {
                    updatedPlanSteps = updatedPlanSteps.Replace(step, operationPlanStep);
                }

                // During the planing process we keep incomplete operation steps around
                // in order to inline requirements. If those do not materialize these
                // operation fragments need to be removed before we can build the
                // execution plan.
                if (IsEmptyOperation(operationPlanStep))
                {
                    updatedPlanSteps = updatedPlanSteps.Remove(operationPlanStep);
                    continue;
                }

                // If all the root selections are conditional, we can pull those conditionals
                // out as conditions onto the execution node.
                // We can do the same for conditional selections below lookup fields.
                if (operationPlanStep.AreAllProvidedSelectionsConditional())
                {
                    var updatedOperationPlanStep = ExtractConditionsAndRewriteSelectionSet(operationPlanStep);

                    updatedPlanSteps = updatedPlanSteps.Replace(operationPlanStep, updatedOperationPlanStep);

                    operationPlanStep = updatedOperationPlanStep;
                }

                // The operation definition of the current OperationPlanStep do not yet
                // have variable definitions declared, so we need to traverse the operation definition
                // and look at what variables and requirements are used within the operation definition.
                updatedPlanSteps = updatedPlanSteps.Replace(
                    operationPlanStep,
                    AddVariableDefinitions(operationPlanStep));

                // Each PlanStep tracks dependant PlanSteps,
                // so PlanSteps that require data (lookup or field requirements)
                // from the current step.
                // For a simpler planing algorithm we are building a lookup in reverse,
                // that tracks the dependencies each node has.
                foreach (var dependent in operationPlanStep.Dependents)
                {
                    if (!dependencyLookup.TryGetValue(dependent, out var dependencies))
                    {
                        dependencies = [];
                        dependencyLookup[dependent] = dependencies;
                    }

                    dependencies.Add(step.Id);
                }
            }
            else if (step is NodeFieldPlanStep nodePlanStep)
            {
                foreach (var (_, dependent) in nodePlanStep.Branches)
                {
                    if (!dependencyLookup.TryGetValue(dependent.Id, out var dependencies))
                    {
                        dependencies = [];
                        dependencyLookup[dependent.Id] = dependencies;
                    }

                    dependencies.Add(nodePlanStep.Id);
                }

                if (!dependencyLookup.TryGetValue(nodePlanStep.FallbackQuery.Id, out var fallbackDependencies))
                {
                    fallbackDependencies = [];
                    dependencyLookup[nodePlanStep.FallbackQuery.Id] = fallbackDependencies;
                }

                fallbackDependencies.Add(nodePlanStep.Id);

                branchesLookup.Add(nodePlanStep.Id, nodePlanStep.Branches.ToDictionary(x => x.Key, x => x.Value.Id));
                fallbackLookup.Add(nodePlanStep.Id, nodePlanStep.FallbackQuery.Id);
            }
        }

        return updatedPlanSteps;

        bool IsEmptyOperation(OperationPlanStep step)
        {
            return step.Definition.SelectionSet.Selections.Count == 0;
        }

        OperationPlanStep RemoveEmptySelectionSets(OperationPlanStep step)
        {
            var updatedDefinition = RemoveEmptySelections(step.Definition);
            return ReferenceEquals(updatedDefinition, step.Definition)
                ? step
                : step with { Definition = updatedDefinition };
        }

        OperationPlanStep AddVariableDefinitions(OperationPlanStep step)
        {
            forwardVariableContext.Reset();

            foreach (var (key, requirement) in step.Requirements.OrderBy(t => t.Key))
            {
                forwardVariableContext.Requirements[key] =
                    new VariableDefinitionNode(
                        null,
                        new VariableNode(null, new NameNode(key)),
                        description: null,
                        requirement.Type,
                        null,
                        []);
            }

            var rewrittenNode = s_forwardVariableRewriter.Rewrite(step.Definition, forwardVariableContext);

            if (rewrittenNode is OperationDefinitionNode rewrittenOperationNode
                && !ReferenceEquals(rewrittenOperationNode, step.Definition))
            {
                return step with { Definition = rewrittenOperationNode };
            }

            return step;
        }
    }

    private static void BuildExecutionNodes(
        ImmutableList<PlanStep> planSteps,
        HashSet<int> completedSteps,
        Dictionary<int, ExecutionNode> completedNodes,
        Dictionary<int, HashSet<int>> dependencyLookup,
        ISchemaDefinition schema,
        bool hasVariables)
    {
        var hasUploadScalar = schema.Types.TryGetType(UploadScalarName, out var uploadType)
            && uploadType.IsScalarType();
        var readySteps = planSteps.Where(t => !dependencyLookup.ContainsKey(t.Id)).ToList();
        List<string>? variables = null;

        while (completedSteps.Count < planSteps.Count)
        {
            foreach (var step in readySteps)
            {
                if (!completedSteps.Add(step.Id))
                {
                    continue;
                }

                if (step is OperationPlanStep operationStep)
                {
                    var requirements = Array.Empty<OperationRequirement>();

                    if (!operationStep.Requirements.IsEmpty)
                    {
                        var temp = new List<OperationRequirement>();

                        foreach (var (_, requirement) in operationStep.Requirements.OrderBy(t => t.Key))
                        {
                            temp.Add(requirement);
                        }

                        requirements = temp.ToArray();
                    }

                    variables?.Clear();

                    if (hasVariables && operationStep.Definition.VariableDefinitions.Count > 0)
                    {
                        variables ??= [];

                        foreach (var variableDef in operationStep.Definition.VariableDefinitions)
                        {
                            if (requirements.Any(r => r.Key == variableDef.Variable.Name.Value))
                            {
                                continue;
                            }

                            variables.Add(variableDef.Variable.Name.Value);
                        }
                    }

                    var requiresFileUpload = hasUploadScalar
                        && DoVariablesContainUploadScalar(operationStep.Definition.VariableDefinitions, schema);

                    var node = new OperationExecutionNode(
                        operationStep.Id,
                        RemoveEmptyTypeNames(operationStep.Definition).ToSourceText(),
                        operationStep.SchemaName,
                        operationStep.Target,
                        operationStep.Source,
                        requirements,
                        variables?.Count > 0 ? variables.ToArray() : [],
                        GetResponseNamesFromPath(operationStep.Definition, operationStep.Source),
                        operationStep.Conditions,
                        requiresFileUpload);

                    completedNodes.Add(step.Id, node);
                }
                else if (step is NodeFieldPlanStep nodeStep)
                {
                    var node = new NodeFieldExecutionNode(
                        nodeStep.Id,
                        nodeStep.ResponseName,
                        nodeStep.IdValue,
                        nodeStep.Conditions);

                    completedNodes.Add(step.Id, node);
                }
            }

            readySteps.Clear();

            foreach (var step in planSteps)
            {
                if (dependencyLookup.TryGetValue(step.Id, out var stepDependencies)
                    && completedSteps.IsSupersetOf(stepDependencies))
                {
                    readySteps.Add(step);
                }
            }

            if (readySteps.Count == 0)
            {
                break;
            }
        }
    }

    private static void BuildDependencyStructure(
        Dictionary<int, ExecutionNode> completedNodes,
        Dictionary<int, HashSet<int>> dependencyLookup,
        Dictionary<int, Dictionary<string, int>> branchesLookup,
        Dictionary<int, int> fallbackLookup)
    {
        foreach (var (nodeId, stepDependencies) in dependencyLookup)
        {
            if (!completedNodes.TryGetValue(nodeId, out var entry) || entry is not OperationExecutionNode node)
            {
                continue;
            }

            foreach (var dependencyId in stepDependencies)
            {
                if (!completedNodes.TryGetValue(dependencyId, out var childEntry)
                    || childEntry is not (OperationExecutionNode or NodeFieldExecutionNode))
                {
                    continue;
                }

                childEntry.AddDependent(node);
                node.AddDependency(childEntry);
            }
        }

        foreach (var (nodeId, branches) in branchesLookup)
        {
            if (!completedNodes.TryGetValue(nodeId, out var entry) || entry is not NodeFieldExecutionNode node)
            {
                continue;
            }

            foreach (var (typeName, branchNodeId) in branches)
            {
                if (!completedNodes.TryGetValue(branchNodeId, out var branchNode))
                {
                    continue;
                }

                node.AddBranch(typeName, branchNode);
            }
        }

        foreach (var (nodeId, fallbackNodeId) in fallbackLookup)
        {
            if (!completedNodes.TryGetValue(nodeId, out var entry) || entry is not NodeFieldExecutionNode node)
            {
                continue;
            }

            if (!completedNodes.TryGetValue(fallbackNodeId, out var fallbackNode))
            {
                continue;
            }

            node.AddFallbackQuery(fallbackNode);
        }
    }

    private static string[] GetResponseNamesFromPath(
        OperationDefinitionNode operationDefinition,
        SelectionPath path)
    {
        var selectionSet = GetSelectionSetNodeFromPath(operationDefinition, path);

        if (selectionSet is null)
        {
            return [];
        }

        var responseNames = new List<string>();

        var stack = new Stack<ISelectionNode>(selectionSet.Selections);

        while (stack.TryPop(out var selection))
        {
            switch (selection)
            {
                case FieldNode fieldNode:
                    responseNames.Add(fieldNode.Alias?.Value ?? fieldNode.Name.Value);
                    break;

                case InlineFragmentNode inlineFragmentNode:
                    foreach (var child in inlineFragmentNode.SelectionSet.Selections)
                    {
                        stack.Push(child);
                    }

                    break;
            }
        }

        return [.. responseNames];
    }

    private static SelectionSetNode? GetSelectionSetNodeFromPath(
        OperationDefinitionNode operationDefinition,
        SelectionPath path)
    {
        var current = operationDefinition.SelectionSet;

        if (path.IsRoot)
        {
            return current;
        }

        for (var i = 0; i < path.Segments.Length; i++)
        {
            var segment = path.Segments[i];

            switch (segment.Kind)
            {
                case SelectionPathSegmentKind.InlineFragment:
                {
                    var selection = current.Selections
                        .OfType<InlineFragmentNode>()
                        .FirstOrDefault(s => s.TypeCondition?.Name.Value == segment.Name);

                    if (selection is null)
                    {
                        return null;
                    }

                    current = selection.SelectionSet;
                    break;
                }
                case SelectionPathSegmentKind.Field:
                {
                    var selection = current.Selections
                        .OfType<FieldNode>()
                        .FirstOrDefault(s => s.Alias?.Value == segment.Name || s.Name.Value == segment.Name);

                    if (selection?.SelectionSet is null)
                    {
                        return null;
                    }

                    current = selection.SelectionSet;
                    break;
                }
            }
        }

        return current;
    }

    private static bool DoVariablesContainUploadScalar(
        IReadOnlyList<VariableDefinitionNode> variables,
        ISchemaDefinition schema)
    {
        var inputObjectTypes = new Queue<IInputObjectTypeDefinition>();

        foreach (var variable in variables)
        {
            var variableTypeName = variable.Type.NamedType().Name.Value;
            var variableType = schema.Types[variableTypeName];

            if (variableType is IScalarTypeDefinition { Name: UploadScalarName })
            {
                return true;
            }

            if (variableType is IInputObjectTypeDefinition inputObjectType)
            {
                inputObjectTypes.Enqueue(inputObjectType);
            }
        }

        while (inputObjectTypes.TryDequeue(out var inputObjectType))
        {
            foreach (var field in inputObjectType.Fields)
            {
                var fieldType = field.Type.NamedType();

                if (fieldType is IScalarTypeDefinition { Name: UploadScalarName })
                {
                    return true;
                }

                if (fieldType is IInputObjectTypeDefinition nestedInputObjectType)
                {
                    inputObjectTypes.Enqueue(nestedInputObjectType);
                }
            }
        }

        return false;
    }

    private static OperationDefinitionNode RemoveEmptySelections(OperationDefinitionNode operationDefinition)
    {
        // Remove fields/fragments whose selection sets collapsed to `{}` during rewriting.
        // This is local cleanup and intentionally does not remove the whole operation node.
        return SyntaxRewriter.Create(
            rewrite: node =>
            {
                if (node is not SelectionSetNode selectionSet)
                {
                    return node;
                }

                List<ISelectionNode>? rewritten = null;
                var selections = selectionSet.Selections;

                for (var i = 0; i < selections.Count; i++)
                {
                    var selection = selections[i];
                    var removeSelection =
                        selection is FieldNode { SelectionSet: { Selections.Count: 0 } }
                        || selection is InlineFragmentNode { SelectionSet.Selections.Count: 0 };

                    if (!removeSelection)
                    {
                        rewritten?.Add(selection);
                        continue;
                    }

                    if (rewritten is null)
                    {
                        rewritten = new List<ISelectionNode>(selections.Count - 1);
                        for (var j = 0; j < i; j++)
                        {
                            rewritten.Add(selections[j]);
                        }
                    }
                }

                return rewritten is null
                    ? node
                    : new SelectionSetNode(rewritten);
            })
            .Rewrite(operationDefinition)!;
    }

    private static OperationDefinitionNode RemoveEmptyTypeNames(OperationDefinitionNode operationDefinition)
    {
        return (OperationDefinitionNode)SyntaxRewriter.Create<List<bool>>(
            rewrite: (node, context) =>
            {
                if (node is SelectionSetNode selectionSet && context.Peek())
                {
                    var items = selectionSet.Selections.ToList();
                    for (var i = items.Count - 1; i >= 0; i--)
                    {
                        if (items[i] is FieldNode
                            {
                                Alias: null,
                                Name.Value: IntrospectionFieldNames.TypeName,
                                Directives: [{ Name.Value: "fusion__empty" }]
                            } field)
                        {
                            if (items.Count > 1)
                            {
                                items.RemoveAt(i);
                            }
                            else
                            {
                                items[i] = field.WithDirectives([]);
                            }
                        }
                    }

                    return new SelectionSetNode(items);
                }

                return node;
            },
            enter: (node, context) =>
            {
                switch (node)
                {
                    case SelectionSetNode:
                        context.Push(false);
                        break;

                    case FieldNode
                    {
                        Alias: null,
                        Name.Value: IntrospectionFieldNames.TypeName,
                        Directives: [{ Name.Value: "fusion__empty" }]
                    }:
                        context[^1] = true;
                        break;
                }

                return context;
            },
            leave: (node, context) =>
            {
                if (node is SelectionSetNode)
                {
                    context.Pop();
                }
            })
            .Rewrite(operationDefinition, [])!;
    }

    /// <summary>
    /// Pulls out conditions around the root selection set or the selection set below a lookup field,
    /// and adds them as conditions to <paramref name="step"/>.
    /// </summary>
    private static OperationPlanStep ExtractConditionsAndRewriteSelectionSet(OperationPlanStep step)
    {
        var context = new ConditionalSelectionSetRewriterContext();

        OperationDefinitionNode newOperation;

        if (step.Lookup is not null)
        {
            FieldNode? lookupFieldNode = null;

            foreach (var selection in step.Definition.SelectionSet.Selections)
            {
                if (selection is FieldNode fieldNode && fieldNode.Name.Value == step.Lookup.FieldName)
                {
                    lookupFieldNode = fieldNode;
                    break;
                }
            }

            if (lookupFieldNode?.SelectionSet is not { } lookupSelectionSet)
            {
                throw new InvalidOperationException(
                    "Expected to find the lookup field with a selection set in the operation definition");
            }

            var newLookupSelectionSet = RewriteConditionalSelectionSet(lookupSelectionSet, context);
            var newLookupField = lookupFieldNode.WithSelectionSet(newLookupSelectionSet);

            newOperation = step.Definition.WithSelectionSet(
                new SelectionSetNode([newLookupField]));
        }
        else
        {
            var newRootSelectionSet = RewriteConditionalSelectionSet(step.Definition.SelectionSet, context);

            newOperation = step.Definition.WithSelectionSet(newRootSelectionSet);
        }

        return step with { Definition = newOperation, Conditions = context.Conditions.ToArray() };
    }

    private static SelectionSetNode RewriteConditionalSelectionSet(
        SelectionSetNode selectionSetNode,
        ConditionalSelectionSetRewriterContext context)
    {
        var selections = new List<ISelectionNode>();

        foreach (var selection in selectionSetNode.Selections)
        {
            switch (selection)
            {
                case FieldNode fieldNode:
                {
                    var conditions = ExtractConditions(fieldNode.Directives);

                    if (conditions is not null)
                    {
                        var newDirectives = new List<DirectiveNode>(fieldNode.Directives);

                        foreach (var condition in conditions)
                        {
                            context.Conditions.Add(condition);
                            newDirectives.Remove(condition.Directive!);
                        }

                        fieldNode = fieldNode.WithDirectives(newDirectives);
                    }

                    selections.Add(fieldNode);
                    break;
                }
                case InlineFragmentNode inlineFragmentNode:
                {
                    if (inlineFragmentNode.TypeCondition is null)
                    {
                        var fragmentSelectionSet = RewriteConditionalSelectionSet(inlineFragmentNode.SelectionSet, context);

                        if (fragmentSelectionSet.Selections.Count == 0)
                        {
                            continue;
                        }

                        var conditions = ExtractConditions(inlineFragmentNode.Directives);

                        if (conditions is not null)
                        {
                            var newDirectives = new List<DirectiveNode>(inlineFragmentNode.Directives);

                            foreach (var condition in conditions)
                            {
                                context.Conditions.Add(condition);
                                newDirectives.Remove(condition.Directive!);
                            }

                            if (newDirectives.Count == 0)
                            {
                                selections.AddRange(fragmentSelectionSet.Selections);
                                continue;
                            }

                            inlineFragmentNode = inlineFragmentNode.WithDirectives(newDirectives);
                        }
                    }

                    selections.Add(inlineFragmentNode);
                    break;
                }
            }
        }

        return new SelectionSetNode(selections);
    }

    private sealed class ConditionalSelectionSetRewriterContext
    {
        public HashSet<ExecutionNodeCondition> Conditions { get; } = [];
    }
}

file static class Extensions
{
    private static readonly Encoding s_encoding = Encoding.UTF8;

    /// <summary>
    /// Checks if an entire selection set, either on the root or below
    /// a lookup field, is conditional.
    /// </summary>
    /// <returns>
    /// <c>true</c>, if all provided selections on either the root
    /// or below a lookup field are conditional, otherwise <c>false</c>.
    /// </returns>
    public static bool AreAllProvidedSelectionsConditional(this OperationPlanStep step)
    {
        var selectionSetNode = step.Definition.SelectionSet;

        if (step.Lookup is not null)
        {
            FieldNode? lookupFieldNode = null;

            if (!step.Lookup.Path.IsEmpty)
            {
                foreach (var fieldName in step.Lookup.Path)
                {
                    var fieldNode = selectionSetNode.Selections.FirstOrDefault(
                        selection => selection is FieldNode fieldNode && fieldNode.Name.Value == fieldName);

                    if (fieldNode is not FieldNode { SelectionSet: { } nextSelectionSetNode })
                    {
                        throw new InvalidOperationException("Unable to resolve the lookup path.");
                    }

                    selectionSetNode = nextSelectionSetNode;
                }
            }

            foreach (var selection in selectionSetNode.Selections)
            {
                if (selection is FieldNode fieldNode && fieldNode.Name.Value == step.Lookup.FieldName)
                {
                    lookupFieldNode = fieldNode;
                    break;
                }
            }

            selectionSetNode = lookupFieldNode?.SelectionSet ?? throw new InvalidOperationException(
                "Expected to find the lookup field with a selection set in the operation definition");
        }

        foreach (var selection in selectionSetNode.Selections)
        {
            switch (selection)
            {
                case FieldNode fieldNode
                    when !fieldNode.Directives.Any(d => d.Name.Value is "skip" or "include"):
                    return false;
                case InlineFragmentNode inlineFragmentNode
                    when !inlineFragmentNode.Directives.Any(d => d.Name.Value is "skip" or "include"):
                    return false;
            }
        }

        return true;
    }

    public static bool IsIntrospectionOnly(this Operation operation)
    {
        foreach (var selection in operation.RootSelectionSet.Selections)
        {
            if (selection.Field.IsIntrospectionField)
            {
                continue;
            }

            return false;
        }

        return true;
    }

    public static bool HasIntrospectionFields(this Operation operation)
    {
        foreach (var selection in operation.RootSelectionSet.Selections)
        {
            if (selection.Field.IsIntrospectionField)
            {
                return true;
            }
        }

        return false;
    }

    public static Selection[] GetIntrospectionSelections(this Operation operation)
    {
        var selections = new List<Selection>(operation.RootSelectionSet.Selections.Length);

        foreach (var selection in operation.RootSelectionSet.Selections)
        {
            if (selection.Field.IsIntrospectionField)
            {
                selections.Add(selection);
            }
        }

        return selections.ToArray();
    }

    public static OperationSourceText ToSourceText(this OperationDefinitionNode operation)
    {
        var sourceText = operation.ToString(indented: true);
        var sourceTextUtf8 = s_encoding.GetBytes(sourceText);
#if NET9_0_OR_GREATER
        var operationHash = Convert.ToHexStringLower(SHA256.HashData(sourceTextUtf8));
#else
        var operationHash = Convert.ToHexString(SHA256.HashData(sourceTextUtf8)).ToLowerInvariant();
#endif
        return new OperationSourceText(operation.Name!.Value, operation.Operation, sourceText, operationHash);
    }
}
