using HotChocolate.Execution.Utilities;
using HotChocolate.Types;
using static HotChocolate.Execution.Utilities.SelectionSetOptimizerHelper;

namespace HotChocolate.Execution
{
    public static class ExecutionObjectFieldDescriptorExtensions
    {
        public static IObjectFieldDescriptor UseOptimizer(
            IObjectFieldDescriptor descriptor,
            ISelectionSetOptimizer optimizer)
        {
            descriptor
                .Extend()
                .OnBeforeCreate(d => RegisterOptimizer(d.ContextData, optimizer));

            return descriptor;
        }
    }
}