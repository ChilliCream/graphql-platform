using System.ComponentModel.Design;
using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Fusion.Metadata;

internal sealed class FetchDefinition
{
    private static readonly FetchRewriter _rewriter = new();

    public FetchDefinition(
        string schemaName,
        ISelectionNode select,
        FragmentSpreadNode? placeholder,
        IReadOnlyList<string> requires)
    {
        SchemaName = schemaName;
        Select = select;
        Placeholder = placeholder;
        Requires = requires;
    }

    /// <summary>
    /// Gets the schema to which the type system member is bound to.
    /// </summary>
    public string SchemaName { get; }

    public ISelectionNode Select { get; }

    public FragmentSpreadNode? Placeholder { get; }

    public IReadOnlyList<string> Requires { get; }

    public (ISelectionNode selectionNode, IReadOnlyList<string> Path) CreateSelection(
        IReadOnlyDictionary<string, IValueNode> variables,
        SelectionSetNode? selectionSet)
    {
        var context = new FetchRewriterContext(Placeholder, variables, selectionSet);
        var selection = _rewriter.Rewrite(Select, context);

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

    private class FetchRewriter : SyntaxRewriter<FetchRewriterContext>
    {
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
                        context.SelectionPath = context.Path.ToArray();
                        rewrittenList = new List<ISelectionNode>();

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
            SelectionSetNode? selectionSet)
        {
            Placeholder = placeholder;
            Variables = variables;
            SelectionSet = selectionSet;
        }

        public Stack<string> Path { get; } = new();

        public FragmentSpreadNode? Placeholder { get; }

        public IReadOnlyDictionary<string, IValueNode> Variables { get; }

        public SelectionSetNode? SelectionSet { get; }

        public IReadOnlyList<string> SelectionPath { get; set; } = Array.Empty<string>();
    }
}
