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
            string query = "{ __typename }";
            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = await executer.ExecuteAsync(query);

            // assert
            Assert.Empty(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task TypeNameIntrospectionNotOnQuery()
        {
            // arrange
            string query = "{ b { __typename } }";
            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = await executer.ExecuteAsync(query);

            // assert
            Assert.Empty(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task TypeIntrospectionOnQuery()
        {
            // arrange
            string query = "{ __type (type: \"Foo\") { name } }";
            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = await executer.ExecuteAsync(query);

            // assert
            Assert.Empty(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task TypeIntrospectionOnQueryWithFields()
        {
            // arrange
            string query =
                "{ __type (type: \"Foo\") " +
                "{ name fields { name type { name } } } }";
            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = await executer.ExecuteAsync(query);

            // assert
            Assert.Empty(result.Errors);
            result.Snapshot();
        }

        [Fact]
        public async Task ExecuteGraphiQLIntrospectionQuery()
        {
            // arrange
            string query =
                FileResource.Open("IntrospectionQuery.graphql");
            IQueryExecuter executer = CreateSchema().MakeExecutable();

            // act
            IExecutionResult result = await executer.ExecuteAsync(query);

            // assert
            Assert.Empty(result.Errors);
            result.Snapshot();
        }

        private static Schema CreateSchema()
        {
            return Schema.Create(c =>
            {
                c.RegisterExtendedScalarTypes();
                c.RegisterType<Query>();
            });
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
