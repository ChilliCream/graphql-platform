using System.Reactive.Disposables;
using Microsoft.Extensions.DependencyInjection;

public class Foo
{
    public static void Bar()
    {
        var services = new ServiceCollection();

        services
            .AddGraphQLGateway()
            .AddFileSystemConfiguration("./schema.graphql")
            .ModifyRequestOptions(o => o.ExecutionTimeout = TimeSpan.FromSeconds(10))
            .UsePersistedOperationPipeline();
    }
}