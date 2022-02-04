using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.AzureFunctions.IsolatedProcess;

[Extension("GraphQLIsolatedProcessExtensions")]
internal class GraphQLIsolatedProcessExtensions : IExtensionConfigProvider
{
    private readonly IServiceProvider _services;

    public GraphQLIsolatedProcessExtensions(IServiceProvider services)
    {
        _services = services;
    }

    public void Initialize(ExtensionConfigContext context)
    {
        context.AddBindingRule<GraphQLAttribute>().BindToInput(BindExecutor);
    }

    private Task<IGraphQLRequestExecutor> BindExecutor(
        GraphQLAttribute attr,
        ValueBindingContext context)
        => Task.FromResult(_services.GetRequiredService<IGraphQLRequestExecutor>());
}
