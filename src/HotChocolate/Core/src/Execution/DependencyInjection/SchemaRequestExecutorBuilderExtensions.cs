using HotChocolate;
using HotChocolate.Execution.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring an <see cref="IRequestExecutorBuilder"/>
/// </summary>
public static partial class SchemaRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Modifies the GraphQL schema options.
    /// </summary>
    /// <param name="builder">
    /// The request executor builder.
    /// </param>
    /// <param name="configure">
    /// A delegate to configure the schema options.
    /// </param>
    /// <returns>
    /// Returns the request executor builder to chain in further configuration.
    /// </returns>
    public static IRequestExecutorBuilder ModifyOptions(
        this IRequestExecutorBuilder builder,
        Action<SchemaOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return builder.ConfigureSchema(b => b.ModifyOptions(configure));
    }

    /// <summary>
    /// Configures the schema to remove types that cannot be reached by the execution engine.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="trim">
    /// A boolean defining if type trimming shall be applied.
    /// </param>
    /// <returns>
    /// Returns <see cref="IRequestExecutorBuilder"/> so that configurations can be chained.
    /// </returns>
    public static IRequestExecutorBuilder TrimTypes(
        this IRequestExecutorBuilder builder,
        bool trim = true)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ModifyOptions(o => o.RemoveUnreachableTypes = trim);
    }
}
