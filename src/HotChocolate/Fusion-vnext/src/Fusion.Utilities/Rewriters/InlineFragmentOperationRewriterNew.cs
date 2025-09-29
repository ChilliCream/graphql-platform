using System.Collections.Immutable;
using HotChocolate.Fusion.Planning;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Rewriters;

public sealed class InlineFragmentOperationRewriterNew(
    ISchemaDefinition schema,
#pragma warning disable CS9113 // Parameter is unread.
    bool removeStaticallyExcludedSelections = false)
#pragma warning restore CS9113 // Parameter is unread.
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
        var context = new Context(operationType, fragmentLookup);

        CollectSelections(operation.SelectionSet, context);

        var newSelectionSet = RewriteSelections(context);

        var newOperation = new OperationDefinitionNode(
            null,
            operation.Name,
            operation.Description,
            operation.Operation,
            operation.VariableDefinitions,
            RewriteDirectives(operation.Directives),
            newSelectionSet);

        return new DocumentNode(ImmutableArray<IDefinitionNode>.Empty.Add(newOperation));
    }

    private SelectionSetNode RewriteSelections(Context context)
    {
        var selections = new List<ISelectionNode>();
        Dictionary<Conditional, List<ISelectionNode>>? conditionals = null;

        foreach (var selection in context.Selections)
        {
            switch (selection)
            {
                case FieldNode fieldNode:
                {
                    var newFieldNode = RewriteField(fieldNode, context);

                    if (context.Conditionals.TryGetValue(fieldNode, out var conditional))
                    {
                        conditionals ??=
                            new Dictionary<Conditional, List<ISelectionNode>>(ConditionalComparer.Instance);

                        if (!conditionals.TryGetValue(conditional, out var conditionalSelections))
                        {
                            conditionalSelections = [];
                            conditionals.Add(conditional, conditionalSelections);
                        }

                        conditionalSelections.Add(newFieldNode);
                    }
                    else
                    {
                        selections.Add(newFieldNode);
                    }

                    break;
                }

                case InlineFragmentNode inlineFragmentNode:
                {
                    var newInlineFragmentNode = RewriteInlineFragment(inlineFragmentNode, context);

                    if (newInlineFragmentNode.SelectionSet.Selections.Count == 0)
                    {
                        continue;
                    }

                    if (context.Conditionals.TryGetValue(newInlineFragmentNode, out var conditional))
                    {
                        conditionals ??=
                            new Dictionary<Conditional, List<ISelectionNode>>(ConditionalComparer.Instance);

                        if (!conditionals.TryGetValue(conditional, out var conditionalSelections))
                        {
                            conditionalSelections = [];
                            conditionals.Add(conditional, conditionalSelections);
                        }

                        conditionalSelections.Add(newInlineFragmentNode);
                    }
                    else
                    {
                        selections.Add(newInlineFragmentNode);
                    }

                    break;
                }
            }
        }

        if (conditionals is not null)
        {
            foreach (var (conditional, conditionalSelections) in conditionals)
            {
                if (conditionalSelections is [FieldNode singleField])
                {
                    selections.Add(singleField.WithDirectives([..singleField.Directives, ..conditional.ToDirectives()]));
                }
                else if (conditionalSelections is [InlineFragmentNode inlineFragment])
                {
                    selections.Add(inlineFragment.WithDirectives([..inlineFragment.Directives, ..conditional.ToDirectives()]));
                }
                else
                {
                    var inlineFragmentNode = new InlineFragmentNode(
                        null,
                        null,
                        conditional.ToDirectives(),
                        new SelectionSetNode(conditionalSelections));

                    selections.Add(inlineFragmentNode);
                }
            }
        }

        if (selections.Count == 0)
        {
            selections.Add(s_typeNameField);
        }

        return new SelectionSetNode(selections);
    }

    private InlineFragmentNode RewriteInlineFragment(InlineFragmentNode inlineFragmentNode, Context context)
    {
        if (!context.Contexts.TryGetValue(inlineFragmentNode, out var fragmentContext))
        {
            throw new InvalidOperationException("Expected to have a fragment context.");
        }

        var newSelectionSet = RewriteSelections(fragmentContext);

        return inlineFragmentNode.WithSelectionSet(newSelectionSet);
    }

    private FieldNode RewriteField(FieldNode fieldNode, Context context)
    {
        if (fieldNode.SelectionSet is null)
        {
            return fieldNode;
        }

        if (!context.Contexts.TryGetValue(fieldNode, out var fieldContext))
        {
            throw new InvalidOperationException("Expected to have a field context.");
        }

        var newSelectionSet = RewriteSelections(fieldContext);

        return fieldNode.WithSelectionSet(newSelectionSet);
    }

    private void CollectSelections(SelectionSetNode selectionSet, Context context, Conditional? conditional = null)
    {
        foreach (var selection in selectionSet.Selections)
        {
            switch (selection)
            {
                case FieldNode field:
                    CollectField(field, context, conditional);
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

    private void CollectField(FieldNode fieldNode, Context context, Conditional? conditionalFromParent)
    {
        if (IsStaticallySkipped(fieldNode))
        {
            return;
        }

        var (conditional, directives) = DivideDirectives(fieldNode);

        conditional ??= conditionalFromParent;

        // executionAlteringDirectives = GetUniqueExecutionAlteringDirectives(executionAlteringDirectives, context);

        var cleanFieldNode = fieldNode
            .WithArguments(RewriteArguments(fieldNode.Arguments))
            .WithDirectives(directives ?? [])
            .WithLocation(null);

        var responseName = fieldNode.Alias?.Value ?? fieldNode.Name.Value;

        if (context.Fields.TryGetValue(responseName, out var existingFields))
        {
            // We have seen this responseName before.

            if (existingFields.TryGetValue(cleanFieldNode, out var existingField))
            {
                // We have seen the exact field node before.

                if (context.Conditionals.TryGetValue(existingField, out var existingConditional))
                {
                    // The existing field has conditionals.

                    if (conditional is not null)
                    {
                        // The new field also has conditionals, so we need to check if they match.

                        if (ConditionalComparer.Instance.Equals(conditional, existingConditional))
                        {
                            // The conditions are compatible, there is nothing to do.
                        }
                        else
                        {
                            throw new NotImplementedException("Can not handle different conditions yet");
                        }
                    }
                    else
                    {
                        // The new field has no conditionals.

                        context.Conditionals.Remove(existingField);

                        if (fieldNode.SelectionSet is null)
                        {
                            // The existing field doesn't have any sub-selections,
                            // so we can just keep the original selection and just remove its conditional.
                        }
                        else
                        {
                            // Since the existing field is conditional, we have to push its conditions down
                            // around the existing child selections.

                            if (!context.Contexts.TryGetValue(existingField, out var existingFieldContext))
                            {
                                throw new InvalidOperationException("Expected to have a field context.");
                            }

                            foreach (var selection in existingFieldContext.Selections)
                            {
                                existingFieldContext.Conditionals.Add(selection, existingConditional);
                            }

                            CollectSelections(fieldNode.SelectionSet, existingFieldContext);

                            return;
                        }
                    }
                }
                else
                {
                    // The existing field doesn't have conditionals.

                    if (conditional is not null)
                    {
                        // The new field has conditionals.

                        if (fieldNode.SelectionSet is null)
                        {
                            // The field doesn't have any sub-selections,
                            // so we can just keep the original selection without changing anything.
                        }
                        else
                        {
                            // Since the existing field doesn't have a conditional,
                            // we have to push these conditions down around
                            // the child selections of the new field.

                            if (!context.Contexts.TryGetValue(existingField, out var existingFieldContext))
                            {
                                throw new InvalidOperationException("Expected to have a field context.");
                            }

                            // TODO: Forwarding the conditional here isn't entirely correct
                            CollectSelections(fieldNode.SelectionSet, existingFieldContext, conditional);

                            return;
                        }
                    }
                    else
                    {
                        // The new field also doesn't have conditionals, so we can just leave the existing one in place.
                    }
                }
            }
            else
            {
                // It's a field node with different directives.

                existingFields.Add(cleanFieldNode);

                context.Selections.Add(cleanFieldNode);

                if (conditional is not null)
                {
                    context.Conditionals.Add(cleanFieldNode, conditional);
                }
            }
        }
        else
        {
            // This is the first time we're seeing this responseName.

            var set = new HashSet<FieldNode>(FieldNodeComparer.Instance)
            {
                cleanFieldNode
            };
            context.Fields.Add(responseName, set);

            context.Selections.Add(cleanFieldNode);

            if (conditional is not null)
            {
                context.Conditionals.Add(cleanFieldNode, conditional);
            }
        }

        if (fieldNode.SelectionSet is null)
        {
            return;
        }

        if (!context.Contexts.TryGetValue(cleanFieldNode, out var fieldContext))
        {
            var field = ((IComplexTypeDefinition)context.Type).Fields[fieldNode.Name.Value];
            var fieldType = field.Type.AsTypeDefinition();
            fieldContext = new Context(fieldType, context.Fragments);

            context.Contexts[cleanFieldNode] = fieldContext;
        }

        CollectSelections(fieldNode.SelectionSet, fieldContext);
    }

    private void CollectFragment(
        SelectionSetNode selectionSet,
        ITypeDefinition typeCondition,
        Conditional? conditional,
        IReadOnlyList<DirectiveNode>? otherDirectives,
        Context context)
    {
        // executionAlteringDirectives = GetUniqueExecutionAlteringDirectives(executionAlteringDirectives, context);

        var isTypeRefinement = !typeCondition.IsAssignableFrom(context.Type);
        var cleanedFragment = new InlineFragmentNode(
            null,
            isTypeRefinement ? new NamedTypeNode(typeCondition.Name) : null,
            otherDirectives ?? [],
            new SelectionSetNode([]));

        if (!isTypeRefinement && otherDirectives is null)
        {
            // We can directly inline the selections.
            CollectSelections(selectionSet, context, conditional);
        }
        else
        {
            if (context.TypeRefinements.TryGetValue(typeCondition.Name, out var existingRefinementsForType))
            {
                // We have seen this type refinement before.

                if (existingRefinementsForType.TryGetValue(cleanedFragment, out var existingFragment))
                {
                     // We have seen the exact type refinement before.

                    if (context.Conditionals.TryGetValue(existingFragment, out var existingConditional))
                    {
                        // The existing type refinement has conditionals.

                        if (conditional is not null)
                        {
                            // The new type refinement also has conditionals, so we need to check if they match.

                            if (ConditionalComparer.Instance.Equals(conditional, existingConditional))
                            {
                                // The conditions are compatible, there is nothing to do.
                            }
                            else
                            {
                                throw new NotImplementedException("Can not handle different conditions yet");
                            }
                        }
                        else
                        {
                            // The new type refinement has no conditionals.

                            context.Conditionals.Remove(cleanedFragment);

                            // Since the existing type refinement is conditional, we have to push its conditions down
                            // around the existing child selections.

                            if (!context.Contexts.TryGetValue(cleanedFragment, out var existingFragmentContext))
                            {
                                throw new InvalidOperationException("Expected to have a fragment context.");
                            }

                            foreach (var selection in existingFragmentContext.Selections)
                            {
                                existingFragmentContext.Conditionals.Add(selection, existingConditional);
                            }

                            CollectSelections(selectionSet, existingFragmentContext);

                            return;
                        }
                    }
                    else
                    {
                        // The existing field doesn't have conditionals.

                        if (conditional is not null)
                        {
                            // The new field has conditionals.
                            // Since the existing type refinement doesn't have a conditional,
                            // we have to push these conditions down around
                            // the child selections of the new field.

                            if (!context.Contexts.TryGetValue(cleanedFragment, out var existingFragmentContext))
                            {
                                throw new InvalidOperationException("Expected to have a fragment context.");
                            }

                            // TODO: Forwarding the conditional here isn't entirely correct
                            CollectSelections(selectionSet, existingFragmentContext, conditional);

                            return;
                        }
                        else
                        {
                            // The new type refinement also doesn't have conditionals,
                            // so we can just leave the existing one in place.
                        }
                    }
                }
                else
                {
                    // It's a type refinement with different directives.

                    existingRefinementsForType.Add(cleanedFragment);

                    context.Selections.Add(cleanedFragment);

                    if (conditional is not null)
                    {
                        context.Conditionals.Add(cleanedFragment, conditional);
                    }
                }
            }
            else
            {
                // We haven't seen this type refinement before.

                var set = new HashSet<InlineFragmentNode>(InlineFragmentNodeComparer.Instance)
                {
                    cleanedFragment
                };
                context.TypeRefinements.Add(typeCondition.Name, set);

                context.Selections.Add(cleanedFragment);

                if (conditional is not null)
                {
                    context.Conditionals.Add(cleanedFragment, conditional);
                }
            }

            if (!context.Contexts.TryGetValue(cleanedFragment, out var fragmentContext))
            {
                fragmentContext = new Context(typeCondition, context.Fragments);

                context.Contexts[cleanedFragment] = fragmentContext;
            }

            CollectSelections(selectionSet, fragmentContext);
        }
    }

    private void CollectInlineFragment(InlineFragmentNode inlineFragment, Context context)
    {
        if (IsStaticallySkipped(inlineFragment))
        {
            return;
        }

        var typeCondition = inlineFragment.TypeCondition is not null
            ? schema.Types[inlineFragment.TypeCondition.Name.Value]
            : context.Type;

        var (conditional, directives) = DivideDirectives(inlineFragment);

        CollectFragment(
            inlineFragment.SelectionSet,
            typeCondition,
            conditional,
            directives,
            context);
    }

    private void CollectFragmentSpread(FragmentSpreadNode fragmentSpread, Context context)
    {
        if (IsStaticallySkipped(fragmentSpread))
        {
            return;
        }

        var fragmentDefinition = context.GetFragmentDefinition(fragmentSpread.Name.Value);
        var typeCondition = schema.Types[fragmentDefinition.TypeCondition.Name.Value];

        // TODO: Since we're going to rewrite to an inline fragment, we need to kick out
        //       any directives that can not also apply to inline fragments.
        var (conditional, directives) = DivideDirectives(fragmentSpread);

        CollectFragment(
            fragmentDefinition.SelectionSet,
            typeCondition,
            conditional,
            directives,
            context);
    }

    private static IReadOnlyList<DirectiveNode>? GetUniqueExecutionAlteringDirectives(
        IReadOnlyList<DirectiveNode>? directives, Context context)
    {
        if (directives is null)
        {
            return null;
        }

        // if (context.ConditionalNodes is null)
        // {
        //     return directives;
        // }

        // TODO: Implement
        // directives = directives
        //     .Except(context.ExecutionAlteringDirectives, SyntaxComparer.BySyntax)
        //     .OfType<DirectiveNode>()
        //     .ToList();

        return directives.Count == 0 ? null : directives;
    }

    private static (Conditional? Conditional, IReadOnlyList<DirectiveNode>? Directives) DivideDirectives(
        IHasDirectives directiveProvider)
    {
        if (directiveProvider.Directives.Count == 0)
        {
            return (null, null);
        }

        Conditional? conditional = null;
        List<DirectiveNode>? directives = null;

        foreach (var directive in directiveProvider.Directives)
        {
            var rewrittenDirective = directive;

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
            buffer[i] = RewriteDirective(directives[0]);
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

    private class Context(
        ITypeDefinition type,
        Dictionary<string, FragmentDefinitionNode> fragments)
    {
        public Dictionary<ISelectionNode, Context> Contexts { get; } = new(SyntaxNodeComparer.Instance);

        public Dictionary<ISelectionNode, Conditional> Conditionals { get; } = new(SyntaxNodeComparer.Instance);

        /// <summary>
        /// Just a faster way to find a collection of nodes by their response name.
        /// </summary>
        public Dictionary<string, HashSet<FieldNode>> Fields { get; } = [];

        public Dictionary<string, HashSet<InlineFragmentNode>> TypeRefinements { get; } = [];

        public List<ISelectionNode> Selections { get; } = [];

        public ITypeDefinition Type { get; } = type;

        public Dictionary<string, FragmentDefinitionNode> Fragments { get; } = fragments;

        public FragmentDefinitionNode GetFragmentDefinition(string name)
            => Fragments[name];
    }

    private sealed class Conditional
    {
        public DirectiveNode? Skip { get; set; }

        public DirectiveNode? Include { get; set; }

        public IReadOnlyList<DirectiveNode> ToDirectives()
        {
            var builder = ImmutableArray.CreateBuilder<DirectiveNode>();

            if (Skip is not null)
            {
                builder.Add(Skip);
            }

            if (Include is not null)
            {
                builder.Add(Include);
            }

            return builder.ToImmutable();
        }
    }

    private sealed class ConditionalComparer : IEqualityComparer<Conditional>
    {
        private static readonly IEqualityComparer<ISyntaxNode> s_comparer = SyntaxComparer.BySyntax;

        public bool Equals(Conditional? x, Conditional? y)
        {
            return s_comparer.Equals(x?.Skip, y?.Skip) && s_comparer.Equals(x?.Include, y?.Include);
        }

        public int GetHashCode(Conditional obj)
        {
            return HashCode.Combine(GetDirectiveHashCode(obj.Skip), GetDirectiveHashCode(obj.Include));
        }

        private static int GetDirectiveHashCode(DirectiveNode? node)
        {
            return node is null ? 0 : s_comparer.GetHashCode(node);
        }

        public static ConditionalComparer Instance { get; } = new();
    }

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
}
