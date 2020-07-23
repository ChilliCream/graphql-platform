using System.Collections.Generic;

#nullable enable

namespace HotChocolate.Execution.Utilities
{
    public interface IPreparedSelectionList
        : IReadOnlyList<IPreparedSelection>
    {
        /// </summary>
        /// Defines if this list needs post processing for skip and include.
        /// <summary>
        bool IsFinal { get; }
    }
}
