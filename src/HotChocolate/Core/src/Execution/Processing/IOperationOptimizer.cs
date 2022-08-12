namespace HotChocolate.Execution.Processing;

/// <summary>
/// The operation optimizer allows to optimize selections and create execution metadata.
/// </summary>
public interface IOperationOptimizer : IOperationCompilerOptimizer
{
    /// <summary>
    /// Is called to apply custom optimizations to a <see cref="Operation"/>.
    /// </summary>
    /// <param name="context">
    /// The <see cref="Operation"/> optimizer context.
    /// </param>
    void OptimizeOperation(OperationOptimizerContext context);
}
