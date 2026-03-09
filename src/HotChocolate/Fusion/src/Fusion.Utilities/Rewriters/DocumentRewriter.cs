using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using HotChocolate.Fusion.Planning;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Rewriters;

public sealed class DocumentRewriter(ISchemaDefinition schema, bool removeStaticallyExcludedSelections = false)
{
    private static readonly FieldNode s_typeNameField =
        new FieldNode(
            null,
            new NameNode(IntrospectionFieldNames.TypeName),
            null,
            [new DirectiveNode("fusion__empty")],
            ImmutableArray<ArgumentNode>.Empty,
            null);

    public DocumentNode RewriteDocument(DocumentNode document, string? operationName = null)
    {
        var operation = document.GetOperation(operationName);
        var operationType = schema.GetOperationType(operation.Operation);
        var fragmentLookup = CreateFragmentLookup(document);

        var newSelectionSet = RewriteSelectionSet(
            operation.SelectionSet,
            operationType,
            fragmentLookup);

        var newOperation = new OperationDefinitionNode(
            null,
            operation.Name,
            operation.Description,
            operation.Operation,
            operation.VariableDefinitions,
            RewriteDirectives(operation.Directives),
            newSelectionSet);

        return new DocumentNode([newOperation]);
    }

    private SelectionSetNode RewriteSelectionSet(
        SelectionSetNode selectionSetNode,
        ITypeDefinition type,
        Dictionary<string, FragmentDefinitionNode>? fragmentLookup)
    {
        var context = new Context(null, null, type, null, fragmentLookup ?? []);

        CollectSelections(selectionSetNode, context);

        var newSelections = RewriteSelections(context) ?? [s_typeNameField];

        var newSelectionSetNode = new SelectionSetNode(newSelections);

        return newSelectionSetNode;
    }

    #region Collecting

    private void CollectSelections(SelectionSetNode selectionSet, Context context)
    {
        foreach (var selection in selectionSet.Selections)
        {
            if (removeStaticallyExcludedSelections && IsStaticallySkipped(selection))
            {
                continue;
            }

            switch (selection)
            {
                case FieldNode field:
                    CollectField(field, context);
                    break;

                case InlineFragmentNode inlineFragment:
                    CollectInlineFragment(inlineFragment, context);
                    break;

                case FragmentSpreadNode fragmentSpread:
                    CollectFragmentSpread(fragmentSpread, context);
                    break;
            }
        }
    }

    private void CollectField(FieldNode fieldNode, Context context)
    {
        var (conditional, directives) = DivideDirectives(
            fieldNode,
            Types.DirectiveLocation.Field);

        if (conditional is not null)
        {
            if (IsStaticallySkipped(conditional, context, out conditional))
            {
                return;
            }

            if (conditional is not null)
            {
                context = context.GetOrAddConditionalContext(conditional);
            }
        }

        fieldNode = new FieldNode(
            fieldNode.Name,
            fieldNode.Alias,
            directives ?? [],
            RewriteArguments(fieldNode.Arguments),
            fieldNode.SelectionSet);

        var fieldName = fieldNode.Name.Value;
        ITypeDefinition? fieldType = null;

        if (fieldNode.SelectionSet is not null && context.Type is IComplexTypeDefinition complexType)
        {
            var field = complexType.Fields[fieldName];

            fieldType = field.Type.AsTypeDefinition();
        }

        var fieldContext = GetOrAddContextForField(context, fieldNode, fieldType);

        if (fieldContext is not null && fieldNode.SelectionSet is not null)
        {
            CollectSelections(fieldNode.SelectionSet, fieldContext);
        }
    }

    private void CollectInlineFragment(InlineFragmentNode inlineFragment, Context context)
    {
        var typeCondition = inlineFragment.TypeCondition is not null
            ? schema.Types[inlineFragment.TypeCondition.Name.Value]
            : context.Type;

        var (conditional, directives) = DivideDirectives(
            inlineFragment,
            Types.DirectiveLocation.InlineFragment);

        CollectFragment(
            inlineFragment.SelectionSet,
            typeCondition,
            conditional,
            directives,
            context);
    }

    private void CollectFragmentSpread(FragmentSpreadNode fragmentSpread, Context context)
    {
        var fragmentDefinition = context.GetFragmentDefinition(fragmentSpread.Name.Value);
        var typeCondition = schema.Types[fragmentDefinition.TypeCondition.Name.Value];

        var (conditional, directives) = DivideDirectives(
            fragmentSpread,
            Types.DirectiveLocation.InlineFragment);

        CollectFragment(
            fragmentDefinition.SelectionSet,
            typeCondition,
            conditional,
            directives,
            context);
    }

