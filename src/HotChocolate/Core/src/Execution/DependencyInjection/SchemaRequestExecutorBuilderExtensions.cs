using HotChocolate;
using HotChocolate.Execution.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for configuring an <see cref="IRequestExecutorBuilder"/>
/// </summary>
public static partial class SchemaRequestExecutorBuilderExtensions
{
    [Obsolete("Use ModifyOptions instead.")]
    public static IRequestExecutorBuilder SetOptions(
        this IRequestExecutorBuilder builder,
        IReadOnlySchemaOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(options);

        return builder.ConfigureSchema(b => b.SetOptions(options));
    }

    public static IRequestExecutorBuilder ModifyOptions(
        this IRequestExecutorBuilder builder,
        Action<SchemaOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return builder.ConfigureSchema(b => b.ModifyOptions(configure));
    }

    public static IRequestExecutorBuilder SetContextData(
        this IRequestExecutorBuilder builder,
        string key,
        object? value)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(key);

        return builder.ConfigureSchema(b => b.SetContextData(key, value));
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
        bool trim = true) =>
        builder.ModifyOptions(o => o.RemoveUnreachableTypes = trim);
}
