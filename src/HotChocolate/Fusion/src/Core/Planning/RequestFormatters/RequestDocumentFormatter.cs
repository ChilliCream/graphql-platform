using System.Diagnostics;
using System.Runtime.CompilerServices;
using HotChocolate.Execution.Processing;
using HotChocolate.Fusion.Metadata;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Introspection;
using HotChocolate.Utilities;
using ThrowHelper = HotChocolate.Fusion.Utilities.ThrowHelper;

namespace HotChocolate.Fusion.Planning;

internal abstract class RequestDocumentFormatter(FusionGraphConfiguration configuration)
{
    protected static readonly FieldNode TypeNameField = new(
        null,
        new NameNode("__typename"),
        null,
        Array.Empty<DirectiveNode>(),
        Array.Empty<ArgumentNode>(),
        null);

    private readonly FusionGraphConfiguration _config = configuration ??
        throw new ArgumentNullException(nameof(configuration));

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

            var unspecifiedArguments = GetUnspecifiedArguments(executionStep.ParentSelection);

            var (rootResolver, p) =
                executionStep.Resolver.CreateSelection(
                    context.VariableValues,
                    rootSelectionSetNode,
                    null,
                    unspecifiedArguments,
                    null);

            rootSelectionSetNode = new SelectionSetNode(new[] { rootResolver, });
            path = p;
        }

        if (executionStep.Resolver is null &&
            executionStep.SelectionResolvers.Count == 0 &&
            executionStep.ParentSelectionPath is not null)
        {
            rootSelectionSetNode = CreateRootLevelQuery(
                context,
                executionStep.ParentSelectionPath,
                rootSelectionSetNode);
        }

        var operationDefinitionNode = new OperationDefinitionNode(
            null,
            context.CreateRemoteOperationName(),
            operationType,
            context.Exports.CreateVariableDefinitions(
                context.ForwardedVariables,
                executionStep.Variables.Values,
                executionStep.ArgumentTypes),
            Array.Empty<DirectiveNode>(),
            rootSelectionSetNode);

        return new RequestDocument(
            new DocumentNode(new[] { operationDefinitionNode, }),
            path);
    }

    private SelectionSetNode CreateRootLevelQuery(
        QueryPlanContext context,
        SelectionPath path,
        SelectionSetNode selectionSet)
    {
        var current = path;

        while (current is not null)
        {
            var selectionNode = current.Selection.SyntaxNode.WithSelectionSet(selectionSet);

            // TODO: this is not good but will fix the include issue ...
            // we need to rework the operation compiler for a proper fix.
            if (selectionNode.Directives.Count > 0)
            {
                foreach (var directive in selectionNode.Directives)
                {
                    foreach (var argument in directive.Arguments)
                    {
                        if (argument.Value is not VariableNode variable)
                        {
                            continue;
                        }

                        var originalVarDef = context.Operation.Definition.VariableDefinitions
                            .First(t => t.Variable.Equals(variable, SyntaxComparison.Syntax));
                        context.ForwardedVariables.Add(originalVarDef);
                    }
                }
            }

            selectionSet = new SelectionSetNode(new[] { selectionNode, });

            current = current.Parent;
        }

        return selectionSet;
    }

    protected virtual SelectionSetNode CreateRootSelectionSetNode(
        QueryPlanContext context,
        SelectionExecutionStep executionStep)
    {
        var selectionNodes = new List<ISelectionNode>();
        var rootNodes = new List<ISelectionNode>();
        var selectionSet = context.Operation.GetSelectionSet(executionStep);
        var selectionSetType = executionStep.SelectionSetTypeMetadata;
        Debug.Assert(selectionSet is not null);

        // create
        foreach (var rootSelection in executionStep.RootSelections)
        {
            ISelectionNode selectionNode;
            var field = selectionSetType.Fields[rootSelection.Selection.Field.Name];

            if (rootSelection.Resolver is null)
            {
                rootNodes.Clear();
                AddSelectionNode(context, executionStep, rootSelection.Selection, field, rootNodes);
                selectionNode = rootNodes[0];

                if (!rootSelection.Selection.Arguments.IsFullyCoercedNoErrors)
                {
                    foreach (var argument in rootSelection.Selection.Arguments)
                    {
                        if (!argument.IsFullyCoerced)
                        {
                            TryForwardVariable(
                                context,
                                executionStep.SubgraphName,
                                null,
                                argument,
                                argument.Name);
                        }
                    }
                }
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

                var unspecifiedArguments = GetUnspecifiedArguments(rootSelection.Selection);

                var (s, _) = rootSelection.Resolver.CreateSelection(
                    context.VariableValues,
                    selectionSetNode,
                    rootSelection.Selection.ResponseName,
                    unspecifiedArguments,
                    rootSelection.Selection.SyntaxNode.Directives);
                selectionNode = s;
            }

            if (selectionNode is FieldNode fieldNode &&
                !rootSelection.Selection.ResponseName.EqualsOrdinal(fieldNode.Name.Value))
            {
                selectionNode = fieldNode.WithAlias(
                    new NameNode(rootSelection.Selection.ResponseName));
            }

            // TODO: this is not good but will fix the include issue ...
            // we need to rework the operation compiler for a proper fix.
            if (selectionNode.Directives.Count > 0)
            {
                foreach (var directive in selectionNode.Directives)
                {
                    foreach (var argument in directive.Arguments)
                    {
                        if (argument.Value is not VariableNode variable)
                        {
                            continue;
                        }

                        var originalVarDef = context.Operation.Definition.VariableDefinitions
                            .First(t => t.Variable.Equals(variable, SyntaxComparison.Syntax));
                        context.ForwardedVariables.Add(originalVarDef);
                    }
                }
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

    protected virtual void AddSelectionNode(
        QueryPlanContext context,
        SelectionExecutionStep executionStep,
        ISelection selection,
        ObjectFieldInfo fieldInfo,
        List<ISelectionNode> selectionNodes)
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

        // TODO: this is not good but will fix the include issue ...
        // we need to rework the operation compiler for a proper fix.
        foreach (var node in selection.SyntaxNodes)
        {
            if(node.Directives.Count > 0)
            {
                foreach (var directive in node.Directives)
                {
                    foreach (var argument in directive.Arguments)
                    {
                        if (argument.Value is not VariableNode variable)
                        {
                            continue;
                        }

                        var originalVarDef = context.Operation.Definition.VariableDefinitions
                            .First(t => t.Variable.Equals(variable, SyntaxComparison.Syntax));
                        context.ForwardedVariables.Add(originalVarDef);
                    }
                }
            }

            selectionNodes.Add(
                new FieldNode(
                    null,
                    new(binding.Name),
                    alias,
                    node.Directives,
                    node.Arguments,
                    selectionSetNode));
        }
    }

    protected virtual SelectionSetNode CreateSelectionSetNode(
        QueryPlanContext context,
        SelectionExecutionStep executionStep,
        ISelection parentSelection)
    {
        var selectionNodes = new List<ISelectionNode>();
        var typeSelectionNodes = selectionNodes;
        var possibleTypes = context.Operation.GetPossibleTypes(parentSelection);
        var parentType = parentSelection.Type.NamedType();

        using var typeEnumerator = possibleTypes.GetEnumerator();
        var next = typeEnumerator.MoveNext();
        var needsTypeNameField = true;
        var isAbstractType = parentType.Kind is TypeKind.Interface or TypeKind.Union;

        while (next)
        {
            var possibleType = typeEnumerator.Current;
            var selectionSet = Unsafe.As<SelectionSet>(
                context.Operation.GetSelectionSet(parentSelection, possibleType));

            var onlyIntrospection =
                CreateSelectionNodes(
                    context,
                    executionStep,
                    possibleType,
                    selectionSet,
                    typeSelectionNodes);

            next = typeEnumerator.MoveNext();

            var single = ReferenceEquals(typeSelectionNodes, selectionNodes);

            if (next && single)
            {
                selectionNodes = [];
                single = false;
            }
            else if (single && isAbstractType && !ReferenceEquals(parentType, possibleType))
            {
                if (!onlyIntrospection)
                {
                    selectionNodes = [];
                    single = false;
                }
            }

            if (single)
            {
                continue;
            }

            if (needsTypeNameField)
            {
                selectionNodes.Add(TypeNameField);
                needsTypeNameField = false;
            }

            AddInlineFragment(possibleType);
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
            typeSelectionNodes = [];
        }
    }

    protected virtual bool CreateSelectionNodes(
        QueryPlanContext context,
        SelectionExecutionStep executionStep,
        IObjectType possibleType,
        SelectionSet selectionSet,
        List<ISelectionNode> selectionNodes)
    {
        var onlyIntrospection = true;
        var typeContext = _config.GetType<ObjectTypeMetadata>(possibleType.Name);

        ref var selection = ref selectionSet.GetSelectionsReference();
        ref var end = ref Unsafe.Add(ref selection, selectionSet.Selections.Count);

        while (Unsafe.IsAddressLessThan(ref selection, ref end))
        {
            if (!executionStep.AllSelections.Contains(selection) &&
                !selection.Field.Name.EqualsOrdinal(IntrospectionFields.TypeName))
            {
                goto NEXT;
            }

            if (onlyIntrospection && !selection.Field.IsIntrospectionField)
            {
                onlyIntrospection = false;
            }

            AddSelectionNode(
                context,
                executionStep,
                selection,
                typeContext.Fields[selection.Field.Name],
                selectionNodes);

            if (!selection.Arguments.IsFullyCoercedNoErrors)
            {
                foreach (var argument in selection.Arguments)
                {
                    if (!argument.IsFullyCoerced)
                    {
                        TryForwardVariable(
                            context,
                            executionStep.SubgraphName,
                            null,
                            argument,
                            argument.Name);
                    }
                }
            }

            NEXT:
            selection = ref Unsafe.Add(ref selection, 1)!;
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

        return onlyIntrospection;
    }

    protected void ResolveRequirements(
        QueryPlanContext context,
        ISelection parent,
        ResolverDefinition resolver,
        Dictionary<string, string> variableStateLookup)
    {
        context.VariableValues.Clear();

        var parentDeclaringType = _config.GetType<ObjectTypeMetadata>(parent.DeclaringType.Name);
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
        ObjectTypeMetadata declaringTypeMetadata,
        ISelection? parent,
        ResolverDefinition resolver,
        Dictionary<string, string> variableStateLookup)
    {
        context.VariableValues.Clear();

        var field = declaringTypeMetadata.Fields[selection.Field.Name];

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

                if (argumentValue.IsDefaultValue)
                {
                    // We don't want to register and pass a value to an argument
                    // that wasn't explicitly specified in the original operation.
                    continue;
                }

                context.VariableValues.Add(variable.Name, argumentValue.ValueLiteral!);
                TryForwardVariable(
                    context,
                    resolver.SubgraphName,
                    resolver,
                    argumentValue,
                    argumentVariable.ArgumentName);
            }
        }

        if (parent is not null)
        {
            var parentDeclaringType = _config.GetType<ObjectTypeMetadata>(parent.DeclaringType.Name);
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
                TryForwardVariable(
                    context,
                    resolver.SubgraphName,
                    resolver,
                    argumentValue,
                    argumentVariable.ArgumentName);
            }
        }

        foreach (var requirement in resolver.Requires)
        {
            if (!context.VariableValues.ContainsKey(requirement) &&
                variableStateLookup.TryGetValue(requirement, out var stateKey))
            {
                context.VariableValues.Add(requirement, new VariableNode(stateKey));
            }
        }
    }

    protected void TryForwardVariable(
        QueryPlanContext context,
        string subgraphName,
        ResolverDefinition? resolver,
        ArgumentValue argumentValue,
        string argumentName)
    {
        if (argumentValue.ValueLiteral is VariableNode variableValue)
        {
            var originalVarDef = context.Operation.Definition.VariableDefinitions
                .First(t => t.Variable.Equals(variableValue, SyntaxComparison.Syntax));

            if (resolver is null || !resolver.ArgumentTypes.TryGetValue(argumentName, out var type))
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

                var typeNode = originalVarDef.Type;
                var originalTypeName = typeNode.NamedType().Name.Value;
                var subgraphTypeName = _config.GetSubgraphTypeName(subgraphName, originalTypeName);

                if (!subgraphTypeName.EqualsOrdinal(originalTypeName))
                {
                    typeNode = typeNode.RenameName(subgraphTypeName);
                }

                context.ForwardedVariables.Add(
                    new VariableDefinitionNode(
                        null,
                        variable,
                        typeNode,
                        originalVarDef.DefaultValue,
                        Array.Empty<DirectiveNode>()));
            }
        }
    }

    private static IReadOnlyList<string>? GetUnspecifiedArguments(ISelection selection)
    {
        List<string>? unspecifiedArguments = null;

        foreach (var argument in selection.Arguments)
        {
            if (argument.IsDefaultValue)
            {
                unspecifiedArguments ??= [];
                unspecifiedArguments.Add(argument.Name);
            }
        }

        return unspecifiedArguments;
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

    private sealed class VariableVisitorContext
    {
        public HashSet<VariableNode> VariableNodes { get; } = new(SyntaxComparer.BySyntax);
    }
}
