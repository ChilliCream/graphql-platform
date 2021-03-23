using System;
using System.Collections.Generic;

namespace HotChocolate.Execution.Processing
{
    /// <summary>
    /// A selection set is primarily composed of field selections.
    /// When needed a selection set can preserve fragments so that the execution engine
    /// can branch the processing of these fragments.
    /// </summary>
    internal class SelectionSet : ISelectionSet
    {
        private static readonly IFragment[] _empty = Array.Empty<IFragment>();

        /// <summary>
        /// Initializes a new instance of <see cref="SelectionSet"/>.
        /// </summary>
        /// <param name="selections">
        /// A list of executable field selections.
        /// </param>
        /// <param name="isConditional">
        /// Defines if this list needs post processing for skip and include.
        /// </param>
        public SelectionSet(
            IReadOnlyList<ISelection> selections,
            bool isConditional)
        {
            Selections = selections;
            Fragments = _empty;
            IsConditional = isConditional;
        }

        /// <summary>
        /// Initializes a new instance of <see cref="SelectionSet"/>.
        /// </summary>
        /// <param name="selections">
        /// A list of executable field selections.
        /// </param>
        /// <param name="fragments">
        /// A list of preserved fragments that can be used to branch of
        /// some of the execution.
        /// </param>
        /// <param name="isConditional">
        /// Defines if this list needs post processing for skip and include.
        /// </param>
        public SelectionSet(
            IReadOnlyList<ISelection> selections,
            IReadOnlyList<IFragment>? fragments,
            bool isConditional)
        {
            Selections = selections;
            Fragments = fragments ?? _empty;
            IsConditional = isConditional;
        }

        /// <inheritdoc />
        public bool IsConditional { get; }

        /// <inheritdoc />
        public IReadOnlyList<ISelection> Selections { get; }

        /// <inheritdoc />
        public IReadOnlyList<IFragment> Fragments { get; }

        /// <summary>
        /// Gets an empty selection set.
        /// </summary>
        public static SelectionSet Empty { get; } = new(Array.Empty<ISelection>(), false);
    }
}
