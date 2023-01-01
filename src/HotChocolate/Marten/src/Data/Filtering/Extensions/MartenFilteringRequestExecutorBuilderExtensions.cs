using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable once CheckNamespace
namespace HotChocolate.Data;

/// <summary>
/// Request Builder Extensions for Filtering
/// </summary>
public static class MartenFilteringRequestExecutorBuilderExtensions
{
    /// <summary>
    /// Adds MartenDB filtering support.
    /// </summary>
    /// <param name="builder">
    /// The GraphQL configuration builder.
    /// </param>
    /// <returns>
    /// Returns the GraphQL configuration builder.
    /// </returns>
    public static IRequestExecutorBuilder AddMartenFiltering(
        this IRequestExecutorBuilder builder)
        => builder.ConfigureSchema(sb => sb.AddMartenFiltering());
}
