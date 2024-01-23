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
            string? responseName,
            IReadOnlyList<string>? unspecifiedArguments)
        {
            Placeholder = placeholder;
            Variables = variables;
            SelectionSet = selectionSet;
            ResponseName = responseName;
            UnspecifiedArguments = unspecifiedArguments;
        }

        public string? ResponseName { get; }

        public Stack<string> Path { get; } = new();

        public FragmentSpreadNode? Placeholder { get; }

        public bool PlaceholderFound { get; set; }

        public IReadOnlyDictionary<string, IValueNode> Variables { get; }

        /// <summary>
        /// An optional list of arguments that weren't explicitly specified in the original query.
        /// </summary>
        public IReadOnlyList<string>? UnspecifiedArguments { get; }

        public SelectionSetNode? SelectionSet { get; }

        public IReadOnlyList<string> SelectionPath { get; set; } = Array.Empty<string>();
    }
}
