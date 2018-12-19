using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Execution
{
    public class IntrospectionTests
    {
        [Fact]
        public async Task TypeNameIntrospectionOnQuery()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = "{ __typename }";

            // act
            IExecutionResult result = await schema.ExecuteAsync(query);

            // assert
            Assert.Null(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task TypeNameIntrospectionNotOnQuery()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = "{ b { __typename } }";

            // act
            IExecutionResult result = await schema.ExecuteAsync(query);

            // assert
            Assert.Null(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task TypeIntrospectionOnQuery()
        {
            // arrange
            Schema schema = CreateSchema();
            string query = "{ __type (type: \"Foo\") { name } }";

            // act
            IExecutionResult result = await schema.ExecuteAsync(query);

            // assert
            Assert.Null(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task TypeIntrospectionOnQueryWithFields()
        {
            // arrange
            Schema schema = CreateSchema();
            string query =
                "{ __type (type: \"Foo\") " +
                "{ name fields { name type { name } } } }";

            // act
            IExecutionResult result = await schema.ExecuteAsync(query);

            // assert
            Assert.Null(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task ExecuteGraphiQLIntrospectionQuery()
        {
            // arrange
            Schema schema = CreateSchema();
            string query =
                FileResource.Open("IntrospectionQuery.graphql");

            // act
            IExecutionResult result = await schema.ExecuteAsync(query);

            // assert
            Assert.Null(result.Errors);
            result.Snapshot();
        }

        private static Schema CreateSchema()
        {
            return Schema.Create(c => c.RegisterType<Query>());
        }

        private class Query
            : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Field("a")
                    .Type<StringType>()
                    .Resolver(() => "a");

                descriptor.Field("b")
                    .Type<Foo>()
                    .Resolver(() => new object());
            }
        }

        private class Foo
            : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Field("a")
                    .Type<StringType>()
                    .Resolver(() => "foo.a");
            }
        }
    }
}
