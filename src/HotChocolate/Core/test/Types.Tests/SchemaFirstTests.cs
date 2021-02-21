using System;
using System.Threading.Tasks;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Types.Introspection;
using Microsoft.VisualBasic.CompilerServices;
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
            string source = FileResource.Open("schema_with_multiline_descriptions.graphql");
            string query = FileResource.Open("IntrospectionQuery.graphql");

            // act
            ISchema schema = Schema.Create(
                source,
                c =>
                {
                    c.Options.StrictValidation = false;
                    c.Use(next => context => next(context));
                });

            // assert
            IRequestExecutor executor = schema.MakeExecutable();
            IExecutionResult result = await executor.ExecuteAsync(query);
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task Interfaces_Impl_Interfaces_Are_Correctly_Exposed_Through_Introspection()
        {
            // arrange
            var source = @"
                type Query {
                    c: C
                }

                interface A {
                    a: String
                }

                interface B implements A {
                    a: String
                }

                type C implements A & B {
                    a: String
                }
            ";
            var query = FileResource.Open("IntrospectionQuery.graphql");

            // act
            ISchema schema = Schema.Create(
                source,
                c =>
                {
                    c.Options.StrictValidation = false;
                    c.Use(next => context => next(context));
                });

            // assert
            IRequestExecutor executor = schema.MakeExecutable();
            IExecutionResult result = await executor.ExecuteAsync(query);
            result.ToJson().MatchSnapshot();
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
            IRequestExecutor executor = schema.MakeExecutable();
            IExecutionResult result =
                await executor.ExecuteAsync("{ __schema { description } }");
            result.ToJson().MatchSnapshot();
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
            IRequestExecutor executor = schema.MakeExecutable();
            IExecutionResult result =
                await executor.ExecuteAsync("{ hello }");
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task SchemaBuilder_AddResolver()
        {
            // arrange
            string sourceText = "type Query { hello: String }";

            // act
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(sourceText)
                .AddResolver("Query", "hello", () => "World")
                .Create();

            // assert
            IRequestExecutor executor = schema.MakeExecutable();
            IExecutionResult result =
                await executor.ExecuteAsync("{ hello }");
            result.ToJson().MatchSnapshot();
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
            IRequestExecutor executor = schema.MakeExecutable();
            IExecutionResult result =
                await executor.ExecuteAsync("{ hello }");
            result.ToJson().MatchSnapshot();
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
            IRequestExecutor executor = schema.MakeExecutable();
            IExecutionResult result =
                await executor.ExecuteAsync("{ hello }");
            result.ToJson().MatchSnapshot();
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
            IRequestExecutor executor = schema.MakeExecutable();
            IExecutionResult result =
                await executor.ExecuteAsync("{ hello }");
            result.ToJson().MatchSnapshot();
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
            IRequestExecutor executor = schema.MakeExecutable();
            IExecutionResult result =
                await executor.ExecuteAsync("{ hello }");
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public void DirectiveArgumentsAreValidated()
        {
            // arrange
            var sourceText = @"
                type Query {
                    foo: String @a(b:1 e:true)
                }

                directive @a(c:Int d:Int! e:Int) on FIELD_DEFINITION
            ";

            // act
            void Action() =>
                SchemaBuilder.New()
                    .AddDocumentFromString(sourceText)
                    .AddResolver("Query", "foo", "bar")
                    .Create();

            // assert
            Assert.Collection(
                Assert.Throws<SchemaException>((Action) Action).Errors,
                    error => Assert.Equal(
                        ErrorCodes.Schema.InvalidArgument,
                        error.Code),
                    error => Assert.Equal(
                        ErrorCodes.Schema.ArgumentValueTypeWrong,
                        error.Code),
                    error => Assert.Equal(
                        ErrorCodes.Schema.NonNullArgument,
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
                .Use(next => context => default(ValueTask))
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
                .Use(next => context => default(ValueTask))
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void ListTypesAreRecognized()
        {
            // arrange
            string sourceText = @"
                type Query {
                    foo: Foo
                }

                type Foo {
                    single_int: Int
                    list_int: [Int]
                    matrix_int: [[Int]]
                    nullable_single_int: Int!
                    nullable_list_int: [Int!]!
                    nullable_matrix_int: [[Int!]!]!
                }
            ";

            // act
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(sourceText)
                .Use(next => context => default(ValueTask))
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
