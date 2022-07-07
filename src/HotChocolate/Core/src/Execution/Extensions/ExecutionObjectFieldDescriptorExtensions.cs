using System;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using static HotChocolate.Execution.Processing.OperationCompilerOptimizerHelper;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Execution;

public static class ExecutionObjectFieldDescriptorExtensions
{
    public static IObjectFieldDescriptor UseOptimizer(
        this IObjectFieldDescriptor descriptor,
        IOperationCompilerOptimizer optimizer)
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
