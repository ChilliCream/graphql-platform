using HotChocolate.Language;

namespace HotChocolate.Fusion.Metadata;

internal sealed partial class ResolverDefinition
{
    private static readonly ResolverRewriter _rewriter = new();
    private readonly FieldNode? _field;

    public ResolverDefinition(
        string subgraphName,
        ResolverKind kind,
        SelectionSetNode select,
        FragmentSpreadNode? placeholder,
        IReadOnlyList<string> requires,
        IReadOnlyDictionary<string, ITypeNode> arguments)
    {
        SubgraphName = subgraphName;
        Kind = kind;
        Select = select;
        Placeholder = placeholder;
        Requires = requires;
        Arguments = arguments;

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

    public IReadOnlyDictionary<string, ITypeNode> Arguments { get;  }

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
}
