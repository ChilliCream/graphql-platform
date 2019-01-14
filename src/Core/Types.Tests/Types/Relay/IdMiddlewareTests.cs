using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
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

            IQueryExecuter executer = schema.MakeExecutable();

            // act
            IExecutionResult result =
                await executer.ExecuteAsync("{ id string }");

            // assert
            result.Snapshot();
        }

        public class SomeQuery
        {
            [GraphQLType(typeof(NonNullType<IdType>))]
            public string GetId() => "Hello";

            public string GetString() => "Hello";
        }
    }
}
