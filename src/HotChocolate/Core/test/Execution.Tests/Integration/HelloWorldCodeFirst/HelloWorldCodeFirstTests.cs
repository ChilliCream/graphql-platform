using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Execution.Integration.HelloWorldCodeFirst;

public class HelloWorldCodeFirstTests
{
    [Fact]
    public async Task ExecuteHelloWorldCodeFirstQuery()
    {
        await ExpectValid(
                "{ hello state }",
                configure: c => c
                    .AddQueryType<QueryHelloWorld>()
                    .AddMutationType<MutationHelloWorld>()
                    .Services
                    .AddSingleton<DataStoreHelloWorld>())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task ExecuteHelloWorldCodeFirstQueryWithArgument()
    {
        await ExpectValid(
                "{ hello(to: \"me\") state }",
                configure: c => c
                    .AddQueryType<QueryHelloWorld>()
                    .AddMutationType<MutationHelloWorld>()
                    .Services
                    .AddSingleton<DataStoreHelloWorld>())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task ExecuteHelloWorldCodeFirstClrQuery()
    {
        await ExpectValid(
                "{ hello state }",
                configure: c => c
                    .AddQueryType<QueryHelloWorldClr>()
                    .Services
                    .AddSingleton<DataStoreHelloWorld>())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task ExecuteHelloWorldCodeFirstClrQueryWithArgument()
    {
        await ExpectValid(
                "{ hello(to: \"me\") state }",
                configure: c => c
                    .AddQueryType<QueryHelloWorldClr>()
                    .Services
                    .AddSingleton<DataStoreHelloWorld>())
            .MatchSnapshotAsync();
    }

    [Fact]
    public async Task ExecuteHelloWorldCodeFirstMutation()
    {
        await ExpectValid(
                "mutation { newState(state:\"1234567\") }",
                configure: c => c
                    .AddQueryType<QueryHelloWorld>()
                    .AddMutationType<MutationHelloWorld>()
                    .Services
                    .AddSingleton<DataStoreHelloWorld>())
            .MatchSnapshotAsync();
    }
}