    private void CollectFragment(
        SelectionSetNode selectionSet,
        ITypeDefinition typeCondition,
        Conditional? conditional,
        IReadOnlyList<DirectiveNode>? otherDirectives,
        Context context)
    {
        if (conditional is not null)
        {
            if (IsStaticallySkipped(conditional, context, out conditional))
            {
                return;
            }

            if (conditional is not null)
            {
                context = context.GetOrAddConditionalContext(conditional);
            }
        }

        var isTypeRefinement = !typeCondition.IsAssignableFrom(context.Type);

        var fragmentContext = context;
        if (isTypeRefinement || otherDirectives is not null)
        {
            var inlineFragment = new InlineFragmentNode(
                null,
                isTypeRefinement
                    ? new NamedTypeNode(typeCondition.Name)
                    : null,
                otherDirectives ?? [],
                selectionSet);

            fragmentContext = GetOrAddContextForFragment(context, inlineFragment, typeCondition);
        }

        CollectSelections(selectionSet, fragmentContext);
    }

    private static Context? GetOrAddContextForField(Context context, FieldNode fieldNode, ITypeDefinition? fieldType)
    {
        if (context.IsConditionalContext)
        {
            var unconditionalContext = context.UnconditionalContext;

            if (unconditionalContext.HasField(fieldNode, out var unconditionalFieldContext))
            {
                if (fieldNode.SelectionSet is null)
                {
                    return null;
                }

                if (unconditionalFieldContext is null)
                {
                    throw new InvalidOperationException("Expected to have a field context");
                }

                var conditionalContextBelowUnconditionalFieldContext =
                    RecreateConditionalContextHierarchy(unconditionalFieldContext, context);

                return conditionalContextBelowUnconditionalFieldContext;
            }

            if (!context.HasField(fieldNode, out var fieldContext))
            {
                fieldContext = context.AddField(fieldNode, fieldType);

                unconditionalContext.RecordReferenceInConditionalContext(fieldNode, context);
            }

            return fieldContext;
        }
        else
        {
            if (!context.HasField(fieldNode, out var fieldContext))
            {
                fieldContext = context.AddField(fieldNode, fieldType);
            }

            if (context.TryGetConditionalContextsWithReferences(fieldNode, out var conditionalContexts))
            {
                foreach (var conditionalContext in conditionalContexts)
                {
                    if (fieldContext is not null
                        && conditionalContext.HasField(fieldNode, out var conditionalFieldContext)
                        && conditionalFieldContext is not null)
                    {
                        var conditionalContextBelowUnconditionalField =
                            RecreateConditionalContextHierarchy(fieldContext, conditionalContext);

                        MergeContexts(conditionalFieldContext, conditionalContextBelowUnconditionalField);
                    }

                    conditionalContext.RemoveField(fieldNode);
                }

                context.RemoveReferenceToConditionalContext(fieldNode);
            }

            return fieldContext;
        }
    }

    private static Context GetOrAddContextForFragment(
        Context context,
        InlineFragmentNode inlineFragmentNode,
        ITypeDefinition typeCondition)
    {
        if (context.IsConditionalContext)
        {
            var unconditionalContext = context.UnconditionalContext;

            if (unconditionalContext.HasFragment(inlineFragmentNode, out var unconditionalFragmentContext))
            {
                var conditionalContextBelowUnconditionalFragmentContext =
                    RecreateConditionalContextHierarchy(unconditionalFragmentContext, context);

                return conditionalContextBelowUnconditionalFragmentContext;
            }

            if (!context.HasFragment(inlineFragmentNode, out var fragmentContext))
            {
                fragmentContext = context.AddFragment(inlineFragmentNode, typeCondition);

                unconditionalContext.RecordReferenceInConditionalContext(inlineFragmentNode, context);
            }

            return fragmentContext;
        }
        else
        {
            if (!context.HasFragment(inlineFragmentNode, out var fragmentContext))
            {
                fragmentContext = context.AddFragment(inlineFragmentNode, typeCondition);
            }

            if (context.TryGetConditionalContextsWithReferences(inlineFragmentNode, out var conditionalContexts))
            {
                foreach (var conditionalContext in conditionalContexts)
                {
                    if (conditionalContext.HasFragment(inlineFragmentNode,
                        out var conditionalFragmentContext))
                    {
                        var conditionalContextBelowUnconditionalFragment =
                            RecreateConditionalContextHierarchy(fragmentContext, conditionalContext);

                        MergeContexts(conditionalFragmentContext, conditionalContextBelowUnconditionalFragment);
                    }

                    conditionalContext.RemoveFragment(inlineFragmentNode);
                }

                context.RemoveReferenceToConditionalContext(inlineFragmentNode);
            }

            return fragmentContext;
        }
    }

