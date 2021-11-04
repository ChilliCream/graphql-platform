using HotChocolate.AzureFunctions;
using HotChocolate.Execution.Configuration;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Azure.Functions.Extensions.DependencyInjection;

public static class HotChocolateFunctionsHostBuilderExtensions
{
    /// <summary>
    /// Adds a GraphQL server and Azure Functions integration services.
    /// </summary>
    /// <param name="builder">
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
        this IFunctionsHostBuilder builder,
        int maxAllowedRequestSize = 20 * 1000 * 1000,
        string apiRoute = "/api/graphql")
    {
        if (builder is null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        return builder.Services.AddGraphQLFunction(maxAllowedRequestSize, apiRoute);
    }
}
