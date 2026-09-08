using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HotChocolate.Fusion.Execution;

public class PipelineOrderTests
{
    [Theory]
    [MemberData(nameof(Pipelines))]
    public void Pipeline_Should_Order_VariableCoercion_Before_PlanCache_When_Configured(
        Func<IFusionGatewayBuilder, IFusionGatewayBuilder> configurePipeline,
        string?[] expectedKeys)
    {
        // arrange
        var builder = new ServiceCollection().AddGraphQLGateway();
        configurePipeline(builder);

        // act
        var keys = GetPipelineKeys(builder);

        // assert
        Assert.Equal(expectedKeys, keys);
    }

    public static TheoryData<Func<IFusionGatewayBuilder, IFusionGatewayBuilder>, string?[]> Pipelines()
    {
        return new TheoryData<Func<IFusionGatewayBuilder, IFusionGatewayBuilder>, string?[]>
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
                    WellKnownRequestMiddleware.DocumentNormalizationMiddleware,
                    WellKnownRequestMiddleware.OperationVariableCoercionMiddleware,
                    WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                    WellKnownRequestMiddleware.OperationPlanMiddleware,
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
                    WellKnownRequestMiddleware.DocumentNormalizationMiddleware,
                    WellKnownRequestMiddleware.OperationVariableCoercionMiddleware,
                    WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                    WellKnownRequestMiddleware.OperationPlanMiddleware,
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
                    WellKnownRequestMiddleware.DocumentNormalizationMiddleware,
                    WellKnownRequestMiddleware.OperationVariableCoercionMiddleware,
                    WellKnownRequestMiddleware.OperationPlanCacheMiddleware,
                    WellKnownRequestMiddleware.OperationPlanMiddleware,
                    WellKnownRequestMiddleware.SkipWarmupExecutionMiddleware,
                    WellKnownRequestMiddleware.ConcurrencyGateMiddleware,
                    WellKnownRequestMiddleware.OperationExecutionMiddleware
                ]
            }
        };
    }

    // Resolves the middleware pipeline the way the request executor manager builds it: the
    // directly-added configurations followed by every registered pipeline modifier.
    private static string?[] GetPipelineKeys(IFusionGatewayBuilder builder)
    {
        using var services = builder.Services.BuildServiceProvider();
        var optionsFactory = services.GetRequiredService<IOptionsFactory<FusionGatewaySetup>>();
        var setup = optionsFactory.Create(builder.Name);

        var pipeline = new List<RequestMiddlewareConfiguration>();

        foreach (var modifier in setup.PipelineModifiers)
        {
            modifier(pipeline);
        }

        return pipeline.Select(configuration => configuration.Key).ToArray();
    }
}
