using HotChocolate.Execution.Configuration;
using HotChocolate.Types.Pagination;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class SchemaRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Sets the global paging options.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="options">
    /// The paging options.
    /// </param>
    /// <returns>
    /// An <see cref="IRequestExecutorBuilder"/> that can be used to configure a schema
    /// and its execution.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <paramref name="builder"/> is <c>null</c>.
    /// </exception>
    [Obsolete("Use ModifyPagingOptions instead.")]
    public static IRequestExecutorBuilder SetPagingOptions(
        this IRequestExecutorBuilder builder,
        PagingOptions options)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.ConfigureSchema(s => s.SetPagingOptions(options));
    }

    /// <summary>
    /// Modifies the global paging options.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// A delegate to modify the paging options.
    /// </param>
    public static IRequestExecutorBuilder ModifyPagingOptions(
        this IRequestExecutorBuilder builder,
        Action<PagingOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        return builder.ConfigureSchema(s => s.ModifyPagingOptions(configure));
    }
}
