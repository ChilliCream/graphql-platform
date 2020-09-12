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
            IExecutionResult result = await executor.ExecuteAsync(
                request, CancellationToken.None);

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
            string schema = @"
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

            return Schema.Create(schema, c =>
            {
                c.BindResolver(ctx => new object())
                    .To("Query", "foo");
                c.BindResolver(ctx => new[] { new object() })
                    .To("Foo", "nullable_list_nullable_element");
                c.BindResolver(ctx => new[] { new object() })
                    .To("Foo", "nonnull_list_nullable_element");
                c.BindResolver(ctx => new[] { new object() })
                    .To("Foo", "nullable_list_nonnull_element");
                c.BindResolver(ctx => new[] { new object() })
                    .To("Foo", "nonnull_list_nonnull_element");
                c.BindResolver(ctx => new object())
                    .To("Foo", "nonnull_prop");
                c.BindResolver(ctx => new object())
                    .To("Foo", "nullable_prop");
                c.BindResolver(ctx => ctx.GetGlobalValue<string>("a"))
                    .To("Bar", "a");
                c.BindResolver(ctx => ctx.GetGlobalValue<string>("b"))
                    .To("Bar", "b");
                c.BindResolver(ctx => ErrorBuilder.New()
                    .SetMessage("ERROR").Build())
                    .To("Bar", "c");
            }).MakeExecutable();
        }
    }
}
