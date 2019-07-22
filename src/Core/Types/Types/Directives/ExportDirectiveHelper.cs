using System.Collections.Concurrent;
using System.Collections.Generic;

namespace HotChocolate.Types
{
    public static class ExportDirectiveHelper
    {
        internal const string Name = "export";
        internal const string ExportedVariables = "HC.ExportedVariables";

        public static void AddExportedVariables(
            IDictionary<string, object> contextData)
        {
            contextData[ExportedVariables] =
                new ConcurrentBag<ExportedVariable>();
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
