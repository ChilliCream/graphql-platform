using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Zeus.Resolvers;

namespace Zeus.Execution
{
    internal class ObjectListSelectionResultProcessor
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
                return Enumerable.Empty<IResolveSelectionTask>();
            }

            if (result is IEnumerable en)
            {
                List<IResolveSelectionTask> nextTasks = new List<IResolveSelectionTask>();
                List<object> list = new List<object>();

                foreach (object o in en)
                {
                    nextTasks.AddRange(CreateElementTasks(selectionTask, list, o));
                }
                selectionTask.IntegrateResult(list.ToArray());

                return nextTasks;
            }
            else
            {
                List<object> list = new List<object>();
                IResolveSelectionTask[] nextTasks = CreateElementTasks(selectionTask, list, result).ToArray();
                selectionTask.IntegrateResult(list.ToArray());
                return nextTasks;
            }
        }

        private IEnumerable<IResolveSelectionTask> CreateElementTasks(IResolveSelectionTask selectionTask, List<object> list, object element)
        {
            Dictionary<string, object> map = new Dictionary<string, object>();
            list.Add(map);

            foreach (IOptimizedSelection selection in selectionTask.Selection.Selections)
            {
                IResolverContext context = selection.CreateContext(selectionTask.Context, element);
                yield return new ResolveSelectionTask(context, selection, r => map[selection.Name] = r);
            }
        }

        internal static ObjectListSelectionResultProcessor Default { get; } = new ObjectListSelectionResultProcessor();
    }
}