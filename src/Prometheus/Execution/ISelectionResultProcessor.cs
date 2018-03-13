using System.Collections.Generic;

namespace Prometheus.Execution
{
    internal interface ISelectionResultProcessor
    {
        // in: the executed selection result that contains the computed result of the selection.
        // out: in case the selection is a list or object we will return new selection tasks that have to be executed.
        IEnumerable<IResolveSelectionTask> Process(IResolveSelectionTask selectionTask);
    }
}