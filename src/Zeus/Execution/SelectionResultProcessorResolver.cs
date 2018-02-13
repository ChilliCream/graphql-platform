using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Zeus.Abstractions;
using Zeus.Resolvers;

namespace Zeus.Execution
{
    internal static class SelectionResultProcessorResolver
    {
        public static ISelectionResultProcessor GetProcessor(IType fieldType)
        {
            if (fieldType == null)
            {
                throw new ArgumentNullException(nameof(fieldType));
            }

            if (fieldType.IsListType())
            {
                if (fieldType.ElementType().IsScalarType())
                {
                    return ScalarListSelectionResultProcessor.Default;
                }
                else
                {
                    return ObjectListSelectionResultProcessor.Default;
                }
            }

            if (fieldType.IsScalarType())
            {
                return ScalarSelectionResultProcessor.Default;
            }

            return ObjectSelectionResultProcessor.Default;
        }
    }
}