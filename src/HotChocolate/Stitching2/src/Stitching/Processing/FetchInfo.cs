using System;
using System.Threading.Tasks;
using HotChocolate.Execution.Processing;

namespace HotChocolate.Stitching.Processing
{
    public interface IFetchProvider
    {
        bool CanHandleSelections(
            ISelectionSet selectionSet,
            out int handledFields);

        IFetchCall CompileFetch();
    }

    public interface IFetchCall
    {
        ValueTask<IQueryResult> InvokeAsync();
    }
}