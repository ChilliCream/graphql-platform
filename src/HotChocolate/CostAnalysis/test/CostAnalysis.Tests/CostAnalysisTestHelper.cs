using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.CostAnalysis;

internal static class CostAnalysisTestHelper
{
    internal static ValueTask<IRequestExecutor> CreateRequestExecutor(string schema)
    {
        return new ServiceCollection()
            .AddGraphQLServer()
            .ModifyCostOptions(o => o.DefaultResolverCost = null)
            .AddDirectiveType<Types.CostDirectiveType>()
            .AddDirectiveType<Types.ListSizeDirectiveType>()
            .AddResolver("Mutation", "setBoth", _ => "ok")
            .AddResolver("Mutation", "setField", _ => "ok")
            .AddResolver("Mutation", "process", _ => "ok")
            .AddResolver("Mutation", "setFieldNested", _ => "ok")
            .AddResolver("Query", "dummy", _ => "ok")
            .AddDocumentFromString(schema)
            .BuildRequestExecutorAsync();
    }

    internal static double GetFieldCost(IExecutionResult result)
    {
        var operationResult = result.ExpectOperationResult();
        var metrics = operationResult.Extensions["operationCost"] as IReadOnlyDictionary<string, object>;
        Assert.NotNull(metrics);
        return Convert.ToDouble(metrics["fieldCost"]);
    }
}
