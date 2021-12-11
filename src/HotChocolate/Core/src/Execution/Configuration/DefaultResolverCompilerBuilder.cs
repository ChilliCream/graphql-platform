using System;

namespace HotChocolate.Execution.Configuration;

/// <summary>
/// The default <see cref="IResolverCompilerBuilder"/> implementation.
/// </summary>
[Obsolete("Implement IParameterExpressionBuilder")]
internal sealed class DefaultResolverCompilerBuilder : IResolverCompilerBuilder
{
    /// <summary>
    /// Initializes a new instance of <see cref="DefaultResolverCompilerBuilder"/>.
    /// </summary>
    /// <param name="requestExecutorBuilder">
    /// The <see cref="IRequestExecutorBuilder"/> for which we want to apply configuration.
    /// </param>
    public DefaultResolverCompilerBuilder(IRequestExecutorBuilder requestExecutorBuilder)
    {
        RequestExecutorBuilder = requestExecutorBuilder;
    }

    /// <inheritdoc />
    public IRequestExecutorBuilder RequestExecutorBuilder { get; }
}
