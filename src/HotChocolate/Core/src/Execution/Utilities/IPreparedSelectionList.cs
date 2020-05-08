using System.Collections.Generic;

namespace HotChocolate.Execution.Utilities
{
    internal interface IPreparedSelectionList : IReadOnlyList<IPreparedSelection>
    {
        /// </summary>
        /// Defines is this list needs no post processing for skip and include.
        /// <summary>
        bool IsFinal { get; }
    }
}
