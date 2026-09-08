using HotChocolate.Data;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Language;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

/// <summary>
/// Variable-aware list-size precedence on the <see cref="PagingTests.Query"/> schema
/// (hc-3-mmh.7 edges a-d): a supplied slicing-argument variable prices the list at the
/// coerced value, not at the query's static shape. Expected numbers are the locked,
/// engine-confirmed values from hc-3-whv.6's fix direction.
/// </summary>
public sealed class VariableAwareCostTests
{
    [Fact(Skip = "enabled by hc-costplan-middleware")]
    public async Task Evaluate_Should_PriceSuppliedValue_When_FirstArgumentIsAVariable()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request =
            OperationRequestBuilder.New()
                .SetDocument("query($first: Int) { books(first: $first) { nodes { title } } }")
                .SetVariableValues("""{ "first": 3 }""")
                .ReportCost()
                .Build();

        // act
        var (typeCost, fieldCost) = await ExecuteAndGetOperationCost(requestExecutor, request);

        // assert
        Assert.Equal(11, typeCost);
        Assert.Equal(5, fieldCost);
    }

    [Fact(Skip = "enabled by hc-costplan-middleware")]
    public async Task Evaluate_Should_ClampToZero_When_FirstArgumentIsNegative()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request =
            OperationRequestBuilder.New()
                .SetDocument("query($first: Int) { books(first: $first) { nodes { title } } }")
                .SetVariableValues("""{ "first": -1 }""")
                .ReportCost()
                .Build();

        // act
        var (typeCost, fieldCost) = await ExecuteAndGetOperationCost(requestExecutor, request);

        // assert
        Assert.Equal(11, typeCost);
        Assert.Equal(2, fieldCost);
    }

    [Fact(Skip = "enabled by hc-costplan-middleware")]
    public async Task Evaluate_Should_StayZero_When_FirstArgumentIsZero()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request =
            OperationRequestBuilder.New()
                .SetDocument("query($first: Int) { books(first: $first) { nodes { title } } }")
                .SetVariableValues("""{ "first": 0 }""")
                .ReportCost()
                .Build();

        // act
        var (typeCost, fieldCost) = await ExecuteAndGetOperationCost(requestExecutor, request);

        // assert
        Assert.Equal(11, typeCost);
        Assert.Equal(2, fieldCost);
    }

    [Fact(Skip = "enabled by hc-costplan-middleware")]
    public async Task Evaluate_Should_PreferSuppliedValue_When_SlicingArgumentDefaultValueIsAlsoConfigured()
    {
        // arrange
        // DefaultPageSize (=> slicingArgumentDefaultValue: 10) is configured by
        // CreateRequestExecutorBuilder, but rank 2 (a present slicing argument) beats
        // rank 3 (slicingArgumentDefaultValue), so the supplied value of 5 still wins.
        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request =
            OperationRequestBuilder.New()
                .SetDocument("query($first: Int) { books(first: $first) { nodes { title } } }")
                .SetVariableValues("""{ "first": 5 }""")
                .ReportCost()
                .Build();

        // act
        var (typeCost, fieldCost) = await ExecuteAndGetOperationCost(requestExecutor, request);

        // assert
        Assert.Equal(11, typeCost);
        Assert.Equal(7, fieldCost);
    }

    [Fact(Skip = "enabled by hc-costplan-middleware")]
    public async Task Evaluate_Should_PriceSuppliedValue_When_LastArgumentIsAVariable()
    {
        // arrange
        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request =
            OperationRequestBuilder.New()
                .SetDocument("query($last: Int) { books(last: $last) { nodes { title } } }")
                .SetVariableValues("""{ "last": 3 }""")
                .ReportCost()
                .Build();

        // act
        var (typeCost, fieldCost) = await ExecuteAndGetOperationCost(requestExecutor, request);

        // assert: backward pagination prices the same as forward pagination for the same
        // supplied multiplier.
        Assert.Equal(11, typeCost);
        Assert.Equal(5, fieldCost);
    }

    [Fact(Skip = "enabled by hc-costplan-middleware")]
    public async Task Evaluate_Should_FallToSlicingArgumentDefaultValue_When_NoSlicingArgumentIsSupplied()
    {
        // arrange
        // R-NULL-VARIABLE: an undefined variable behaves as an absent argument. With no
        // real schema argument default on `first`, the field falls through to
        // slicingArgumentDefaultValue (DefaultPageSize = 10, configured below).
        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request =
            OperationRequestBuilder.New()
                .SetDocument("query($first: Int) { books(first: $first) { nodes { title } } }")
                .ReportCost()
                .Build();

        // act
        var (typeCost, fieldCost) = await ExecuteAndGetOperationCost(requestExecutor, request);

        // assert
        Assert.Equal(11, typeCost);
        Assert.Equal(12, fieldCost);
    }

    [Fact(Skip = "enabled by hc-costplan-middleware")]
    public async Task Evaluate_Should_ChargeOneBranch_When_IncludeAndSkipAreComplementary()
    {
        // arrange
        var snapshot = new Snapshot();

        var requestExecutor = await CreateRequestExecutorBuilder()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var operation =
            Utf8GraphQLParser.Parse(
                """
                query($x: Boolean!) {
                    a: books(first: 3) @include(if: $x) { nodes { title } }
                    b: books(first: 3) @skip(if: $x) { nodes { title } }
                }
                """);

        var request =
            OperationRequestBuilder.New()
                .SetDocument(operation)
                .SetVariableValues("""{ "x": true }""")
                .ReportCost()
                .Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert: exactly one of the two `books` selections is active for either value of
        // $x, so the reported cost must be the same as a single `books(first: 3)` call.
        await snapshot
            .Add(operation, "Operation")
            .AddResult(response.ExpectOperationResult(), "Result")
            .MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    [Fact(Skip = "enabled by hc-costplan-middleware")]
    public async Task Evaluate_Should_PriceInterfaceSelectedField_When_ObjectTypeCarriesListSizeAnnotation()
    {
        // arrange
        // R-INTERFACE-FIELDS: the interceptor annotates object types only, never
        // interfaces, so a paging field selected through an interface is priced via each
        // possible object type's own @listSize definition.
        var snapshot = new Snapshot();

        const string sdl =
            """
            interface Library {
                books(first: Int, last: Int): BookConnection
            }

            type PublicLibrary implements Library {
                books(first: Int, last: Int): BookConnection
                    @listSize(slicingArguments: ["first", "last"], sizedFields: ["nodes"])
            }

            type BookConnection {
                nodes: [Book!]
            }

            type Book {
                title: String
            }

            type Query {
                library: Library
            }
            """;

        var operation =
            Utf8GraphQLParser.Parse(
                """
                query($first: Int) {
                    library {
                        books(first: $first) {
                            nodes { title }
                        }
                    }
                }
                """);

        var requestExecutor =
            await new ServiceCollection()
                .AddGraphQLServer()
                .AddDocumentFromString(sdl)
                .ModifyCostOptions(o => o.DefaultResolverCost = null)
                .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        var request =
            OperationRequestBuilder.New()
                .SetDocument(operation)
                .SetVariableValues("""{ "first": 3 }""")
                .ReportCost()
                .Build();

        // act
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);

        // assert
        await snapshot
            .Add(operation, "Operation")
            .AddResult(response.ExpectOperationResult(), "Result")
            .MatchMarkdownAsync(TestContext.Current.CancellationToken);
    }

    private static async Task<(double TypeCost, double FieldCost)> ExecuteAndGetOperationCost(
        IRequestExecutor requestExecutor,
        IOperationRequest request)
    {
        var response = await requestExecutor.ExecuteAsync(request, TestContext.Current.CancellationToken);
        var operationCost = (IReadOnlyDictionary<string, object?>)response.ExpectOperationResult().Extensions["operationCost"]!;
        return (Convert.ToDouble(operationCost["typeCost"]), Convert.ToDouble(operationCost["fieldCost"]));
    }

    private static IRequestExecutorBuilder CreateRequestExecutorBuilder()
        => new ServiceCollection()
            .AddGraphQLServer()
            .AddQueryType<PagingTests.Query>()
            .AddFiltering()
            .AddSorting()
            .ModifyPagingOptions(o => o.DefaultPageSize = 10)
            .ModifyCostOptions(o => o.DefaultResolverCost = null);
}
