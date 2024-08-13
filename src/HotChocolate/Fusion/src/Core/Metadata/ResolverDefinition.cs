using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using static HotChocolate.Fusion.FusionResources;

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
        IReadOnlyDictionary<string, ITypeNode> argumentTypes)
    {
        SubgraphName = subgraphName;
        Kind = kind;
        Select = select;
        Placeholder = placeholder;
        Requires = requires;
        ArgumentTypes = argumentTypes;

        if (select.Selections is [FieldNode field,])
        {
            _field = field;
        }
    }

    /// <summary>
    /// Gets the schema to which the type system member is bound to.
    /// </summary>
    public string SubgraphName { get; }

    /// <summary>
    /// Gets the kind of the resolver.
    /// </summary>
    public ResolverKind Kind { get; }

    public SelectionSetNode Select { get; }

    public FragmentSpreadNode? Placeholder { get; }

    public IReadOnlyList<string> Requires { get; }

    /// <summary>
    /// Gets the argument target types of this resolver.
    /// </summary>
    public IReadOnlyDictionary<string, ITypeNode> ArgumentTypes { get; }

    public (ISelectionNode selectionNode, IReadOnlyList<string> Path) CreateSelection(
        IReadOnlyDictionary<string, IValueNode> variables,
        SelectionSetNode? selectionSet,
        string? responseName,
        IReadOnlyList<string>? unspecifiedArguments,
        IReadOnlyList<DirectiveNode>? directives)
    {
        var context = new FetchRewriterContext(Placeholder, variables, selectionSet, responseName, unspecifiedArguments,
            directives);
        var selection = _rewriter.Rewrite(_field ?? (ISyntaxNode)Select, context);

        if (Placeholder is null && selectionSet is not null)
        {
            if (selection is not FieldNode fieldNode)
            {
                throw new InvalidOperationException(
                    CreateSelection_MustBePlaceholderOrSelectExpression);
            }

            return (fieldNode.WithSelectionSet(selectionSet), new[] { fieldNode.Name.Value, });
        }

        return ((ISelectionNode)selection!, context.SelectionPath);
    }
}
