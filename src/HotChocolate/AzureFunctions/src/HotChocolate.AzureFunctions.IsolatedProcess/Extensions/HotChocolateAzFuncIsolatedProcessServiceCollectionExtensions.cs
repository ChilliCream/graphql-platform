using HotChocolate.AzureFunctions;
using HotChocolate.AzureFunctions.IsolatedProcess;
using HotChocolate.Execution.Configuration;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides DI extension methods to configure a GraphQL server.
/// </summary>
public static class HotChocolateAzFuncIsolatedProcessServiceCollectionExtensions
{
    /// <summary>
    /// Adds a GraphQL server and Azure Functions integration services to the DI for Azure Functions Isolated processing model.
    /// </summary>
    /// <param name="services">
    /// The <see cref="IServiceCollection"/>.
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
    public static IRequestExecutorBuilder AddGraphQLFunctionIsolatedProcess(
        this IServiceCollection services,
        int maxAllowedRequestSize = 20 * 1000 * 1000,
        string apiRoute = GraphQLAzureFunctionsConstants.DefaultGraphQLRoute)
    {
        if (services is null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        IRequestExecutorBuilder executorBuilder =
            services.AddGraphQLServer(maxAllowedRequestSize: maxAllowedRequestSize);

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IExtensionConfigProvider, GraphQLIsolatedProcessExtensions>());

        //Add the Request Executor Dependency...
        services.AddAzureFunctionsGraphQLRequestExecutorDependency(apiRoute);

        return executorBuilder;
    }
}
