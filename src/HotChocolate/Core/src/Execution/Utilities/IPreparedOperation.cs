using System.Collections.Generic;
using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Utilities
{
    public interface IPreparedOperation : IOperation
    {
        /// <summary>
        /// Gets the internal unique identifier for this operation.
        /// </summary>
        string Id { get; }

        IReadOnlyList<IPreparedSelection> GetSelections(
            SelectionSetNode selectionSet,
            ObjectType typeContext);

        string Print();
    }
}
