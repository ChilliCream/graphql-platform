using System;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Internal;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class RequestExecutorBuilderExtensions
{
    /// <summary>
    /// Configures the resolver compiler.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// A delegate that is to configure the resolver compiler.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
    /// and its execution.
    /// </returns>
    [Obsolete("Implement IParameterExpressionBuilder")]
    public static IRequestExecutorBuilder ConfigureResolverCompiler(
        this IRequestExecutorBuilder builder,
        Action<IResolverCompilerBuilder> configure)
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (configure is null)
        {
            throw new ArgumentNullException(nameof(configure));
        }

        configure(new DefaultResolverCompilerBuilder(builder));
        return builder;
    }

    /// <summary>
    /// Registers a well-known service with the resolver compiler.
    /// The service does no longer need any annotation in the resolver.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="kind">
    /// The service kind defines the way a service is injected and handled by the execution engine.
    /// </param>
    /// <typeparam name="TService">
    /// The service type.
    /// </typeparam>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
    /// and its execution.
    /// </returns>
    public static IRequestExecutorBuilder RegisterService<TService>(
        this IRequestExecutorBuilder builder,
        ServiceKind kind = ServiceKind.Default)
        where TService : class
    {
        builder.Services.AddSingleton<IParameterExpressionBuilder>(
            new CustomServiceParameterExpressionBuilder<TService>(kind));
        return builder;
    }
}
