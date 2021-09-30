using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using ChilliCream.Testing;
using HotChocolate.Execution;
using HotChocolate.Tests;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;
using Snapshot = Snapshooter.Xunit.Snapshot;

namespace HotChocolate
{
    public class SchemaFirstTests
    {
        [Fact]
        public async Task DescriptionsAreCorrectlyRead()
        {
            // arrange
            Snapshot.FullName();
            var source = FileResource.Open("schema_with_multiline_descriptions.graphql");
            var query = FileResource.Open("IntrospectionQuery.graphql");

            // act & act
            await new ServiceCollection()
                .AddGraphQL()
                .AddDocumentFromString(source)
                .ModifyOptions(o => o.SortFieldsByName = true)
                .UseField(next => next)
                .ExecuteRequestAsync(query)
                .MatchSnapshotAsync();
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
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(source)
                .Use(next => next)
                .ModifyOptions(o => o.StrictValidation = false)
                .Create();

            // assert
            IRequestExecutor executor = schema.MakeExecutable();
            IExecutionResult result = await executor.ExecuteAsync(query);
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task SchemaDescription()
        {
            // arrange
            var sourceText = "\"\"\"\nMy Schema Description\n\"\"\"" +
                "schema" +
                "{ query: Foo }" +
                "type Foo { bar: String }";

            // act
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(sourceText)
                .Use(next => next)
                .Create();

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
            var sourceText = "type Query { hello: String }";

            // act
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(sourceText)
                .AddRootResolver(new { Query = new Query() })
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
            var sourceText = "type Query { hello: String }";

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
                Assert.Throws<SchemaException>(Action).Errors,
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
            var sourceText = @"
                type Query {
                    string_field: String
                    string_non_null_field: String!
                    int_field: Int
                    int_non_null_field: Int!
                    float_field: Float
                    float_non_null_field: Float!
                    bool_field: Boolean
                    bool_non_null_field: Boolean!
                }";

            // act
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(sourceText)
                .Use(_ => _)
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void BuiltInScalarsAreRecognized2()
        {
            // arrange
            var sourceText = @"
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
                .Use(_ => _)
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void ListTypesAreRecognized()
        {
            // arrange
            var sourceText = @"
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
                .Use(_ => _)
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public async Task SchemaBuilder_AnyType()
        {
            // arrange
            var sourceText = "type Query { hello: Any }";

            // act
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(sourceText)
                .AddResolver<Query>()
                .Create();

            // assert
            IRequestExecutor executor = schema.MakeExecutable();
            IExecutionResult result = await executor.ExecuteAsync("{ hello }");
            result.ToJson().MatchSnapshot();
        }

        [Fact]
        public async Task SchemaFirst_Cursor_Paging()
        {
            // arrange
            var sdl = "type Query { items: [String!] }";

            // act
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddDocumentFromString(sdl)
                    .BindRuntimeType<QueryWithItems>("Query")
                    .BuildSchemaAsync();

            // assert
            schema.Print().MatchSnapshot();
        }

        [Fact]
        public async Task SchemaFirst_Cursor_OffsetPaging()
        {
            // arrange
            var sdl = "type Query { items: [String!] }";

            // act
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddDocumentFromString(sdl)
                    .BindRuntimeType<QueryWithOffsetItems>("Query")
                    .BuildSchemaAsync();

            // assert
            schema.Print().MatchSnapshot();
        }

        [Fact]
        public async Task SchemaFirst_Cursor_Paging_With_Objects()
        {
            // arrange
            var sdl = "type Query { items: [Person!] } type Person { name: String }";

            // act
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddDocumentFromString(sdl)
                    .BindRuntimeType<QueryWithPersons>("Query")
                    .BuildSchemaAsync();

            // assert
            schema.Print().MatchSnapshot();
        }

        // we need to apply the changes we did to cursor paging to offset paging.
        [Fact(Skip = "Offset paging for schema first is not supported in 12.")]
        public async Task SchemaFirst_Cursor_OffSetPaging_With_Objects()
        {
            // arrange
            var sdl = "type Query { items: [Person!] } type Person { name: String }";

            // act
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddDocumentFromString(sdl)
                    .BindRuntimeType<QueryWithOffsetPersons>("Query")
                    .BuildSchemaAsync();

            // assert
            schema.Print().MatchSnapshot();
        }

        [Fact]
        public async Task SchemaFirst_Cursor_Paging_Execute()
        {
            // arrange
            var sdl = "type Query { items: [String!] }";

            // act
            IExecutionResult result =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddDocumentFromString(sdl)
                    .BindRuntimeType<QueryWithItems>("Query")
                    .ExecuteRequestAsync("{ items { nodes } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SchemaFirst_Cursor_Paging_With_Objects_Execute()
        {
            // arrange
            var sdl = "type Query { items: [Person!] } type Person { name: String }";

            // act
            IExecutionResult result =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddDocumentFromString(sdl)
                    .BindRuntimeType<QueryWithPersons>("Query")
                    .ExecuteRequestAsync("{ items { nodes { name } } }");

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task SchemaFirst_Cursor_Paging_With_Resolver()
        {
            // arrange
            var sdl = "type Query { items: [String!] }";

            // act
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddDocumentFromString(sdl)
                    .AddResolver<QueryWithItems>("Query")
                    .BuildSchemaAsync();

            // assert
            schema.Print().MatchSnapshot();
        }

        [Fact]
        public async Task Reference_Schema_First_Types_From_Code_First_Models()
        {
            // arrange
            var sdl = "type Person { name: String! }";

            // act
            ISchema schema =
                await new ServiceCollection()
                    .AddGraphQL()
                    .AddDocumentFromString(sdl)
                    .AddQueryType<QueryCodeFirst>()
                    .BindRuntimeType<Person>()
                    .BuildSchemaAsync();

            // assert
            schema.Print().MatchSnapshot();
        }
    }

    public class Query
    {
        public string Hello() => "World";
    }

    public class QueryWithItems
    {
        [UsePaging]
        public string[] GetItems() => new[] { "a", "b" };
    }

    public class QueryWithOffsetItems
    {
        [UseOffsetPaging]
        public string[] GetItems() => new[] { "a", "b" };
    }

    public class QueryWithPersons
    {
        [UsePaging]
        public Person[] GetItems() => new[] { new Person { Name = "Foo" } };
    }

    public class QueryWithOffsetPersons
    {
        [UseOffsetPaging]
        public Person[] GetItems() => new[] { new Person { Name = "Foo" } };
    }

    public class QueryCodeFirst
    {
        [GraphQLType("Person!")]
        public object GetPerson() => new Person { Name = "Hello" };
    }

    public class Person
    {
        public string Name { get; set; }
    }
}
