using HotChocolate.Execution;
using HotChocolate.Execution.Options;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace HotChocolate;

/// <summary>
/// Provides extension methods for the <see cref="Schema"/> class.
/// </summary>
public static class ExecutionSchemaExtensions
{
    /// <summary>
    /// Creates an executor from the schema.
    /// </summary>
    /// <param name="schema">
    /// The schema.
    /// </param>
    /// <returns>
    /// Returns an executor from the schema.
    /// </returns>
    /// <remarks>
    /// This helper is only meant for testing purposes.
    /// </remarks>
    public static IRequestExecutor MakeExecutable(
        this Schema schema)
        => MakeExecutable(schema, _ => { });

    /// <summary>
    /// Creates an executor from the schema.
    /// </summary>
    /// <param name="schema">
    /// The schema.
    /// </param>
    /// <param name="configure">
    /// The configure action.
    /// </param>
    /// <returns>
    /// Returns an executor from the schema.
    /// </returns>
    /// <remarks>
    /// This helper is only meant for testing purposes.
    /// </remarks>
    public static IRequestExecutor MakeExecutable(
        this Schema schema,
        Action<RequestExecutorOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(configure);

        return new ServiceCollection()
            .AddGraphQL()
            .Configure(o => o.Schema = schema)
            .ModifyRequestOptions(configure)
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorProvider>()
            .GetExecutorAsync()
            .Result;
    }
}
