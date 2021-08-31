using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snapshooter;
using Snapshooter.Xunit;

namespace HotChocolate.Data
{
    public static class CommonTestExtensions
    {
        public static ValueTask<IRequestExecutor> CreateExecptionExecutor(
            this IRequestExecutorBuilder builder)
        {
            return builder.UseRequest(
                next => async context =>
                {
                    await next(context);
                    if (context.Result is IReadOnlyQueryResult result &&
                        context.ContextData.TryGetValue("ex", out object? queryString))
                    {
                        context.Result =
                            QueryResultBuilder
                                .FromResult(result)
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
                    result.ContextData.TryGetValue("ex", out object? queryResult))
                {
                    queryResult.MatchSnapshot(new SnapshotNameExtension(snapshotName + "_ex"));
                }
            }
        }
    }
}
