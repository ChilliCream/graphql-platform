using HotChocolate.CostAnalysis.Caching;
using HotChocolate.CostAnalysis.Doubles;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace HotChocolate.CostAnalysis;

public sealed class CachingTests
{
    [Fact]
    public async Task Execute_SameQueryTwice_UsesCostMetricsCache()
    {
        // arrange
        const string schema =
            """
            type Query {
                examples(limit: Int! @cost(weight: "2.0")): [Example!]!
                    @cost(weight: "3.0") @listSize(slicingArguments: ["limit"])
            }

            type Example @cost(weight: "4.0") {
                exampleField1: Boolean!
                exampleField2: Int!
            }
            """;

        const string operation =
            """
            query {
                examples(limit: 10) {
                    exampleField1
                    exampleField2
                }
            }
            """;

        var request =
            OperationRequestBuilder.New()
                .SetDocument(operation)
                .ReportCost()
                .Build();

        var requestExecutor = await CreateRequestExecutorBuilder()
            .AddDocumentFromString(schema)
            .BuildRequestExecutorAsync();

        var cache = (FakeCostMetricsCache)requestExecutor.Schema.Services
            .GetRequiredService<ICostMetricsCache>();

        // act
        await requestExecutor.ExecuteAsync(request);
        await requestExecutor.ExecuteAsync(request);

        // assert
        Assert.Equal(1, cache.Misses);
        Assert.Equal(1, cache.Additions);
        Assert.Equal(1, cache.Hits);
    }

    private static IRequestExecutorBuilder CreateRequestExecutorBuilder()
    {
        var requestExecutorBuilder = new ServiceCollection()
            .AddGraphQLServer()
            .UseField(next => next);

        requestExecutorBuilder.Services.Replace(
            new ServiceDescriptor(
                typeof(ICostMetricsCache),
                new FakeCostMetricsCache()));

        return requestExecutorBuilder;
    }
}