    /// <summary>
    /// Rebuilds the conditional directive hierarchy of <paramref name="sourceContext"/> into
    /// <paramref name="targetContext"/>, returning the innermost, rebuilt conditional context.
    /// </summary>
    private static Context RecreateConditionalContextHierarchy(Context targetContext, Context sourceContext)
    {
        var conditionalStack = new Stack<Context>();
        var current = sourceContext;

        while (current?.IsConditionalContext == true)
        {
            conditionalStack.Push(current);
            current = current.Parent;
        }

        while (conditionalStack.TryPop(out var conditionalContext))
        {
            targetContext = targetContext.GetOrAddConditionalContext(conditionalContext.Conditional!);
        }

        return targetContext;
    }

    private static void MergeContexts(Context source, Context target)
    {
        if (source.Conditionals is not null)
        {
            foreach (var (conditional, conditionalContext) in source.Conditionals)
            {
                var targetConditionalContext = target.GetOrAddConditionalContext(conditional);

                MergeContexts(conditionalContext, targetConditionalContext);
            }
        }

        if (source.Fields is not null)
        {
            foreach (var (_, fieldContextLookup) in source.Fields)
            {
                foreach (var (fieldNode, fieldContext) in fieldContextLookup)
                {
                    if (!target.HasField(fieldNode, out var targetFieldContext))
                    {
                        targetFieldContext = GetOrAddContextForField(target, fieldNode, fieldContext?.Type);
                    }

                    if (fieldContext is not null && targetFieldContext is not null)
                    {
                        MergeContexts(fieldContext, targetFieldContext);
                    }
                }
            }
        }

        if (source.Fragments is not null)
        {
            foreach (var (_, fragmentContextLookup) in source.Fragments)
            {
                foreach (var (inlineFragmentNode, fragmentContext) in fragmentContextLookup)
                {
                    if (!target.HasFragment(inlineFragmentNode, out var targetFragmentContext))
                    {
                        targetFragmentContext = GetOrAddContextForFragment(
                            target,
                            inlineFragmentNode,
                            fragmentContext.Type);
                    }

                    MergeContexts(fragmentContext, targetFragmentContext);
                }
            }
        }
    }

    private (Conditional? Conditional, IReadOnlyList<DirectiveNode>? Directives) DivideDirectives(
        IHasDirectives directiveProvider,
        Types.DirectiveLocation targetLocation)
    {
        if (directiveProvider.Directives.Count == 0)
        {
            return (null, null);
        }

        Conditional? conditional = null;
        List<DirectiveNode>? directives = null;

        foreach (var directive in directiveProvider.Directives)
        {
            if (schema.DirectiveDefinitions.TryGetDirective(directive.Name.Value, out var directiveDefinition)
                && !directiveDefinition.Locations.HasFlag(targetLocation))
            {
                continue;
            }

            var rewrittenDirective = RewriteDirective(directive);

            if (directive.Name.Value.Equals(DirectiveNames.Skip.Name, StringComparison.Ordinal))
            {
                if (directive.Arguments is [{ Value: BooleanValueNode }])
                {
                    continue;
                }

                conditional ??= new Conditional();
                conditional.Skip = rewrittenDirective;

                continue;
            }

            if (directive.Name.Value.Equals(DirectiveNames.Include.Name, StringComparison.Ordinal))
            {
                if (directive.Arguments is [{ Value: BooleanValueNode }])
                {
                    continue;
                }

                conditional ??= new Conditional();
                conditional.Include = rewrittenDirective;

                continue;
            }

            if (directive.Name.Value.Equals(DirectiveNames.Defer.Name, StringComparison.Ordinal))
            {
                var ifArgument = directive.Arguments
                    .FirstOrDefault(a => a.Name.Value.Equals("if", StringComparison.Ordinal));

                if (ifArgument?.Value is BooleanValueNode { Value: false })
                {
                    continue;
                }
            }

            directives ??= [];
            directives.Add(rewrittenDirective);
        }

        return (conditional, directives);
    }

