using System.Diagnostics;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Planning;

internal abstract class RequestDocumentFormatter
{
    private readonly FusionGraphConfiguration _config;

    protected RequestDocumentFormatter(FusionGraphConfiguration configuration)
    {
        _config = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected FusionGraphConfiguration Configuration => _config;

    internal RequestDocument CreateRequestDocument(
        QueryPlanContext context,
        SelectionExecutionStep executionStep,
        OperationType operationType = OperationType.Query)
    {
        var rootSelectionSetNode = CreateRootSelectionSetNode(context, executionStep);
        IReadOnlyList<string> path = Array.Empty<string>();

        if (executionStep.Resolver is not null &&
            executionStep.ParentSelection is not null)
        {
            ResolveRequirements(
                context,
                executionStep.ParentSelection,
                executionStep.Resolver,
                executionStep.Variables);

            var (rootResolver, p) =
                executionStep.Resolver.CreateSelection(
                    context.VariableValues,
                    rootSelectionSetNode,
                    null);

            rootSelectionSetNode = new SelectionSetNode(new[] { rootResolver });
            path = p;
        }

        var operationDefinitionNode = new OperationDefinitionNode(
            null,
            context.CreateRemoteOperationName(),
            operationType,
            context.Exports.CreateVariableDefinitions(
                context.ForwardedVariables,
                executionStep.Variables.Values,
                executionStep.Resolver?.Arguments),
            Array.Empty<DirectiveNode>(),
            rootSelectionSetNode);

        return new RequestDocument(
            new DocumentNode(new[] { operationDefinitionNode }),
            path);
    }

    protected virtual SelectionSetNode CreateRootSelectionSetNode(
        QueryPlanContext context,
        SelectionExecutionStep executionStep)
    {
        var selectionNodes = new List<ISelectionNode>();
        var selectionSet = executionStep.RootSelections[0].Selection.DeclaringSelectionSet;
        var selectionSetType = executionStep.SelectionSetTypeInfo;
        Debug.Assert(selectionSet is not null);

        // create
        foreach (var rootSelection in executionStep.RootSelections)
        {
            ISelectionNode selectionNode;
            var field = selectionSetType.Fields[rootSelection.Selection.Field.Name];

            if (rootSelection.Resolver is null)
            {
                selectionNode =
                    CreateSelectionNode(
                        context,
                        executionStep,
                        rootSelection.Selection,
                        field);
            }
            else
            {
                SelectionSetNode? selectionSetNode = null;

                if (rootSelection.Selection.SelectionSet is not null)
                {
                    selectionSetNode =
                        CreateSelectionSetNode(
                            context,
                            executionStep,
                            rootSelection.Selection);
                }

                ResolveRequirements(
                    context,
                    rootSelection.Selection,
                    selectionSetType,
                    executionStep.ParentSelection,
                    rootSelection.Resolver,
                    executionStep.Variables);

                var (s, _) = rootSelection.Resolver.CreateSelection(
                    context.VariableValues,
                    selectionSetNode,
                    rootSelection.Selection.ResponseName);
                selectionNode = s;
            }

            if (selectionNode is FieldNode fieldNode &&
                !rootSelection.Selection.ResponseName.EqualsOrdinal(fieldNode.Name.Value))
            {
                selectionNode = fieldNode.WithAlias(
                    new NameNode(rootSelection.Selection.ResponseName));
            }

            selectionNodes.Add(selectionNode);
        }

        // append exports that were required by other execution steps.
        selectionNodes.AddRange(
            context.Exports.GetExportSelections(
                executionStep,
                selectionSet));

        return new SelectionSetNode(selectionNodes);
    }

    protected virtual ISelectionNode CreateSelectionNode(
        QueryPlanContext context,
        SelectionExecutionStep executionStep,
        ISelection selection,
        ObjectFieldInfo fieldInfo)
    {
        SelectionSetNode? selectionSetNode = null;

        if (selection.SelectionSet is not null)
        {
            selectionSetNode =
                CreateSelectionSetNode(
                    context,
                    executionStep,
                    selection);
        }

        var binding = fieldInfo.Bindings[executionStep.SubgraphName];

        var alias = !selection.ResponseName.Equals(binding.Name)
            ? new NameNode(selection.ResponseName)
            : null;

        return new FieldNode(
            null,
            new(binding.Name),
            alias,
            selection.SyntaxNode.Required,
            Array.Empty<DirectiveNode>(), // todo : not sure if we should pass down directives.
            selection.SyntaxNode.Arguments,
            selectionSetNode);
    }

    protected virtual SelectionSetNode CreateSelectionSetNode(
        QueryPlanContext context,
        SelectionExecutionStep executionStep,
        ISelection parentSelection)
    {
        var selectionNodes = new List<ISelectionNode>();
        var typeSelectionNodes = selectionNodes;
        var possibleTypes = context.Operation.GetPossibleTypes(parentSelection);

        using var typeEnumerator = possibleTypes.GetEnumerator();
        var next = typeEnumerator.MoveNext();

        while (next)
        {
            var possibleType = typeEnumerator.Current;

            CreateSelectionNodes(
                context,
                executionStep,
                parentSelection,
                possibleType,
                typeSelectionNodes);

            next = typeEnumerator.MoveNext();

            var single = ReferenceEquals(typeSelectionNodes, selectionNodes);

            if (next && single)
            {
                selectionNodes = new List<ISelectionNode>();
                single = false;
            }

            if (!single)
            {
                AddInlineFragment(possibleType);
            }
        }

        return new SelectionSetNode(selectionNodes);

        void AddInlineFragment(IObjectType possibleType)
        {
            var inlineFragment = new InlineFragmentNode(
                null,
                new NamedTypeNode(
                    null,
                    new NameNode(
                        _config.GetSubgraphTypeName(
                            executionStep.SubgraphName,
                            possibleType.Name))),
                Array.Empty<DirectiveNode>(),
                new SelectionSetNode(typeSelectionNodes));
            selectionNodes.Add(inlineFragment);
            typeSelectionNodes = new List<ISelectionNode>();
        }
    }

    protected virtual void CreateSelectionNodes(
        QueryPlanContext context,
        SelectionExecutionStep executionStep,
        ISelection parentSelection,
        IObjectType possibleType,
        List<ISelectionNode> selectionNodes)
    {
        var typeContext = _config.GetType<ObjectTypeInfo>(possibleType.Name);
        var selectionSet = context.Operation.GetSelectionSet(parentSelection, possibleType);

        foreach (var selection in selectionSet.Selections)
        {
            if (executionStep.AllSelections.Contains(selection) ||
                selection.Field.Name.EqualsOrdinal(IntrospectionFields.TypeName))
            {
                selectionNodes.Add(
                    CreateSelectionNode(
                        context,
                        executionStep,
                        selection,
                        typeContext.Fields[selection.Field.Name]));

                if (!selection.Arguments.IsFullyCoercedNoErrors)
                {
                    foreach (var argument in selection.Arguments)
                    {
                        if (!argument.IsFullyCoerced)
                        {
                            TryForwardVariable(
                                context,
                                null,
                                argument,
                                argument.Name);
                        }
                    }
                }
            }
        }

        // append exports that were required by other execution steps.
        selectionNodes.AddRange(
            context.Exports.GetExportSelections(
                executionStep,
                selectionSet));

        if (selectionSet.Selections.Count > 0 && selectionNodes.Count == 0)
        {
            throw ThrowHelper.RequestFormatter_SelectionSetEmpty();
        }
    }

    protected void ResolveRequirements(
        QueryPlanContext context,
        ISelection parent,
        ResolverDefinition resolver,
        Dictionary<string, string> variableStateLookup)
    {
        context.VariableValues.Clear();

        var parentDeclaringType = _config.GetType<ObjectTypeInfo>(parent.DeclaringType.Name);
        var parentField = parentDeclaringType.Fields[parent.Field.Name];

        foreach (var variable in parentField.Variables)
        {
            if (!resolver.Requires.Contains(variable.Name))
            {
                continue;
            }

            if (variable is not ArgumentVariableDefinition argumentVariable)
            {
                throw ThrowHelper.RequestFormatter_ArgumentVariableExpected();
            }

            var argumentValue = parent.Arguments[argumentVariable.ArgumentName];
            context.VariableValues.Add(variable.Name, argumentValue.ValueLiteral!);
        }

        foreach (var requirement in resolver.Requires)
        {
            if (!context.VariableValues.ContainsKey(requirement))
            {
                var stateKey = variableStateLookup[requirement];
                context.VariableValues.Add(requirement, new VariableNode(stateKey));
            }
        }
    }

    protected void ResolveRequirements(
        QueryPlanContext context,
        ISelection selection,
        ObjectTypeInfo declaringTypeInfo,
        ISelection? parent,
        ResolverDefinition resolver,
        Dictionary<string, string> variableStateLookup)
    {
        context.VariableValues.Clear();

        var field = declaringTypeInfo.Fields[selection.Field.Name];

        foreach (var variable in field.Variables)
        {
            if (resolver.Requires.Contains(variable.Name) &&
                resolver.SubgraphName.Equals(variable.SubgraphName))
            {
                if (variable is not ArgumentVariableDefinition argumentVariable)
                {
                    throw ThrowHelper.RequestFormatter_ArgumentVariableExpected();
                }

                var argumentValue = selection.Arguments[argumentVariable.ArgumentName];
                context.VariableValues.Add(variable.Name, argumentValue.ValueLiteral!);
                TryForwardVariable(context, resolver, argumentValue, argumentVariable.ArgumentName);
            }
        }

        if (parent is not null)
        {
            var parentDeclaringType = _config.GetType<ObjectTypeInfo>(parent.DeclaringType.Name);
            var parentField = parentDeclaringType.Fields[parent.Field.Name];

            foreach (var variable in parentField.Variables)
            {
                if (context.VariableValues.ContainsKey(variable.Name) ||
                    !resolver.Requires.Contains(variable.Name))
                {
                    continue;
                }

                if (variable is not ArgumentVariableDefinition argumentVariable)
                {
                    throw ThrowHelper.RequestFormatter_ArgumentVariableExpected();
                }

                var argumentValue = parent.Arguments[argumentVariable.ArgumentName];
                context.VariableValues.Add(variable.Name, argumentValue.ValueLiteral!);
                TryForwardVariable(context, resolver, argumentValue, argumentVariable.ArgumentName);
            }
        }

        foreach (var requirement in resolver.Requires)
        {
            if (!context.VariableValues.ContainsKey(requirement))
            {
                var stateKey = variableStateLookup[requirement];
                context.VariableValues.Add(requirement, new VariableNode(stateKey));
            }
        }
    }

    protected static void TryForwardVariable(
        QueryPlanContext context,
        ResolverDefinition? resolver,
        ArgumentValue argumentValue,
        string argumentName)
    {
        if (argumentValue.ValueLiteral is VariableNode variableValue)
        {
            var originalVarDef = context.Operation.Definition.VariableDefinitions
                .First(t => t.Variable.Equals(variableValue, SyntaxComparison.Syntax));

            if (resolver is null ||
                !resolver.Arguments.TryGetValue(argumentName, out var type))
            {
                type = originalVarDef.Type;
            }

            context.ForwardedVariables.Add(
                new VariableDefinitionNode(
                    null,
                    variableValue,
                    type,
                    originalVarDef.DefaultValue,
                    Array.Empty<DirectiveNode>()));
        }
        else if (argumentValue.ValueLiteral?.Kind is SyntaxKind.ListValue or SyntaxKind.ObjectValue)
        {
            foreach (var variable in VariableVisitor.Collect(argumentValue.ValueLiteral))
            {
                var originalVarDef = context.Operation.Definition.VariableDefinitions
                    .First(t => t.Variable.Equals(variable, SyntaxComparison.Syntax));

                if (resolver is null ||
                    !resolver.Arguments.TryGetValue(argumentName, out var type))
                {
                    type = originalVarDef.Type;
                }

                context.ForwardedVariables.Add(
                    new VariableDefinitionNode(
                        null,
                        variable,
                        type,
                        originalVarDef.DefaultValue,
                        Array.Empty<DirectiveNode>()));
            }
        }


    }

    private sealed class VariableVisitor : SyntaxWalker<VariableVisitorContext>
    {
        protected override ISyntaxVisitorAction Enter(
            VariableNode node,
            VariableVisitorContext context)
        {
            context.VariableNodes.Add(node);
            return Continue;
        }

        public static IEnumerable<VariableNode> Collect(IValueNode node)
        {
            var context = new VariableVisitorContext();
            var visitor = new VariableVisitor();
            visitor.Visit(node, context);
            return context.VariableNodes;
        }
    }

    private class VariableVisitorContext : ISyntaxVisitorContext
    {
        public HashSet<VariableNode> VariableNodes { get; } = new(SyntaxComparer.BySyntax);
    }
}
