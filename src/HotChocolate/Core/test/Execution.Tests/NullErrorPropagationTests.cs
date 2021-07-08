using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Tests;
using Xunit;

namespace HotChocolate.Execution
{
    public class NullErrorPropagation
    {
        [Fact]
        public async Task Lists_NullableElementIsNull()
        {
            // arrange
            IRequestExecutor executor = CreateExecutor();

            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery(@"
                        {
                            foo {
                                nullable_list_nullable_element
                                {
                                    a
                                    b
                                }
                                nonnull_list_nullable_element
                                {
                                    a
                                    b
                                }
                                nullable_list_nonnull_element
                                {
                                    a
                                    b
                                }
                                nonnull_list_nonnull_element
                                {
                                    a
                                    b
                                }
                            }
                        }")
                    .AddProperty("a", null)
                    .AddProperty("b", "not_null")
                    .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                request, CancellationToken.None);

            // assert
            result.MatchSnapshot();
        }

        [InlineData("nullable_list_nullable_element")]
        [InlineData("nonnull_list_nullable_element")]
        [InlineData("nullable_list_nonnull_element")]
        [InlineData("nonnull_list_nonnull_element")]
        [Theory]
        public async Task List_NonNullElementIsNull(string fieldType)
        {
            // arrange
            IRequestExecutor executor = CreateExecutor();

            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery($"{{ foo {{ {fieldType} {{ b }} }} }}")
                    .AddProperty("b", null)
                    .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                request, CancellationToken.None);

            // assert
            result.MatchSnapshot(fieldType);
        }

        [InlineData("nullable_list_nullable_element")]
        [InlineData("nullable_list_nonnull_element")]
        [InlineData("nonnull_list_nullable_element")]
        [InlineData("nonnull_list_nonnull_element")]
        [Theory]
        public async Task List_NonNullElementHasError(string fieldType)
        {
            // arrange
            IRequestExecutor executor = CreateExecutor();

            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery($"{{ foo {{ {fieldType} {{ c }} }} }}")
                    .AddProperty("b", null)
                    .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(request, CancellationToken.None);

            // assert
            result.MatchSnapshot(fieldType);
        }

        [InlineData("nonnull_prop")]
        [InlineData("nullable_prop")]
        [Theory]
        public async Task Object_NonNullElementIsNull(string fieldType)
        {
            // arrange
            IRequestExecutor executor = CreateExecutor();

            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery($"{{ foo {{ {fieldType} {{ b }} }} }}")
                    .AddProperty("b", null)
                    .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                request, CancellationToken.None);

            // assert
            result.MatchSnapshot(fieldType);
        }

        [InlineData("nonnull_prop")]
        [InlineData("nullable_prop")]
        [Theory]
        public async Task Object_NonNullElementHasError(string fieldType)
        {
            // arrange
            IRequestExecutor executor = CreateExecutor();

            IReadOnlyQueryRequest request =
                QueryRequestBuilder.New()
                    .SetQuery($"{{ foo {{ {fieldType} {{ c }} }} }}")
                    .AddProperty("b", null)
                    .Create();

            // act
            IExecutionResult result = await executor.ExecuteAsync(
                request, CancellationToken.None);

            // assert
            result.MatchSnapshot(fieldType);
        }

        private IRequestExecutor CreateExecutor()
        {
            var schema = @"
                type Query {
                    foo: Foo
                }

                type Foo {
                    nullable_list_nullable_element: [Bar]
                    nonnull_list_nullable_element: [Bar]!
                    nullable_list_nonnull_element: [Bar!]
                    nonnull_list_nonnull_element: [Bar!]!
                    nonnull_prop: Bar!
                    nullable_prop: Bar
                }

                type Bar {
                    a: String
                    b: String!
                    c: String!
                }
                ";

            return SchemaBuilder.New()
                .AddDocumentFromString(schema)
                .AddResolver("Query.foo", _ => new(new object()))
                .AddResolver("Foo.nullable_list_nullable_element", _ => new(new[] { new object() }))
                .AddResolver("Foo.nonnull_list_nullable_element", _ => new(new[] { new object() }))
                .AddResolver("Foo.nullable_list_nonnull_element", _ => new(new[] { new object() }))
                .AddResolver("Foo.nonnull_list_nonnull_element", _ => new(new[] { new object() }))
                .AddResolver("Foo.nonnull_prop", _ => new(new object()))
                .AddResolver("Foo.nullable_prop", _ => new(new object()))
                .AddResolver("Bar.a", c => new(c.GetGlobalValue<string>("a")))
                .AddResolver("Bar.b", c => new(c.GetGlobalValue<string>("b")))
                .AddResolver("Bar.c", _ => throw new GraphQLException("ERROR"))
                .Create()
                .MakeExecutable();
        }
    }
}
