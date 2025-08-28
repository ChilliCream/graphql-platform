using HotChocolate.Features;
using HotChocolate.Types.Pagination;

// ReSharper disable once CheckNamespace
namespace HotChocolate;

public static partial class SchemaBuilderExtensions
{
    /// <summary>
    /// Modify the paging options for the schema.
    /// </summary>
    /// <param name="builder">
    /// The schema builder.
    /// </param>
    /// <param name="configure">
    /// The configuration action.
    /// </param>
    /// <returns>
    /// Returns the schema builder.
    /// </returns>
    public static ISchemaBuilder ModifyPagingOptions(
        this ISchemaBuilder builder,
        Action<PagingOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentNullException.ThrowIfNull(configure);

        configure(builder.Features.GetOrSet<PagingOptions>());
        return builder;
    }
}
