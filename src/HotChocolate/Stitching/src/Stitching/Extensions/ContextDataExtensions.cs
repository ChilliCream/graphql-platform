using System;
using System.Collections.Generic;
using HotChocolate.Execution;
using static HotChocolate.Stitching.WellKnownContextData;

namespace HotChocolate.Stitching
{
    internal static class ContextDataExtensions
    {
        public static IReadOnlyDictionary<NameString, IRequestExecutor> GetRemoteExecutors(
            this IRequestContext context)
        {
            if (context.Schema.ContextData.TryGetValue(RemoteExecutors, out object? o) &&
                o is IReadOnlyDictionary<NameString, IRequestExecutor> executors)
            {
                return executors;
            }
            else
            {
                // TODO : throw helper
                throw new InvalidOperationException(
                    "The mandatory remote executors have not been found.");
            }
        }
    }
}
