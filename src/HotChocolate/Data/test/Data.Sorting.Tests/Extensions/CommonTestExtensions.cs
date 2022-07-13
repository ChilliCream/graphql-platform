using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;

namespace HotChocolate.Data;

public static class CommonTestExtensions
{
    public static ValueTask<IRequestExecutor> CreateExecptionExecutor(
        this IRequestExecutorBuilder builder)
    {
        return builder.UseRequest(
            next => async context =>
            {
                await next(context);
                if (context.ContextData.TryGetValue("ex", out var queryString))
                {
                    context.Result =
                        QueryResultBuilder
                            .FromResult(context.Result!.ExpectQueryResult())
                            .SetContextData("ex", queryString)
                            .Create();
                }
            })
            .UseDefaultPipeline()
            .Services
            .BuildServiceProvider()
            .GetRequiredService<IRequestExecutorResolver>()
            .GetRequestExecutorAsync();
    }

    public static void MatchException(
        this IExecutionResult? result,
        string snapshotName = "")
    {
        if (result is { })
        {
            result.MatchSnapshot(snapshotName);
            if (result.ContextData is { } &&
                result.ContextData.TryGetValue("ex", out var queryResult))
            {
                queryResult.MatchSnapshot(new SnapshotNameExtension(snapshotName + "_ex"));
            }
        }
    }
}
