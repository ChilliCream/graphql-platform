using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using static HotChocolate.Execution.Processing.OperationCompilerOptimizerHelper;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Execution;

/// <summary>
/// Provides extension methods to the <see cref="IObjectFieldDescriptor"/> class.
/// </summary>
public static class ExecutionObjectFieldDescriptorExtensions
{
    /// <summary>
    /// Adds a selection set optimizer to the field.
    /// The optimizer will be used for this field's selection
    /// set and also for all child field selection sets.
    /// </summary>
    /// <param name="descriptor">
    /// The object field descriptor.
    /// </param>
    /// <param name="optimizer">
    /// The selection set optimizer.
    /// </param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static IObjectFieldDescriptor UseOptimizer(
        this IObjectFieldDescriptor descriptor,
        ISelectionSetOptimizer optimizer)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(optimizer);

        descriptor
            .Extend()
            .OnBeforeCreate(d => RegisterOptimizer(d, optimizer));

        return descriptor;
    }
}
