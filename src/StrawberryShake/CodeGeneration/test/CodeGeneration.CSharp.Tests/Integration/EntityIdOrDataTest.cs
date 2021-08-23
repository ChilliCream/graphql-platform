using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.AspNetCore.Utilities;
using HotChocolate.Types;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using StrawberryShake.Transport.WebSockets;
using Xunit;

namespace StrawberryShake.CodeGeneration.CSharp.Integration.EntityIdOrData
{
    public class EntityIdOrDataTest : ServerTestBase
    {
        public EntityIdOrDataTest(TestServerFactory serverFactory) : base(serverFactory)
        {
        }

        [Fact]
        public async Task Execute_EntityIdOrData_Test()
        {
            // arrange
            CancellationToken ct = new CancellationTokenSource(20_000).Token;
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
            EntityIdOrDataClient client = services.GetRequiredService<EntityIdOrDataClient>();

            // act
            IOperationResult<IGetFooResult> result = await client.GetFoo.ExecuteAsync(ct);

            // assert
            result.MatchSnapshot();
        }

        public class Query
        {
            public IBar[] GetFoo() => new IBar[]
            {
                new Baz { Id = "BarId" },
                new Baz2 { Id = "Bar2Id" },
                new Quox { Foo = "QuoxFoo" },
                new Quox2 { Foo = "Quox2Foo" }
            };
        }

        [UnionType("Bar")]
        public interface IBar
        {
        }

        public class Baz : IBar
        {
            public string Id { get; set; }
        }

        public class Baz2 : IBar
        {
            public string Id { get; set; }
        }

        public class Quox : IBar
        {
            public string Foo { get; set; }
        }

        public class Quox2 : IBar
        {
            public string Foo { get; set; }
        }
    }
}
