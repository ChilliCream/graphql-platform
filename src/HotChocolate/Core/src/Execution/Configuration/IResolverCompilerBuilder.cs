using System;

namespace HotChocolate.Execution.Configuration;

/// <summary>
/// The resolver compiler builder.
/// </summary>
[Obsolete("Implement IParameterExpressionBuilder")]
public interface IResolverCompilerBuilder
{
    /// <summary>
    /// THe inner request executor builder.
    /// </summary>
    IRequestExecutorBuilder RequestExecutorBuilder { get; }
}
