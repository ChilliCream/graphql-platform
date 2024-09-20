using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

public class NullErrorPropagation
{
    [Fact]
    public async Task Lists_NullableElementIsNull()
    {
        // arrange
        using var snapshot = SnapshotHelpers.StartResultSnapshot();

        var executor = await CreateExecutorAsync();

        var request =
            OperationRequestBuilder.New()
                .SetDocument(
                    """
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
                    }
                    """)
                .AddGlobalState("a", null)
                .AddGlobalState("b", "not_null")
                .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        snapshot.Add(result);
    }

    [InlineData("nullable_list_nullable_element")]
    [InlineData("nonnull_list_nullable_element")]
    [InlineData("nullable_list_nonnull_element")]
    [InlineData("nonnull_list_nonnull_element")]
    [Theory]
    public async Task List_NonNullElementIsNull(string fieldType)
    {
        // arrange
        using var snapshot = SnapshotHelpers.StartResultSnapshot(fieldType);

        var executor = await CreateExecutorAsync();

        var request =
            OperationRequestBuilder.New()
                .SetDocument($"{{ foo {{ {fieldType} {{ b }} }} }}")
                .AddGlobalState("b", null)
                .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        snapshot.Add(result);
    }

    [InlineData("nullable_list_nullable_element")]
    [InlineData("nullable_list_nonnull_element")]
    [InlineData("nonnull_list_nullable_element")]
    [InlineData("nonnull_list_nonnull_element")]
    [Theory]
    public async Task List_NonNullElementHasError(string fieldType)
    {
        // arrange
        using var snapshot = SnapshotHelpers.StartResultSnapshot(fieldType);

        var executor = await CreateExecutorAsync();

        var request =
            OperationRequestBuilder.New()
                .SetDocument($"{{ foo {{ {fieldType} {{ c }} }} }}")
                .AddGlobalState("b", null)
                .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        snapshot.Add(result);
    }

    [InlineData("nonnull_prop")]
    [InlineData("nullable_prop")]
    [Theory]
    public async Task Object_NonNullElementIsNull(string fieldType)
    {
        // arrange
        using var snapshot = SnapshotHelpers.StartResultSnapshot(fieldType);

        var executor = await CreateExecutorAsync();

        var request =
            OperationRequestBuilder.New()
                .SetDocument($"{{ foo {{ {fieldType} {{ b }} }} }}")
                .AddGlobalState("b", null)
                .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        snapshot.Add(result);
    }

    [InlineData("nonnull_prop")]
    [InlineData("nullable_prop")]
    [Theory]
    public async Task Object_NonNullElementHasError(string fieldType)
    {
        // arrange
        using var snapshot = SnapshotHelpers.StartResultSnapshot(fieldType);

        var executor = await CreateExecutorAsync();

        var request =
            OperationRequestBuilder.New()
                .SetDocument($"{{ foo {{ {fieldType} {{ c }} }} }}")
                .AddGlobalState("b", null)
                .Build();

        // act
        var result = await executor.ExecuteAsync(request);

        // assert
        snapshot.Add(result);
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync()
    {
        const string schema =
            """
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
            """;

        return await new ServiceCollection()
            .AddGraphQL()
            .AddDocumentFromString(schema)
            .AddResolver("Query", "foo", _ => new(new object()))
            .AddResolver("Foo", "nullable_list_nullable_element", _ => new(new[] { new object() }))
            .AddResolver("Foo", "nonnull_list_nullable_element", _ => new(new[] { new object() }))
            .AddResolver("Foo", "nullable_list_nonnull_element", _ => new(new[] { new object() }))
            .AddResolver("Foo", "nonnull_list_nonnull_element", _ => new(new[] { new object() }))
            .AddResolver("Foo", "nonnull_prop", _ => new(new object()))
            .AddResolver("Foo", "nullable_prop", _ => new(new object()))
            .AddResolver("Bar", "a", c => new(c.GetGlobalStateOrDefault<string>("a")))
            .AddResolver("Bar", "b", c => new(c.GetGlobalStateOrDefault<string>("b")))
            .AddResolver("Bar", "c", _ => throw new GraphQLException("ERROR"))
            .BuildRequestExecutorAsync();
    }
}
