using System;
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
                    c.Options.StrictValidation = false;
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

        [Fact]
        public async Task SchemaBuilder_BindType_And_Resolver_NameBind()
        {
            // arrange
            string sourceText = "type Query { hello: String }";

            // act
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(sourceText)
                .BindComplexType<Query>()
                .BindResolver<QueryResolver>(c => c
                    .To("Query")
                    .Resolve("hello")
                    .With(r => r.Resolve(default)))
                .Create();

            // assert
            IQueryExecutor executor = schema.MakeExecutable();
            IExecutionResult result =
                await executor.ExecuteAsync("{ hello }");
            result.MatchSnapshot();
        }


        [Fact]
        public async Task SchemaBuilder_BindType_And_Resolver_Implicit()
        {
            // arrange
            string sourceText = "type Query { hello: String }";

            // act
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(sourceText)
                .BindResolver<Query>()
                .Create();

            // assert
            IQueryExecutor executor = schema.MakeExecutable();
            IExecutionResult result =
                await executor.ExecuteAsync("{ hello }");
            result.MatchSnapshot();
        }

        [Fact]
        public void DirectiveArgumentsAreValidated()
        {
            // arrange
            string sourceText = @"
                type Query {
                    foo: String @a(b:1 e:true)
                }

                directive @a(c:Int d:Int! e:Int) on FIELD_DEFINITION
            ";

            // act
            Action action = () => SchemaBuilder.New()
                .AddDocumentFromString(sourceText)
                .AddResolver("Query", "foo", "bar")
                .Create();

            // assert
            Assert.Collection(
                Assert.Throws<SchemaException>(action).Errors,
                    error => Assert.Equal(
                        TypeErrorCodes.InvalidArgument,
                        error.Code),
                    error => Assert.Equal(
                        TypeErrorCodes.ArgumentValueTypeWrong,
                        error.Code),
                    error => Assert.Equal(
                        TypeErrorCodes.NonNullArgument,
                        error.Code));
        }

        [Fact]
        public void BuiltInScalarsAreRecognized()
        {
            // arrange
            string sourceText = @"
                type Query {
                    string_field: String
                    string_non_null_field: String!
                    int_field: Int
                    int_non_null_field: Int!
                    float_field: Float
                    float_non_null_field: Float!
                    bool_field: Boolean
                    bool_non_null_field: Boolean!
                }
            ";

            // act
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(sourceText)
                .Use(next => context => Task.CompletedTask)
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void BuiltInScalarsAreRecognized2()
        {
            // arrange
            string sourceText = @"
                type Query {
                    foo: Foo
                }

                type Foo {
                    string_field: String
                    string_non_null_field: String!
                    int_field: Int
                    int_non_null_field: Int!
                    float_field: Float
                    float_non_null_field: Float!
                    bool_field: Boolean
                    bool_non_null_field: Boolean!
                }
            ";

            // act
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(sourceText)
                .Use(next => context => Task.CompletedTask)
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
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
