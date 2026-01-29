using HotChocolate.Language;
using HotChocolate.Resolvers;
using HotChocolate.Types;

namespace HotChocolate.Execution.Processing;

/// <summary>
/// The <see cref="Operation"/> optimizer provides helper methods
/// to optimize a <see cref="Operation"/> and store additional execution metadata.
/// </summary>
public readonly ref struct OperationOptimizerContext
{
    /// <summary>
    /// Initializes a new instance of <see cref="OperationOptimizerContext"/>
    /// </summary>
    internal OperationOptimizerContext(
        Operation operation)
    {
        Operation = operation;
    }

    /// <summary>
    /// Gets the operation.
    /// </summary>
    public Operation Operation { get; }
}
