using HotChocolate.Language;

namespace HotChocolate.Fusion.Metadata;

internal sealed partial class ResolverDefinition
{
    private sealed class FetchRewriterContext(
        FragmentSpreadNode? placeholder,
        IReadOnlyDictionary<string, IValueNode> variables,
        SelectionSetNode? selectionSet,
        string? responseName,
        IReadOnlyList<string>? unspecifiedArguments,
        IReadOnlyList<DirectiveNode>? directives)
    {
        public string? ResponseName { get; } = responseName;

        public Stack<string> Path { get; } = new();

        public FragmentSpreadNode? Placeholder { get; } = placeholder;

        public bool PlaceholderFound { get; set; }

        public IReadOnlyDictionary<string, IValueNode> Variables { get; } = variables;

        /// <summary>
        /// An optional list of arguments that weren't explicitly specified in the original query.
        /// </summary>
        public IReadOnlyList<string>? UnspecifiedArguments { get; } = unspecifiedArguments;

        public IReadOnlyList<DirectiveNode>? Directives { get; } = directives;

        public SelectionSetNode? SelectionSet { get; } = selectionSet;

        public IReadOnlyList<string> SelectionPath { get; set; } = Array.Empty<string>();
    }
}
