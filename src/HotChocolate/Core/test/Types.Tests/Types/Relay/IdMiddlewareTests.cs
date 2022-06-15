using System.Collections.Generic;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Tests;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace HotChocolate.Types.Relay
{
    public class IdMiddlewareTests
    {
        [Fact]
        public async Task ExecuteQueryThatReturnsId_IdShouldBeOpaque()
        {
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<SomeQuery>()
                .AddGlobalObjectIdentification(false)
                .ExecuteRequestAsync("{ id string }")
                .MatchSnapshotAsync();
        }

        [Fact]
        public async Task CustomIds_IdsShouldBeOpaque()
        {
            await new ServiceCollection()
                .AddGraphQL()
                .AddQueryType<SomeQuery>()
                .AddTypeConverter<CustomId, int>(c => c.Value)
                //.BindRuntimeType<CustomId, IntType>()
                .AddGlobalObjectIdentification(false)
                .ExecuteRequestAsync("{ customId1 customId2 customIds }")
                .MatchSnapshotAsync();
        }

        public class SomeQuery
        {
            [ID]
            public string GetId() => "Hello";

            public string GetString() => "Hello";

            [ID]
            public CustomId CustomId1() => new CustomId(1);

            [ID]
            public CustomId CustomId2() => new CustomId(2);

            [ID]
            public List<CustomId> CustomIds()
                => new List<CustomId>
                {
                    new CustomId(1),
                    new CustomId(2)
                };
        }
    }
}
