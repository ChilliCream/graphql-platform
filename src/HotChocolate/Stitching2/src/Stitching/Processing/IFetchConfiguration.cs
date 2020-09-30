using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace HotChocolate.Stitching.Processing
{
    public interface IFetchConfiguration
    {
        NameString SchemaName { get; }

        NameString TypeName { get; }

        IReadOnlyList<Field> Requires { get; }

        /// <summary>
        /// Gets the selections that can be handled by this <see cref="IFetchConfiguration" />.
        /// </summary>
        SelectionSetNode Provides { get; }

        bool CanHandleSelections(
            IPreparedOperation operation,
            ISelection selection,
            out IReadOnlyList<ISelection> handledSelections);
    }
}
