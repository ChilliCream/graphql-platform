using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate
{
    public class SchemaFirstTests
    {
        [Fact]
        public async Task CommentsAreCorrectlyRead()
        {
            // arrange
            string source = FileResource.Open(
                "schema_with_multiline_descriptions.graphql");
            string query = FileResource.Open(
                "IntrospectionQuery.graphql");

            // act
            ISchema schema = Schema.Create(
                source,
                c =>
                {
                    c.Use(next => context => next(context));
                });

            // assert
            IQueryExecutor executor = schema.MakeExecutable();
            IExecutionResult result = await executor.ExecuteAsync(query);
            result.MatchSnapshot();
        }
    }
}
