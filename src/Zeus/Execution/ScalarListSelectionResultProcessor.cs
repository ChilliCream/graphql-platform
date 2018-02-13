using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Zeus.Resolvers;

namespace Zeus.Execution
{
    internal class ScalarListSelectionResultProcessor
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

            if (!IsValueType(result) && result is IEnumerable<object> eno)
            {
                selectionTask.IntegrateResult(eno.ToArray());
            }
            else if (!IsValueType(result) && result is IEnumerable en)
            {
                List<object> list = new List<object>();

                foreach (object o in en)
                {
                    list.Add(o);
                }

                selectionTask.IntegrateResult(list.ToArray());
            }
            else
            {
                selectionTask.IntegrateResult(new[] { result });
            }
        }

        private static bool IsValueType(object result)
        {
            if (result is string)
            {
                return true;
            }
            return result.GetType().IsValueType;
        }

        internal static ScalarListSelectionResultProcessor Default { get; } = new ScalarListSelectionResultProcessor();
    }
}