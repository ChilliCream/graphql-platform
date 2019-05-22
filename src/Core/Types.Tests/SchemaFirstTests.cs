using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate
{
    public class SchemaFirstTests
    {
        [Fact]
        public async Task DescriptionsAreCorrectlyRead()
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

        [Fact]
        public async Task SchemaDescription()
        {
            // arrange
            string sourceText = "\"\"\"\nMy Schema Description\n\"\"\"" +
                "schema" +
                "{ query: Foo }" +
                "type Foo { bar: String }";

            // act
            ISchema schema = Schema.Create(
                sourceText,
                c =>
                {
                    c.Use(next => context => next(context));
                });

            // assert
            IQueryExecutor executor = schema.MakeExecutable();
            IExecutionResult result =
                await executor.ExecuteAsync("{ __schema { description } }");
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SchemaBuilder_BindType()
        {
            // arrange
            string sourceText = "type Query { hello: String }";

            // act
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(sourceText)
                .BindComplexType<Query>()
                .Create();

            // assert
            IQueryExecutor executor = schema.MakeExecutable();
            IExecutionResult result =
                await executor.ExecuteAsync("{ hello }");
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SchemaBuilder_BindType_Configure()
        {
            // arrange
            string sourceText = "type Query { hello: String }";

            // act
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(sourceText)
                .BindComplexType<Query1>(c => c
                    .To("Query")
                    .Field(t => t.Hello1())
                    .Name("hello"))
                .Create();

            // assert
            IQueryExecutor executor = schema.MakeExecutable();
            IExecutionResult result =
                await executor.ExecuteAsync("{ hello }");
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SchemaBuilder_BindType_And_Resolver()
        {
            // arrange
            string sourceText = "type Query { hello: String }";

            // act
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(sourceText)
                .BindComplexType<Query>()
                .BindResolver<QueryResolver>(c => c
                    .To<Query>()
                    .Resolve(f => f.Hello())
                    .With(r => r.Resolve(default)))
                .Create();

            // assert
            IQueryExecutor executor = schema.MakeExecutable();
            IExecutionResult result =
                await executor.ExecuteAsync("{ hello }");
            result.MatchSnapshot();
        }

        public class Query
        {
            public string Hello() => "World";
        }

        public class Query1
        {
            public string Hello1() => "World1";
        }

        public class QueryResolver
        {
            public string Resolve(Query query)
            {
                return query.Hello() + " with resolver";
            }
        }
    }
}
