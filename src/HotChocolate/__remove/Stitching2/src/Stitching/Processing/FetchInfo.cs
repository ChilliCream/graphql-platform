using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;

namespace HotChocolate.Stitching.Processing
{

    public interface IFetchHandler
    {
        bool CanHandleSelections(
            IPreparedOperation operation,
            IEnumerable<ISelection> selections,
            IObjectType typeContext,
            out IReadOnlyList<ISelection> handledSelections);

        IFetchCall CompileFetch(
            IPreparedOperation operation,
            ISelectionSet selectionSet,
            IObjectType typeContext,
            out IReadOnlyList<ISelection> handledSelections);
    }




}