    /// <summary>
    /// Checks whether a parent context already contains an opposite conditional part
    /// with the same variable value, e.g. <c>@skip(if: $value) :: @include(if: $value)</c>.
    /// <br/>
    /// If that's the case, the child conditional will always be skipped, so we can statically
    /// skip the selection this conditional is on.
    /// <br/>
    /// Additionally, we check whether a part of the conditional already exists in a parent context.
    /// <br/>
    /// In a hierarchy like <c>@skip(if: $skip) -> something -> @skip(if: $skip)</c>,
    /// we can get rid of the second @skip, since it will always be included.
    /// </summary>
    private static bool IsStaticallySkipped(
        Conditional conditional,
        Context context,
        out Conditional? newConditional)
    {
        newConditional = conditional;

        var current = context;
        do
        {
            if (current.Conditional is { } parentConditional)
            {
                if (parentConditional.Skip is not null)
                {
                    if (newConditional.Skip?.Equals(parentConditional.Skip, SyntaxComparison.Syntax) == true)
                    {
                        // If the parent has exactly the same @skip, we can remove the new one.
                        newConditional.Skip = null;
                    }
                    else if (ConditionalDirectiveHasSameVariable(parentConditional.Skip, newConditional.Include))
                    {
                        // If the parent has a @skip with the same variable as the new @include,
                        // the new @include will never be included, so the selection its on
                        // can be statically removed.
                        return true;
                    }
                }

                if (parentConditional.Include is not null)
                {
                    if (newConditional.Include?.Equals(parentConditional.Include, SyntaxComparison.Syntax) == true)
                    {
                        // If the parent has exactly the same @include, we can remove the new one.
                        newConditional.Include = null;
                    }
                    else if (ConditionalDirectiveHasSameVariable(parentConditional.Include, newConditional.Skip))
                    {
                        // If the parent has a @include with the same variable as the new @skip,
                        // the new @skip will never be included, so the selection its on
                        // can be statically removed.
                        return true;
                    }
                }

                if (newConditional.Skip is null && newConditional.Include is null)
                {
                    // Both of the @skip and @include in the new conditional have already
                    // appeared on a parent, so we can get rid of the entire conditional.
                    newConditional = null;
                    break;
                }
            }

            current = current.Parent;
        } while (current is not null);

        return false;
    }

    private static bool ConditionalDirectiveHasSameVariable(DirectiveNode directive1, DirectiveNode? directive2)
    {
        if (directive2 is null)
        {
            return false;
        }

        var if1 = directive1.Arguments[0];
        var if2 = directive2.Arguments[0];

        return if1.Value.Equals(if2.Value, SyntaxComparison.Syntax);
    }

    private static bool IsStaticallySkipped(IHasDirectives directiveProvider)
    {
        if (directiveProvider.Directives.Count == 0)
        {
            return false;
        }

        foreach (var directive in directiveProvider.Directives)
        {
            if (directive.Name.Value.Equals(DirectiveNames.Skip.Name, StringComparison.Ordinal)
                && directive.Arguments is [{ Value: BooleanValueNode { Value: true } }])
            {
                return true;
            }

            if (directive.Name.Value.Equals(DirectiveNames.Include.Name, StringComparison.Ordinal)
                && directive.Arguments is [{ Value: BooleanValueNode { Value: false } }])
            {
                return true;
            }
        }

        return false;
    }

    private static Dictionary<string, FragmentDefinitionNode> CreateFragmentLookup(DocumentNode document)
    {
        var lookup = new Dictionary<string, FragmentDefinitionNode>();

        foreach (var definition in document.Definitions)
        {
            if (definition is FragmentDefinitionNode fragmentDef)
            {
                lookup.Add(fragmentDef.Name.Value, fragmentDef);
            }
        }

        return lookup;
    }

    #endregion

    #region Rewriting

