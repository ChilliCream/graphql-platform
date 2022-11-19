using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Data;

/// <summary>
/// Request Builder Extensions for Sorting
/// </summary>
public static class MartenSortingRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds MartenDB sorting support.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    public static IRequestExecutorBuilder AddMartenSorting(
        this IRequestExecutorBuilder builder)
        => builder.ConfigureSchema(x => x.AddMartenSorting());
}
