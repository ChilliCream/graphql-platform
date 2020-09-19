using System.Collections.Generic;

namespace HotChocolate.Execution.Utilities
{
    /// <summary>
    /// A selection set is primarily composed of field selections.
    /// When needed a selection set can preserve fragments so that the execution engine
    /// can branch the processing of these fragments.
    /// </summary>
    public interface ISelectionSet
    {
        /// <summary>
        /// Defines if this list needs post processing for skip and include.
        /// </summary>
        bool IsConditional { get; }

        /// <summary>
        /// This list contains the selections that shall be executed.
        /// </summary>
        IReadOnlyList<ISelection> Selections { get; }

        /// <summary>
        /// This list contains fragments if any were preserved for execution.
        /// </summary>
        IReadOnlyList<IFragment> Fragments { get; }
    }
}