    private List<ISelectionNode>? RewriteSelections(Context context)
    {
        List<ISelectionNode>? selections = null;

        if (context.Fields is not null)
        {
            foreach (var (_, fieldContextLookup) in context.Fields)
            {
                foreach (var (fieldNode, fieldContext) in fieldContextLookup)
                {
                    var newFieldNode = RewriteField(fieldNode, fieldContext);

                    if (newFieldNode is null)
                    {
                        continue;
                    }

                    selections ??= [];
                    selections.Add(newFieldNode);
                }
            }
        }

        if (context.Fragments is not null)
        {
            foreach (var (_, fragmentContextLookup) in context.Fragments)
            {
                foreach (var (inlineFragmentNode, fragmentContext) in fragmentContextLookup)
                {
                    var newInlineFragmentNode = RewriteInlineFragment(
                        inlineFragmentNode,
                        fragmentContext);

                    if (newInlineFragmentNode is null)
                    {
                        continue;
                    }

                    selections ??= [];
                    selections.Add(newInlineFragmentNode);
                }
            }
        }

        if (context.Conditionals is not null)
        {
            foreach (var (conditional, conditionalContext) in context.Conditionals)
            {
                var conditionalSelection = RewriteConditional(conditional, conditionalContext);

                if (conditionalSelection is null)
                {
                    continue;
                }

                selections ??= [];
                selections.Add(conditionalSelection);
            }
        }

        return selections;
    }

    private ISelectionNode? RewriteConditional(
        Conditional conditional,
        Context context)
    {
        var conditionalSelections = RewriteSelections(context);

        if (conditionalSelections is null)
        {
            return null;
        }

        var conditionalDirectives = conditional.ToDirectives();

        // If we only have a single selection and this selection does not have directives of its own,
        // we can push the conditional directives down on it.
        // Otherwise we return an inline fragment with all the conditional selections.
        return conditionalSelections switch
        {
            [FieldNode { Directives.Count: 0 } fieldNode] => fieldNode
                .WithDirectives([.. fieldNode.Directives, .. conditionalDirectives]),
            [InlineFragmentNode { Directives.Count: 0 } inlineFragmentNode] => inlineFragmentNode
                .WithDirectives([.. inlineFragmentNode.Directives, .. conditionalDirectives]),
            _ => new InlineFragmentNode(
                null,
                null,
                conditionalDirectives.ToArray(),
                new SelectionSetNode(conditionalSelections))
        };
    }

    private FieldNode? RewriteField(
        FieldNode fieldNode,
        Context? fieldContext)
    {
        if (fieldNode.SelectionSet is null)
        {
            return fieldNode;
        }

        if (fieldContext is null)
        {
            throw new InvalidOperationException("Expected to have field context");
        }

        var fieldSelections = RewriteSelections(fieldContext);

        if (fieldSelections is null)
        {
            if (!removeStaticallyExcludedSelections)
            {
                return null;
            }

            fieldSelections = [s_typeNameField];
        }

        return fieldNode.WithSelectionSet(new SelectionSetNode(fieldSelections));
    }

    private InlineFragmentNode? RewriteInlineFragment(
        InlineFragmentNode inlineFragmentNode,
        Context fragmentContext)
    {
        var fragmentSelections = RewriteSelections(fragmentContext);

        if (fragmentSelections is null)
        {
            if (!removeStaticallyExcludedSelections)
            {
                return null;
            }

            fragmentSelections = [s_typeNameField];
        }

        return inlineFragmentNode.WithSelectionSet(new SelectionSetNode(fragmentSelections));
    }

    private static IReadOnlyList<DirectiveNode> RewriteDirectives(IReadOnlyList<DirectiveNode> directives)
    {
        if (directives.Count == 0)
        {
            return directives;
        }

        if (directives.Count == 1)
        {
            return ImmutableArray<DirectiveNode>.Empty.Add(RewriteDirective(directives[0]));
        }

        var buffer = new DirectiveNode[directives.Count];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = RewriteDirective(directives[i]);
        }

