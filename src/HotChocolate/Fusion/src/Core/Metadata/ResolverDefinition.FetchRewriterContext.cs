using HotChocolate.Language;
using HotChocolate.Language.Visitors;

namespace HotChocolate.Fusion.Metadata;

internal sealed partial class ResolverDefinition
{
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
