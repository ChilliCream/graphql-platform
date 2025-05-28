using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types.Pagination;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection;

public static partial class SchemaRequestExecutorBuilderExtensions
{
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
