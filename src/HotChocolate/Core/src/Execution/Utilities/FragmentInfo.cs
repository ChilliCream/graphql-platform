using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    /// <summary>
    /// Represents an aligned interface for fragments to the execution engine.
    /// </summary>
    internal sealed class FragmentInfo
    {
        public FragmentInfo(
            IType typeCondition,
            SelectionSetNode selectionSet,
            IReadOnlyList<DirectiveNode> directives)
        {
            TypeCondition = typeCondition;
            SelectionSet = selectionSet;
            Directives = directives;
        }

        /// <summary>
        /// Gets the fragment type condition.
        /// </summary>
        public IType TypeCondition { get; }

        /// <summary>
        /// Gets the fragment selection set.
        /// </summary>
        /// <value></value>
        public SelectionSetNode SelectionSet { get; }

        /// <summary>
        /// Gets the directives of this fragment.
        /// </summary>
        /// <value></value>
        public IReadOnlyList<DirectiveNode> Directives { get; }
    }
}
