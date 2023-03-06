using HotChocolate.Language;
using HotChocolate.Language.Visitors;
using HotChocolate.Utilities;

namespace HotChocolate.Fusion.Metadata;

internal sealed class ResolverDefinition
{
    private static readonly ResolverRewriter _rewriter = new();
    private readonly FieldNode? _field;

    public ResolverDefinition(
        string subgraphName,
        ResolverKind kind,
        SelectionSetNode select,
        FragmentSpreadNode? placeholder,
        IReadOnlyList<string> requires)
    {
        SubgraphName = subgraphName;
        Kind = kind;
        Select = select;
        Placeholder = placeholder;
        Requires = requires;

        if (select.Selections is [FieldNode field])
        {
            _field = field;
        }
    }

    /// <summary>
    /// Gets the schema to which the type system member is bound to.
    /// </summary>
    public string SubgraphName { get; }

    public ResolverKind Kind { get; }

    public SelectionSetNode Select { get; }

    public FragmentSpreadNode? Placeholder { get; }

    public IReadOnlyList<string> Requires { get; }

    public (ISelectionNode selectionNode, IReadOnlyList<string> Path) CreateSelection(
        IReadOnlyDictionary<string, IValueNode> variables,
        SelectionSetNode? selectionSet,
        string? responseName)
    {
        var context = new FetchRewriterContext(Placeholder, variables, selectionSet, responseName);
        var selection = _rewriter.Rewrite(_field ?? (ISyntaxNode)Select, context);

        if (Placeholder is null && selectionSet is not null)
        {
            if (selection is not FieldNode fieldNode)
            {
                throw new InvalidOperationException(
                    "Either provide a placeholder or the select expression must be a FieldNode.");
            }

            return (fieldNode.WithSelectionSet(selectionSet), new[] { fieldNode.Name.Value });
        }

        return ((ISelectionNode)selection!, context.SelectionPath);
    }

    private class ResolverRewriter : SyntaxRewriter<FetchRewriterContext>
    {
        protected override FieldNode? RewriteField(FieldNode node, FetchRewriterContext context)
        {
            var result = base.RewriteField(node, context);

            if (result is not null && context.PlaceholderFound)
            {
                context.PlaceholderFound = false;

                if (context.ResponseName is not null &&
                    !node.Name.Value.EqualsOrdinal(context.ResponseName))
                {
                    return result.WithAlias(new NameNode(context.ResponseName));
                }
            }

            return result;
        }

        protected override SelectionSetNode? RewriteSelectionSet(
            SelectionSetNode node,
            FetchRewriterContext context)
        {
            var rewritten = base.RewriteSelectionSet(node, context);

            if (rewritten is not null && context.SelectionSet is not null)
            {
                List<ISelectionNode>? rewrittenList = null;
                for (var i = 0; i < rewritten.Selections.Count; i++)
                {
                    var selectionNode = rewritten.Selections[i];

                    if (rewrittenList is null)
                    {
                        if (!selectionNode.Equals(context.Placeholder, SyntaxComparison.Syntax))
                        {
                            continue;
                        }

                        // preserve selection path, so we are later able to unwrap the result.
                        var path = context.Path.ToArray();
                        context.SelectionPath = path;
                        context.PlaceholderFound = true;
                        rewrittenList = new List<ISelectionNode>();

                        if (context.ResponseName is not null)
                        {
                            path[^1] = context.ResponseName;
                        }

                        for (var j = 0; j < i; j++)
                        {
                            rewrittenList.Add(rewritten.Selections[j]);
                        }
                    }

                    foreach (var selection in context.SelectionSet.Selections)
                    {
                        rewrittenList.Add(selection);
                    }
                }

                return rewrittenList is null
                    ? rewritten
                    : rewritten.WithSelections(rewrittenList);
            }

            return rewritten;
        }

        protected override ISyntaxNode? OnRewrite(ISyntaxNode node, FetchRewriterContext context)
        {
            if (node is VariableNode variableNode &&
                context.Variables.TryGetValue(variableNode.Name.Value, out var valueNode))
            {
                return valueNode;
            }

            return base.OnRewrite(node, context);
        }

        protected override FetchRewriterContext OnEnter(
            ISyntaxNode node,
            FetchRewriterContext context)
        {
            if (node is FieldNode field)
            {
                context.Path.Push(field.Name.Value);
            }

            return base.OnEnter(node, context);
        }

        protected override void OnLeave(
            ISyntaxNode? node,
            FetchRewriterContext context)
        {
            if (node is FieldNode)
            {
                context.Path.Pop();
            }

            base.OnLeave(node, context);
        }
    }

    private sealed class FetchRewriterContext : ISyntaxVisitorContext
    {
        public FetchRewriterContext(
            FragmentSpreadNode? placeholder,
            IReadOnlyDictionary<string, IValueNode> variables,
            SelectionSetNode? selectionSet,
            string? responseName)
        {
            Placeholder = placeholder;
            Variables = variables;
            SelectionSet = selectionSet;
            ResponseName = responseName;
        }

        public string? ResponseName { get; }

        public Stack<string> Path { get; } = new();

        public FragmentSpreadNode? Placeholder { get; }

        public bool PlaceholderFound { get; set; }

        public IReadOnlyDictionary<string, IValueNode> Variables { get; }

        public SelectionSetNode? SelectionSet { get; }

        public IReadOnlyList<string> SelectionPath { get; set; } = Array.Empty<string>();
    }
}

internal enum ResolverKind
{
    Query,
    Subscription,
    Batch
}
