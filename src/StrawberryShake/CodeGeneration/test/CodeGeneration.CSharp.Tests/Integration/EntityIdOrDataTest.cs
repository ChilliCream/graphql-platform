using HotChocolate.AspNetCore.Tests.Utilities;
using Microsoft.Extensions.DependencyInjection;
using HotChocolate.Types;
using Snapshooter.Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.EntityIdOrData;

public class EntityIdOrDataTest : ServerTestBase
{
    public EntityIdOrDataTest(TestServerFactory serverFactory) : base(serverFactory)
    {
    }

    [Fact]
    public async Task Execute_EntityIdOrData_Test()
    {
        // arrange
        var ct = new CancellationTokenSource(20_000).Token;
        var serviceCollection = new ServiceCollection();
        serviceCollection
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddType<IBar>()
            .AddType<Baz>()
            .AddType<Baz2>()
            .AddType<Quox>()
            .AddType<Quox2>();
        serviceCollection.AddEntityIdOrDataClient().ConfigureInMemoryClient();
        IServiceProvider services = serviceCollection.BuildServiceProvider();
        var client = services.GetRequiredService<EntityIdOrDataClient>();

        // act
        IOperationResult<IGetFooResult> result = await client.GetFoo.ExecuteAsync(ct);

        // assert
        result.MatchSnapshot();
    }

    public class Query
    {
        public IBar[] GetFoo() =>
        [
            new Baz { Id = "BarId", },
            new Baz2 { Id = "Bar2Id", },
            new Quox { Foo = "QuoxFoo", },
            new Quox2 { Foo = "Quox2Foo", },
        ];
    }

    [UnionType("Bar")]
    public interface IBar
    {
    }

    public class Baz : IBar
    {
        public string Id { get; set; } = default!;
    }

    public class Baz2 : IBar
    {
        public string Id { get; set; } = default!;
    }

    public class Quox : IBar
    {
        public string Foo { get; set; } = default!;
    }

    public class Quox2 : IBar
    {
        public string Foo { get; set; } = default!;
    }
}
