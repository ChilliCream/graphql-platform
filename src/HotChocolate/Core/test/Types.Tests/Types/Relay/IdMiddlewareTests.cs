using System.Threading.Tasks;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Types.Relay
{
    public class IdMiddlewareTests
    {
        [Fact]
        public async Task ExecuteQueryThatReturnsId_IdShouldBeOpaque()
        {
            // arrange
            var schema = Schema.Create(t =>
            {
                t.RegisterQueryType<SomeQuery>();
                t.UseGlobalObjectIdentifier();
            });

            IQueryExecutor executor = schema.MakeExecutable();

            // act
            IExecutionResult result =
                await executor.ExecuteAsync("{ id string }");

            // assert
            result.MatchSnapshot();
        }

        public class SomeQuery
        {
            [GraphQLType(typeof(NonNullType<IdType>))]
            public string GetId() => "Hello";

            public string GetString() => "Hello";
        }
    }
}
