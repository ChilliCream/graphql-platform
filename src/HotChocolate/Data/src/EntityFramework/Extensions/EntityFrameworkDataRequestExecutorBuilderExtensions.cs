using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Extensions;

/// <summary>
/// Provides extensions for <see cref="IRequestExecutorBuilder"/> for projections
/// </summary>
public static class EntityFrameworkDataRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds projections support.
    /// </summary>
    /// <param name="builder">
    /// The <see cref="IRequestExecutorBuilder"/>.
    /// </param>
    /// <param name="name">
    /// The projection convention name.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/>.
    /// </returns>
    public static IRequestExecutorBuilder AddEntityFrameworkProjections(
        this IRequestExecutorBuilder builder,
        string? name = null) =>
        builder.AddProjections(x => x.AddEntityFrameworkDefaults(), name);
}
