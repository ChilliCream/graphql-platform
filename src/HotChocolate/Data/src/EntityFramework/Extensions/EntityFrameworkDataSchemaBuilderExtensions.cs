namespace HotChocolate.Data.Extensions;

/// <summary>
/// Provides extensions for <see cref="ISchemaBuilder"/> for projections
/// </summary>
public static class EntityFrameworkDataSchemaBuilderExtensions
{
    /// <summary>
    /// Adds projection support.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="ISchemaBuilder"/>.
    /// </param>
    /// <param name="name">
    /// The name of the scope of the convention. You can use scopes to apply different drivers for
    /// different resolvers
    /// <c>
    /// [UseProjection("scope_name")]
    /// </c>
    /// </param>
    /// <returns>
    /// Returns the <see cref="ISchemaBuilder"/>.
    /// </returns>
    public static ISchemaBuilder AddEntityFrameworkProjections(
        this ISchemaBuilder builder,
        string? name = null) =>
        builder.AddProjections(x => x.AddEntityFrameworkDefaults(), name);
}
