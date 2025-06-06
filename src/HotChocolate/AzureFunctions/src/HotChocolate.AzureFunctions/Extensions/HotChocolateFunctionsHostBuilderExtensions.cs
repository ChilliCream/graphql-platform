using HotChocolate.AzureFunctions;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Azure.Functions.Extensions.DependencyInjection;

public static class HotChocolateFunctionsHostBuilderExtensions
{
    /// <summary>
    /// Adds a GraphQL server and Azure Functions integration services.
    /// This specific configuration method is only supported by the Azure Functions
    /// In-process model;
    /// the overload offers compatibility with the isolated process model
    /// for configuration code portability.
    /// </summary>
    /// <param name="hostBuilder">
    /// The <see cref="IFunctionsHostBuilder"/>.
    /// </param>
    /// <param name="maxAllowedRequestSize">
    /// The max allowed GraphQL request size.
    /// </param>
    /// <param name="apiRoute">
    /// The API route that was used in the GraphQL Azure Function.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IRequestExecutorBuilder"/> so that configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <see cref="IServiceCollection"/> is <c>null</c>.
    /// </exception>
    public static IRequestExecutorBuilder AddGraphQLFunction(
        this IFunctionsHostBuilder hostBuilder,
        int maxAllowedRequestSize = GraphQLAzureFunctionsConstants.DefaultMaxRequests,
        string apiRoute = GraphQLAzureFunctionsConstants.DefaultGraphQLRoute)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);

        return hostBuilder.Services.AddGraphQLFunction(maxAllowedRequestSize, apiRoute);
    }

    /// <summary>
    /// Adds a GraphQL server and Azure Functions integration services in an identical
    /// way as the Azure Functions Isolated processing model; providing compatibility
    /// and portability of configuration code.
    /// </summary>
    /// <param name="hostBuilder">
    /// The <see cref="IFunctionsHostBuilder"/>.
    /// </param>
    /// <param name="configure">
    /// The GraphQL Configuration function that will be invoked, for chained configuration,
    /// when the Host is built.
    /// </param>
    /// <param name="maxAllowedRequestSize">
    /// The max allowed GraphQL request size.
    /// </param>
    /// <param name="apiRoute">
    /// The API route that was used in the GraphQL Azure Function.
    /// </param>
    /// <returns>
    /// Returns the <see cref="IHostBuilder"/> so that host configuration can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// The <see cref="IServiceCollection"/> is <c>null</c>.
    /// </exception>
    public static IFunctionsHostBuilder AddGraphQLFunction(
        this IFunctionsHostBuilder hostBuilder,
        Action<IRequestExecutorBuilder> configure,
        int maxAllowedRequestSize = GraphQLAzureFunctionsConstants.DefaultMaxRequests,
        string apiRoute = GraphQLAzureFunctionsConstants.DefaultGraphQLRoute)
    {
        ArgumentNullException.ThrowIfNull(hostBuilder);
        ArgumentNullException.ThrowIfNull(configure);

        var executorBuilder = hostBuilder.AddGraphQLFunction(maxAllowedRequestSize, apiRoute);
        configure.Invoke(executorBuilder);

        return hostBuilder;
    }
}
