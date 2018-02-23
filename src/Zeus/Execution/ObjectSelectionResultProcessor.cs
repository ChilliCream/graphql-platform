using System;
using System.Collections.Generic;
using Zeus.Abstractions;
using Zeus.Resolvers;

namespace Zeus.Execution
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

            IType type = selectionTask.Context.Schema.InferType(
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