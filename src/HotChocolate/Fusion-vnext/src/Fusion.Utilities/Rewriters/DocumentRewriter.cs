using System.Collections.Immutable;
using HotChocolate.Fusion.Planning;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Rewriters;

internal sealed class DocumentRewriter(ISchemaDefinition schema, bool removeStaticallyExcludedSelections = false)
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

        return null!;

        // var context = new Context(null, operationType, fragmentLookup);
        //
        // CollectSelections(operation.SelectionSet, context);
        //
        // var newSelections = RewriteSelections(context);
        //
        // // TODO: Handle empty case better
        // var newSelectionSet = new SelectionSetNode(newSelections ?? []);
        //
        // var newOperation = new OperationDefinitionNode(
        //     null,
        //     operation.Name,
        //     operation.Description,
        //     operation.Operation,
        //     operation.VariableDefinitions,
        //     RewriteDirectives(operation.Directives),
        //     newSelectionSet);
        //
        // return new DocumentNode([newOperation]);
    }

    public SelectionSetNode RewriteSelectionSet(
        SelectionSetNode selectionSet,
        ISelectionSetMergeObserver mergeObserver)
    {
        throw new NotImplementedException();
    }

    #region Collecting
    #endregion

    #region Rewriting
    private List<ISelectionNode>? RewriteSelections(Context context)
    {
        List<ISelectionNode>? selections = null;

        if (context.ResponseNameToFieldLookup is not null)
        {
            // TODO: Maybe we still need to order this by key manually
            foreach (var (_, fieldNodes) in context.ResponseNameToFieldLookup)
            {
                foreach (var fieldNode in fieldNodes)
                {
                    var newFieldNode = RewriteField(fieldNode, context);

                    if (newFieldNode is null)
                    {
                        continue;
                    }

                    selections ??= [];
                    selections.Add(newFieldNode);
                }
            }
        }

        if (context.TypeNameToFragmentLookup is not null)
        {
            // TODO: Maybe we still need to order this by key manually
            foreach (var (_, inlineFragmentNodes) in context.TypeNameToFragmentLookup)
            {
                foreach (var inlineFragmentNode in inlineFragmentNodes)
                {
                    var newInlineFragmentNode = RewriteInlineFragment(inlineFragmentNode, context);

                    if (newInlineFragmentNode is null)
                    {
                        continue;
                    }

                    selections ??= [];
                    selections.Add(newInlineFragmentNode);
                }
            }
        }

        if (context.ConditionalContexts is not null)
        {
            foreach (var (conditional, conditionalContext) in context.ConditionalContexts)
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

    private ISelectionNode? RewriteConditional(Conditional conditional, Context context)
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
                .WithDirectives([..fieldNode.Directives, ..conditionalDirectives]),
            [InlineFragmentNode { Directives.Count: 0 } inlineFragmentNode] => inlineFragmentNode
                .WithDirectives([..inlineFragmentNode.Directives, ..conditionalDirectives]),
            _ => new InlineFragmentNode(
                null,
                null,
                conditionalDirectives.ToArray(),
                new SelectionSetNode(conditionalSelections))
        };
    }

    private FieldNode? RewriteField(FieldNode fieldNode, Context context)
    {
        if (fieldNode.SelectionSet is null)
        {
            return fieldNode;
        }

        if (context.FieldContexts is null
            || !context.FieldContexts.TryGetValue(fieldNode, out var fieldContext))
        {
            throw new InvalidOperationException("Expected to have a field context.");
        }

        var fieldSelections = RewriteSelections(fieldContext);

        if (fieldSelections is null)
        {
            // TODO: Is this right?
            if (!removeStaticallyExcludedSelections)
            {
                return null;
            }

            fieldSelections = [s_typeNameField];
        }

        return fieldNode.WithSelectionSet(new SelectionSetNode(fieldSelections));
    }

    private InlineFragmentNode? RewriteInlineFragment(InlineFragmentNode inlineFragmentNode, Context context)
    {
        if (context.FragmentContexts is null
            || !context.FragmentContexts.TryGetValue(inlineFragmentNode, out var fragmentContext))
        {
            throw new InvalidOperationException("Expected to have a fragment context.");
        }

        var fragmentSelections = RewriteSelections(fragmentContext);

        if (fragmentSelections is null)
        {
            // TODO: Is this right?
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
    #endregion

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

    private sealed class Context(ITypeDefinition type)
    {
        /// <summary>
        /// The type selections in this context are on.
        /// </summary>
        public ITypeDefinition Type { get; } = type;

        /// <summary>
        /// Points to the parent of this context.
        /// Null for the root context.
        /// </summary>
        public Context? Parent { get; }

        #region Conditional
        /// <summary>
        /// Contains the conditional, if this context is conditional.
        /// </summary>
        public Conditional? Conditional { get; }

        /// <summary>
        /// If this context is conditional, this points to the unconditional version
        /// of the context.
        /// </summary>
        public Context? UnconditionalContext { get; }

        /// <summary>
        /// The context for a specific <see cref="HotChocolate.Fusion.Rewriters.DocumentRewriter.Conditional"/>.
        /// </summary>
        public Dictionary<Conditional, Context>? ConditionalContexts { get; }
        #endregion

        #region Fields
        /// <summary>
        /// Provides a fast way to get all FieldNodes for the same response name.
        /// The key is the respones name.
        /// </summary>
        public Dictionary<string, HashSet<FieldNode>>? ResponseNameToFieldLookup { get; }

        /// <summary>
        /// The context for a specific FieldNode with a SelectionSet.
        /// </summary>
        public Dictionary<FieldNode, Context>? FieldContexts { get; }
        #endregion

        #region Fragments
        /// <summary>
        /// Provides a fast way to get all InlineFragmentNodes of the same type refinement.
        /// The key is the name of the type being refined to or an empty string
        /// for an inline fragment without type refinement.
        /// </summary>
        public Dictionary<string, HashSet<InlineFragmentNode>>? TypeNameToFragmentLookup { get; }

        /// <summary>
        /// The context for a specific InlineFragmentNode.
        /// </summary>
        public Dictionary<InlineFragmentNode, Context>? FragmentContexts { get; }
        #endregion
    }

    /// <summary>
    /// Holds a combination of @skip and @include.
    /// </summary>
    private sealed record Conditional
    {
        private static readonly IEqualityComparer<ISyntaxNode> s_comparer = SyntaxComparer.BySyntax;

        public DirectiveNode? Skip { get; init; }

        public DirectiveNode? Include { get; init; }

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

        public bool Equals(Conditional? other)
        {
            return s_comparer.Equals(Skip, other?.Skip) && s_comparer.Equals(Include, other?.Include);
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
