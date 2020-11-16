using System.Threading.Tasks;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;
using static HotChocolate.Tests.TestHelper;

namespace HotChocolate.Execution.Integration.HelloWorldCodeFirst
{
    public class HelloWorldCodeFirstTests
    {
        [Fact]
        public async Task ExecuteHelloWorldCodeFirstQuery()
        {
            Snapshot.FullName();
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
            Snapshot.FullName();
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
            Snapshot.FullName();
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
            Snapshot.FullName();
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
            Snapshot.FullName();
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
}
