using System;
using System.Collections.Generic;
using System.Linq;

namespace Prometheus.Execution
{
    internal class ScalarSelectionResultProcessor
        : ISelectionResultProcessor
    {
        public IEnumerable<IResolveSelectionTask> Process(IResolveSelectionTask selectionTask)
        {
            if (selectionTask == null)
            {
                throw new ArgumentNullException(nameof(selectionTask));
            }

            object result = selectionTask.Result;
            if (result is Func<object> f)
            {
                result = f();
            }

            if (result == null)
            {
                selectionTask.IntegrateResult(null);
            }
            else
            {
                selectionTask.IntegrateResult(result);
            }

            return Enumerable.Empty<IResolveSelectionTask>();
        }

        internal static ScalarSelectionResultProcessor Default { get; } = new ScalarSelectionResultProcessor();
    }
}