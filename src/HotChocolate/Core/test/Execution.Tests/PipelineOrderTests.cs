using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HotChocolate.Execution;

public class PipelineOrderTests
{
    [Theory]
    [MemberData(nameof(Pipelines))]
    public void Pipeline_Should_Order_CostAnalyzer_After_VariableCoercion_When_AddCostAnalyzer_Applied(
        Func<IRequestExecutorBuilder, IRequestExecutorBuilder> configurePipeline,
        string?[] expectedKeys)
    {
        // arrange
        var builder = new ServiceCollection().AddGraphQL();
        configurePipeline(builder).AddCostAnalyzer();

        // act
        var keys = GetPipelineKeys(builder);

        // assert
        Assert.Equal(expectedKeys, keys);
    }

    public static TheoryData<Func<IRequestExecutorBuilder, IRequestExecutorBuilder>, string?[]> Pipelines()
    {
        return new TheoryData<Func<IRequestExecutorBuilder, IRequestExecutorBuilder>, string?[]>
        {
            {
                builder => builder.UseDefaultPipeline(),
                [
                    WellKnownRequestMiddleware.InstrumentationMiddleware,
                    WellKnownRequestMiddleware.ExceptionMiddleware,
                    WellKnownRequestMiddleware.TimeoutMiddleware,
                    WellKnownRequestMiddleware.DocumentCacheMiddleware,
                    WellKnownRequestMiddleware.DocumentParserMiddleware,
                    WellKnownRequestMiddleware.DocumentValidationMiddleware,
                    WellKnownRequestMiddleware.OperationCacheMiddleware,
                    WellKnownRequestMiddleware.OperationResolverMiddleware,
                    WellKnownRequestMiddleware.OperationVariableCoercionMiddleware,
                    WellKnownRequestMiddleware.CostAnalyzerMiddleware,
                    WellKnownRequestMiddleware.SkipWarmupExecutionMiddleware,
                    WellKnownRequestMiddleware.ConcurrencyGateMiddleware,
                    WellKnownRequestMiddleware.OperationExecutionMiddleware
                ]
            },
            {
                builder => builder.UsePersistedOperationPipeline(),
                [
                    WellKnownRequestMiddleware.InstrumentationMiddleware,
                    WellKnownRequestMiddleware.ExceptionMiddleware,
                    WellKnownRequestMiddleware.TimeoutMiddleware,
                    WellKnownRequestMiddleware.DocumentCacheMiddleware,
                    WellKnownRequestMiddleware.ReadPersistedOperationMiddleware,
                    WellKnownRequestMiddleware.PersistedOperationNotFoundMiddleware,
                    WellKnownRequestMiddleware.OnlyPersistedOperationsAllowed,
                    WellKnownRequestMiddleware.DocumentParserMiddleware,
                    WellKnownRequestMiddleware.DocumentValidationMiddleware,
                    WellKnownRequestMiddleware.OperationCacheMiddleware,
                    WellKnownRequestMiddleware.OperationResolverMiddleware,
                    WellKnownRequestMiddleware.OperationVariableCoercionMiddleware,
                    WellKnownRequestMiddleware.CostAnalyzerMiddleware,
                    WellKnownRequestMiddleware.SkipWarmupExecutionMiddleware,
                    WellKnownRequestMiddleware.ConcurrencyGateMiddleware,
                    WellKnownRequestMiddleware.OperationExecutionMiddleware
                ]
            },
            {
                builder => builder.UseAutomaticPersistedOperationPipeline(),
                [
                    WellKnownRequestMiddleware.InstrumentationMiddleware,
                    WellKnownRequestMiddleware.ExceptionMiddleware,
                    WellKnownRequestMiddleware.TimeoutMiddleware,
                    WellKnownRequestMiddleware.DocumentCacheMiddleware,
                    WellKnownRequestMiddleware.ReadPersistedOperationMiddleware,
                    WellKnownRequestMiddleware.AutomaticPersistedOperationNotFoundMiddleware,
                    WellKnownRequestMiddleware.WritePersistedOperationMiddleware,
                    WellKnownRequestMiddleware.DocumentParserMiddleware,
                    WellKnownRequestMiddleware.DocumentValidationMiddleware,
                    WellKnownRequestMiddleware.OperationCacheMiddleware,
                    WellKnownRequestMiddleware.OperationResolverMiddleware,
                    WellKnownRequestMiddleware.OperationVariableCoercionMiddleware,
                    WellKnownRequestMiddleware.CostAnalyzerMiddleware,
                    WellKnownRequestMiddleware.SkipWarmupExecutionMiddleware,
                    WellKnownRequestMiddleware.ConcurrencyGateMiddleware,
                    WellKnownRequestMiddleware.OperationExecutionMiddleware
                ]
            }
        };
    }

    // Resolves the middleware pipeline the way the request executor manager builds it: the
    // directly-added configurations followed by every registered pipeline modifier.
    private static string?[] GetPipelineKeys(IRequestExecutorBuilder builder)
    {
        using var services = builder.Services.BuildServiceProvider();
        var optionsFactory = services.GetRequiredService<IOptionsFactory<RequestExecutorSetup>>();
        var setup = optionsFactory.Create(builder.Name);

        foreach (var modifier in setup.PipelineModifiers)
        {
            modifier(setup.Pipeline);
        }

        return setup.Pipeline.Select(configuration => configuration.Key).ToArray();
    }
}
