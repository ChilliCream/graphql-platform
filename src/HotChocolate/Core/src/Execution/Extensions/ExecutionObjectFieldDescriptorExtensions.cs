using System;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using static HotChocolate.Execution.Processing.SelectionOptimizerHelper;

namespace HotChocolate.Execution
{
    public static class ExecutionObjectFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor UseOptimizer(
            this IObjectFieldDescriptor descriptor,
            ISelectionOptimizer optimizer)
        {
            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            if (optimizer is null)
            {
                throw new ArgumentNullException(nameof(optimizer));
            }

            descriptor
                .Extend()
                .OnBeforeCreate(d => RegisterOptimizer(d.ContextData, optimizer));

            return descriptor;
        }
    }
}