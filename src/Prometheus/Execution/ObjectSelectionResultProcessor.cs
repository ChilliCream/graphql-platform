using System;
using System.Collections.Generic;
using Prometheus.Abstractions;
using Prometheus.Resolvers;

namespace Prometheus.Execution
{
    internal class ObjectSelectionResultProcessor
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
                yield break;
            }

            Dictionary<string, object> map = new Dictionary<string, object>();
            selectionTask.IntegrateResult(map);

            IType type = selectionTask.Context.Schema.ResolveAbstractType(
                selectionTask.Selection.TypeDefinition,
                selectionTask.Selection.FieldDefinition,
                result);

            foreach (IOptimizedSelection selection in selectionTask.Selection.GetSelections(type))
            {
                IResolverContext context = selection.CreateContext(selectionTask.Context, result);
                yield return new ResolveSelectionTask(context, selection, r => map[selection.Name] = r);
            }
        }

        internal static ObjectSelectionResultProcessor Default { get; } = new ObjectSelectionResultProcessor();
    }
}