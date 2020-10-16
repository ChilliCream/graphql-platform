using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolate.Types;

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
            ISelectionSet selectionSet,
            IObjectType objectType,
            out IReadOnlyList<ISelection> handledSelections);
    }
}
