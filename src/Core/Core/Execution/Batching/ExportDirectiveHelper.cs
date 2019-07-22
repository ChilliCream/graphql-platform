using System.Collections.Concurrent;
using System.Collections.Generic;

namespace HotChocolate.Execution.Batching
{
    internal static class ExportDirectiveHelper
    {
        public const string Name = "export";
        public const string ExportedVariables = "HC.ExportedVariables";

        public static void AddExportedVariables(
            IQueryRequestBuilder builder,
            ConcurrentBag<ExportedVariable> exportedVariables)
        {
            builder.SetProperty(
                ExportedVariables,
                exportedVariables);
        }

        public static bool TryGetExportedVariables(
            IDictionary<string, object> contextData,
            out ICollection<ExportedVariable> exportedVariables)
        {
            if (contextData.TryGetValue(ExportedVariables, out object obj)
                && obj is ICollection<ExportedVariable> exp)
            {
                exportedVariables = exp;
                return true;
            }

            exportedVariables = null;
            return false;
        }
    }
}
