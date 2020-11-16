using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing
{
    /// <summary>
    /// Represents an aligned interface for fragments to the execution engine.
    /// </summary>
    internal sealed class FragmentInfo
    {
        /// <summary>
        /// Initializes a new instance of <see cref="FragmentInfo"/>.
        /// </summary>
        public FragmentInfo(
            IType typeCondition,
            SelectionSetNode selectionSet,
            IReadOnlyList<DirectiveNode> directives,
            InlineFragmentNode? inlineFragment,
            FragmentDefinitionNode? fragmentDefinition)
        {
            TypeCondition = typeCondition;
            SelectionSet = selectionSet;
            Directives = directives;
            InlineFragment = inlineFragment;
            FragmentDefinition = fragmentDefinition;
        }

        /// <summary>
        /// Gets the fragment type condition.
        /// </summary>
        public IType TypeCondition { get; }

        /// <summary>
        /// Gets the fragment selection set.
        /// </summary>
        public SelectionSetNode SelectionSet { get; }

        /// <summary>
        /// Gets the directives of this fragment.
        /// </summary>
        public IReadOnlyList<DirectiveNode> Directives { get; }

        /// <summary>
        /// Gets the associated inline fragment syntax node.
        /// </summary>
        public InlineFragmentNode? InlineFragment { get; }

        /// <summary>
        /// Gets the associated fragment definition syntax node.
        /// </summary>
        public FragmentDefinitionNode? FragmentDefinition { get; }
    }
}
