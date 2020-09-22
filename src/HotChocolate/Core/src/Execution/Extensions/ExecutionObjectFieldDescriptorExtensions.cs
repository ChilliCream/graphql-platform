using System;
using HotChocolate.Execution.Utilities;
using HotChocolate.Types;
using static HotChocolate.Execution.Utilities.SelectionOptimizerHelper;

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