        return ImmutableArray.Create(buffer);
    }

    private static DirectiveNode RewriteDirective(DirectiveNode directive)
    {
        return new DirectiveNode(directive.Name.Value, RewriteArguments(directive.Arguments));
    }

    private static IReadOnlyList<ArgumentNode> RewriteArguments(IReadOnlyList<ArgumentNode> arguments)
    {
        if (arguments.Count == 0)
        {
            return arguments;
        }

        if (arguments.Count == 1)
        {
            return ImmutableArray<ArgumentNode>.Empty.Add(arguments[0].WithLocation(null));
        }

        var buffer = new ArgumentNode[arguments.Count];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = arguments[i].WithLocation(null);
        }

        return ImmutableArray.Create(buffer);
    }

    #endregion

    [DebuggerDisplay(
        "{Type.Name}, Fields: {Fields?.Count}, Fragments: {Fragments?.Count}, Conditionals: {Conditionals?.Count}")]
    private sealed class Context(
        Context? parent,
        Context? unconditionalContext,
        ITypeDefinition type,
        Conditional? conditional,
        Dictionary<string, FragmentDefinitionNode> fragmentLookup)
    {
        /// <summary>
        /// Points to the parent of this context.
        /// Null for the root context.
        /// </summary>
        public Context? Parent { get; set; } = parent;

        /// <summary>
        /// The type selections in this context are on.
        /// </summary>
        public ITypeDefinition Type { get; } = type;

        [MemberNotNullWhen(true, nameof(Conditional))]
        [MemberNotNullWhen(true, nameof(UnconditionalContext))]
        public bool IsConditionalContext { get; } = conditional is not null;

        /// <summary>
        /// Contains the conditional, if this context is conditional.
        /// </summary>
        public Conditional? Conditional { get; } = conditional;

        /// <summary>
        /// If this context is conditional, this points to the unconditional version
        /// of the context.
        /// </summary>
        public Context? UnconditionalContext { get; } = unconditionalContext;

        /// <summary>
        /// The context for a specific <see cref="DocumentRewriter.Conditional"/>.
        /// </summary>
        public Dictionary<Conditional, Context>? Conditionals { get; private set; }

        /// <summary>
        /// Provides a way to find all conditional contexts a given selection node is referenced in.
        /// </summary>
        private Dictionary<ISelectionNode, List<Context>>? ReferencesInConditionalContexts { get; set; }

        /// <summary>
        /// Provides a fast way to get all FieldNodes for the same response name.
        /// The key is the response name.
        /// </summary>
        public Dictionary<string, Dictionary<FieldNode, Context?>>? Fields { get; private set; }

        /// <summary>
        /// Provides a fast way to get all InlineFragmentNodes of the same type refinement.
        /// The key is the name of the type being refined to or an empty string
        /// for an inline fragment without type refinement.
        /// </summary>
        public Dictionary<string, Dictionary<InlineFragmentNode, Context>>? Fragments { get; private set; }

        public FragmentDefinitionNode GetFragmentDefinition(string name)
            => fragmentLookup[name];

        public Context GetOrAddConditionalContext(Conditional conditional)
        {
            Conditionals ??= [];

            if (!Conditionals.TryGetValue(conditional, out var conditionalContext))
            {
                conditionalContext = new Context(
                    this,
                    GetUnconditionalContext(),
                    Type,
                    conditional,
                    fragmentLookup);

                Conditionals[conditional] = conditionalContext;
            }

            return conditionalContext;
        }

        /// <summary>
        /// Records that <paramref name="selectionNode"/> is referenced in <paramref name="conditionalContext"/>,
        /// so we can later quickly jump there.
        /// </summary>
        public void RecordReferenceInConditionalContext(ISelectionNode selectionNode, Context conditionalContext)
        {
            ReferencesInConditionalContexts ??=
                new Dictionary<ISelectionNode, List<Context>>(SyntaxNodeComparer.Instance);

            if (!ReferencesInConditionalContexts.TryGetValue(selectionNode, out var conditionalContexts))
            {
                conditionalContexts = [];
                ReferencesInConditionalContexts[selectionNode] = conditionalContexts;
            }

            conditionalContexts.Add(conditionalContext);
        }

        public bool TryGetConditionalContextsWithReferences(
            ISelectionNode selectionNode,
            [NotNullWhen(true)] out List<Context>? conditionalContexts)
        {
            conditionalContexts = null;

            if (ReferencesInConditionalContexts is null)
            {
                return false;
            }

            return ReferencesInConditionalContexts.TryGetValue(selectionNode, out conditionalContexts);
        }

        public void RemoveReferenceToConditionalContext(ISelectionNode selectionNode)
        {
            if (ReferencesInConditionalContexts is null)
            {
                return;
            }

            ReferencesInConditionalContexts.Remove(selectionNode);
        }

        public bool HasField(FieldNode fieldNode, out Context? fieldContext)
        {
            fieldContext = null;

            if (Fields is null)
            {
                return false;
            }

            var responseName = fieldNode.Alias?.Value ?? fieldNode.Name.Value;

            if (!Fields.TryGetValue(responseName, out var existingFieldContextLookup))
            {
                return false;
            }

            return existingFieldContextLookup.TryGetValue(fieldNode, out fieldContext);
        }

        public Context? AddField(FieldNode fieldNode, ITypeDefinition? fieldType)
        {
            Context? fieldContext = null;

            if (fieldNode.SelectionSet is not null && fieldType is not null)
            {
                fieldContext = new Context(
                    this,
                    GetUnconditionalContext(),
                    fieldType,
                    null,
                    fragmentLookup);
            }

            AddField(fieldNode, fieldContext);

            return fieldContext;
        }

        public void AddField(FieldNode fieldNode, Context? fieldContext)
        {
            Fields ??= [];

            var responseName = fieldNode.Alias?.Value ?? fieldNode.Name.Value;

            if (!Fields.TryGetValue(responseName, out var existingFieldContextLookup))
            {
                existingFieldContextLookup = new Dictionary<FieldNode, Context?>(FieldNodeComparer.Instance);
                Fields[responseName] = existingFieldContextLookup;
            }

            existingFieldContextLookup.Add(fieldNode, fieldContext);
        }

        public void RemoveField(FieldNode fieldNode)
        {
            if (Fields is null)
            {
                return;
            }

            var responseName = fieldNode.Alias?.Value ?? fieldNode.Name.Value;

            if (!Fields.TryGetValue(responseName, out var existingFieldContextLookup))
            {
                return;
            }

            existingFieldContextLookup.Remove(fieldNode);
        }

        public bool HasFragment(
            InlineFragmentNode inlineFragmentNode,
            [NotNullWhen(true)] out Context? fragmentContext)
        {
            fragmentContext = null;

            if (Fragments is null)
            {
                return false;
            }

            var typeName = inlineFragmentNode.TypeCondition?.Name.Value ?? string.Empty;

            if (!Fragments.TryGetValue(typeName, out var existingFragmentContextLookup))
            {
                return false;
            }

            return existingFragmentContextLookup.TryGetValue(inlineFragmentNode, out fragmentContext);
        }

        public Context AddFragment(InlineFragmentNode inlineFragmentNode, ITypeDefinition typeCondition)
        {
            var fragmentContext = new Context(
                this,
                GetUnconditionalContext(),
                typeCondition,
                null,
                fragmentLookup);

            AddFragment(inlineFragmentNode, fragmentContext);

            return fragmentContext;
        }

        public void AddFragment(InlineFragmentNode inlineFragmentNode, Context fragmentContext)
        {
            Fragments ??= [];

            var typeName = inlineFragmentNode.TypeCondition?.Name.Value ?? string.Empty;

            if (!Fragments.TryGetValue(typeName, out var existingFragmentContextLookup))
            {
                existingFragmentContextLookup =
                    new Dictionary<InlineFragmentNode, Context>(InlineFragmentNodeComparer.Instance);
                Fragments[typeName] = existingFragmentContextLookup;
            }

            existingFragmentContextLookup.Add(inlineFragmentNode, fragmentContext);
        }

        public void RemoveFragment(InlineFragmentNode inlineFragmentNode)
        {
            if (Fragments is null)
            {
                return;
            }

            var typeName = inlineFragmentNode.TypeCondition?.Name.Value ?? string.Empty;

            if (!Fragments.TryGetValue(typeName, out var existingFragmentContextLookup))
            {
                return;
            }

            existingFragmentContextLookup.Remove(inlineFragmentNode);
        }

        private Context GetUnconditionalContext()
        {
            if (IsConditionalContext)
            {
                return UnconditionalContext;
            }

            return this;
        }
    }

    /// <summary>
    /// Holds a combination of @skip and @include.
    /// </summary>
    private sealed class Conditional
    {
        private static readonly IEqualityComparer<ISyntaxNode> s_comparer = SyntaxComparer.BySyntax;

        public DirectiveNode? Skip { get; set; }

        public DirectiveNode? Include { get; set; }

        public IEnumerable<DirectiveNode> ToDirectives()
        {
            if (Skip is not null)
            {
                yield return Skip;
            }

            if (Include is not null)
            {
                yield return Include;
            }
        }

        public override bool Equals(object? obj)
        {
            if (obj is not Conditional other)
            {
                return false;
            }

            return s_comparer.Equals(Skip, other.Skip) && s_comparer.Equals(Include, other.Include);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(GetDirectiveHashCode(Skip), GetDirectiveHashCode(Include));
        }

        public override string ToString()
        {
            var skipDirective = Skip?.ToString();
            var includeDirective = Include?.ToString();

            if (skipDirective is not null && includeDirective is not null)
            {
                return $"{skipDirective} {includeDirective}";
            }

            if (skipDirective is not null)
            {
                return skipDirective;
            }

            if (includeDirective is not null)
            {
                return includeDirective;
            }

            throw new InvalidOperationException();
        }

        private static int GetDirectiveHashCode(DirectiveNode? node)
        {
            return node is null ? 0 : s_comparer.GetHashCode(node);
        }
    }

    #region Comparers

    private sealed class SyntaxNodeComparer : IEqualityComparer<ISyntaxNode>
    {
        public bool Equals(ISyntaxNode? x, ISyntaxNode? y)
        {
            if (x is FieldNode xField && y is FieldNode yField)
            {
                return FieldNodeComparer.Instance.Equals(xField, yField);
            }

            if (x is InlineFragmentNode xFragment && y is InlineFragmentNode yFragment)
            {
                return InlineFragmentNodeComparer.Instance.Equals(xFragment, yFragment);
            }

            return false;
        }

        public int GetHashCode(ISyntaxNode obj)
        {
            if (obj is FieldNode field)
            {
                return FieldNodeComparer.Instance.GetHashCode(field);
            }

            if (obj is InlineFragmentNode inlineFragment)
            {
                return InlineFragmentNodeComparer.Instance.GetHashCode(inlineFragment);
            }

            throw new NotImplementedException();
        }

        public static SyntaxNodeComparer Instance { get; } = new();
    }

    private sealed class InlineFragmentNodeComparer : IEqualityComparer<InlineFragmentNode>
    {
        public bool Equals(InlineFragmentNode? x, InlineFragmentNode? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null)
            {
                return false;
            }

            if (y is null)
            {
                return false;
            }

            return SyntaxComparer.BySyntax.Equals(x.TypeCondition, y.TypeCondition)
                && Equals(x.Directives, y.Directives);
        }

        private bool Equals(IReadOnlyList<ISyntaxNode> a, IReadOnlyList<ISyntaxNode> b)
        {
            if (a.Count == 0 && b.Count == 0)
            {
                return true;
            }

            return a.SequenceEqual(b, SyntaxComparer.BySyntax);
        }

        public int GetHashCode(InlineFragmentNode obj)
        {
            var hashCode = new HashCode();

            if (obj.TypeCondition is not null)
            {
                hashCode.Add(obj.TypeCondition.Name.Value);
            }

            for (var i = 0; i < obj.Directives.Count; i++)
            {
                hashCode.Add(SyntaxComparer.BySyntax.GetHashCode(obj.Directives[i]));
            }

            return hashCode.ToHashCode();
        }

        public static InlineFragmentNodeComparer Instance { get; } = new();
    }

    private sealed class FieldNodeComparer : IEqualityComparer<FieldNode>
    {
        public bool Equals(FieldNode? x, FieldNode? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null)
            {
                return false;
            }

            if (y is null)
            {
                return false;
            }

            return Equals(x.Alias, y.Alias)
                && x.Name.Equals(y.Name)
                && Equals(x.Directives, y.Directives)
                && Equals(x.Arguments, y.Arguments);
        }

        private bool Equals(IReadOnlyList<ISyntaxNode> a, IReadOnlyList<ISyntaxNode> b)
        {
            if (a.Count == 0 && b.Count == 0)
            {
                return true;
            }

            return a.SequenceEqual(b, SyntaxComparer.BySyntax);
        }

        public int GetHashCode(FieldNode obj)
        {
            var hashCode = new HashCode();

            if (obj.Alias is not null)
            {
                hashCode.Add(obj.Alias.Value);
            }

            hashCode.Add(obj.Name.Value);

            for (var i = 0; i < obj.Directives.Count; i++)
            {
                hashCode.Add(SyntaxComparer.BySyntax.GetHashCode(obj.Directives[i]));
            }

            for (var i = 0; i < obj.Arguments.Count; i++)
            {
                hashCode.Add(SyntaxComparer.BySyntax.GetHashCode(obj.Arguments[i]));
            }

            return hashCode.ToHashCode();
        }

        public static FieldNodeComparer Instance { get; } = new();
    }

    #endregion
}
